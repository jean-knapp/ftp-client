using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FtpClient.Remote;

namespace FtpClient.Transfers
{
    /// <summary>Totals for the queue card's footer and tabs.</summary>
    public sealed class QueueStats
    {
        public int Active;
        public int Queued;
        public int Paused;
        public int Completed;
        public int Failed;
        public long TotalBytes;
        public long TransferredBytes;
        public double UploadBytesPerSecond;
        public double DownloadBytesPerSecond;
        public int BusyConnections;
        public int OpenConnections;

        /// <summary>Items that count toward the aggregate: everything not skipped or cancelled.</summary>
        public int Counted;
    }

    /// <summary>
    /// The application's transfer queue. Items run concurrently up to a per-site connection
    /// limit, each on its own pooled connection. State changes are posted to the synchronization
    /// context the queue was created on, so the UI can bind to the events directly.
    /// </summary>
    public sealed class TransferQueue : IDisposable
    {
        private const int BufferSize = 81920;
        private static readonly TimeSpan NotifyInterval = TimeSpan.FromMilliseconds(120);

        private readonly object _gate = new object();
        private readonly List<TransferItem> _items = new List<TransferItem>();
        private readonly Dictionary<string, ConnectionPool> _pools = new Dictionary<string, ConnectionPool>(StringComparer.Ordinal);
        private readonly Dictionary<long, DateTime> _lastNotified = new Dictionary<long, DateTime>();
        private readonly Func<Site, SessionLog> _logFor;
        private readonly Func<Site, SessionLog, IRemoteSession> _createSession;
        private readonly SynchronizationContext _context;
        private readonly RateLimiter _limiter = new RateLimiter();
        private readonly SemaphoreSlim _conflictGate = new SemaphoreSlim(1, 1);
        private long _nextId;
        private bool _paused;
        private ConflictResolution _standingRule;
        private bool _disposed;

        public TransferQueue(Func<Site, SessionLog> logFor, Func<Site, SessionLog, IRemoteSession> createSession)
        {
            _logFor = logFor ?? throw new ArgumentNullException(nameof(logFor));
            _createSession = createSession ?? throw new ArgumentNullException(nameof(createSession));
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public event EventHandler<TransferItem> ItemAdded;
        public event EventHandler<TransferItem> ItemChanged;
        public event EventHandler ItemsRemoved;
        public event EventHandler<BatchSettledEventArgs> BatchSettled;

        /// <summary>
        /// Asked on a worker thread when a transfer would replace a file. The handler shows the
        /// dialog on the UI thread and completes with the user's choice.
        /// </summary>
        public Func<ConflictInfo, Task<ConflictResolution>> ConflictHandler { get; set; }

        public int MaxConnectionsPerSite { get; set; } = 4;
        public bool ResumeTransfers { get; set; } = true;
        public bool PreserveTimestamps { get; set; } = true;

        public long SpeedLimitBytesPerSecond
        {
            get => _limiter.BytesPerSecond;
            set => _limiter.BytesPerSecond = value;
        }

        public bool IsPaused
        {
            get { lock (_gate) return _paused; }
        }

        public List<TransferItem> Snapshot()
        {
            lock (_gate) return new List<TransferItem>(_items);
        }

        /// <summary>Position among the queued items for its site, counting from 1.</summary>
        public int QueuePosition(TransferItem item)
        {
            lock (_gate)
            {
                int position = 0;
                foreach (var other in _items)
                {
                    if (other.Status != TransferStatus.Queued) continue;
                    position++;
                    if (ReferenceEquals(other, item)) return position;
                }
                return 0;
            }
        }

        // ------------------------------------------------------------------ enqueue

        public TransferItem EnqueueUpload(Site site, string localPath, string remotePath, string batchTag = null)
        {
            long size = 0;
            try { size = new FileInfo(localPath).Length; } catch (IOException) { } catch (UnauthorizedAccessException) { }
            return Add(new TransferItem(Interlocked.Increment(ref _nextId), site, TransferDirection.Upload, localPath, RemotePath.Normalize(remotePath))
            {
                TotalBytes = size,
                BatchTag = batchTag,
            });
        }

        public TransferItem EnqueueDownload(Site site, string remotePath, string localPath, long size)
        {
            return Add(new TransferItem(Interlocked.Increment(ref _nextId), site, TransferDirection.Download, localPath, RemotePath.Normalize(remotePath))
            {
                TotalBytes = size,
            });
        }

        public TransferItem EnqueueDelete(Site site, string remotePath, string batchTag = null)
        {
            return Add(new TransferItem(Interlocked.Increment(ref _nextId), site, TransferDirection.Delete, null, RemotePath.Normalize(remotePath))
            {
                BatchTag = batchTag,
            });
        }

        private TransferItem Add(TransferItem item)
        {
            lock (_gate)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(TransferQueue));
                _items.Add(item);
                // A new batch of work gets asked about conflicts afresh.
                if (_items.All(i => i.IsFinished || i.Status == TransferStatus.Queued)) _standingRule = null;
            }
            Post(() => ItemAdded?.Invoke(this, item));
            Pump();
            return item;
        }

        // ------------------------------------------------------------------ control

        public void PauseAll()
        {
            List<TransferItem> active;
            lock (_gate)
            {
                _paused = true;
                active = _items.Where(i => i.Status == TransferStatus.Active).ToList();
                foreach (var item in _items.Where(i => i.Status == TransferStatus.Queued)) item.Status = TransferStatus.Paused;
            }
            foreach (var item in active)
            {
                item.PauseRequested = true;
                item.Cancellation?.Cancel();
            }
            NotifyAll();
        }

        public void ResumeAll()
        {
            lock (_gate)
            {
                _paused = false;
                foreach (var item in _items.Where(i => i.Status == TransferStatus.Paused)) item.Status = TransferStatus.Queued;
            }
            NotifyAll();
            Pump();
        }

        public void Retry(TransferItem item)
        {
            lock (_gate)
            {
                if (!item.IsFinished) return;
                item.Status = _paused ? TransferStatus.Paused : TransferStatus.Queued;
                item.Error = null;
                item.FinishedAt = null;
            }
            Notify(item, true);
            Pump();
        }

        public void Cancel(TransferItem item)
        {
            bool active;
            lock (_gate)
            {
                if (item.IsFinished) return;
                active = item.Status == TransferStatus.Active;
                if (!active)
                {
                    item.Status = TransferStatus.Cancelled;
                    item.FinishedAt = DateTime.Now;
                }
            }
            if (active) item.Cancellation?.Cancel();
            else
            {
                Notify(item, true);
                CheckBatch(item.BatchTag);
            }
        }

        public void ClearCompleted()
        {
            lock (_gate)
            {
                _items.RemoveAll(i => i.Status == TransferStatus.Completed || i.Status == TransferStatus.Skipped || i.Status == TransferStatus.Cancelled);
            }
            Post(() => ItemsRemoved?.Invoke(this, EventArgs.Empty));
        }

        public QueueStats Stats()
        {
            var stats = new QueueStats();
            lock (_gate)
            {
                foreach (var item in _items)
                {
                    switch (item.Status)
                    {
                        case TransferStatus.Active: stats.Active++; break;
                        case TransferStatus.Queued: stats.Queued++; break;
                        case TransferStatus.Paused: stats.Paused++; break;
                        case TransferStatus.Completed: stats.Completed++; break;
                        case TransferStatus.Failed: stats.Failed++; break;
                    }
                    if (item.Status == TransferStatus.Skipped || item.Status == TransferStatus.Cancelled || item.Direction == TransferDirection.Delete) continue;
                    stats.Counted++;
                    stats.TotalBytes += item.TotalBytes;
                    stats.TransferredBytes += item.Status == TransferStatus.Completed ? item.TotalBytes : item.TransferredBytes;
                    if (item.Status != TransferStatus.Active) continue;
                    if (item.Direction == TransferDirection.Upload) stats.UploadBytesPerSecond += item.BytesPerSecond;
                    else stats.DownloadBytesPerSecond += item.BytesPerSecond;
                }
                foreach (var pool in _pools.Values)
                {
                    stats.BusyConnections += pool.Busy;
                    stats.OpenConnections += pool.Open;
                }
            }
            return stats;
        }

        /// <summary>Closes idle pooled connections, e.g. when a site's tab is closed.</summary>
        public void CloseIdleConnections(Site site = null)
        {
            List<ConnectionPool> pools;
            lock (_gate) pools = _pools.Values.Where(p => site == null || p.Site.Id == site.Id).ToList();
            foreach (var pool in pools) pool.CloseIdle();
        }

        // ------------------------------------------------------------------ scheduling

        private void Pump()
        {
            var starts = new List<TransferItem>();
            lock (_gate)
            {
                if (_paused || _disposed) return;
                var busy = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var item in _items.Where(i => i.Status == TransferStatus.Active))
                {
                    busy.TryGetValue(item.Site.Id, out int count);
                    busy[item.Site.Id] = count + 1;
                }
                foreach (var item in _items.Where(i => i.Status == TransferStatus.Queued))
                {
                    busy.TryGetValue(item.Site.Id, out int count);
                    if (count >= Math.Max(1, MaxConnectionsPerSite)) continue;
                    busy[item.Site.Id] = count + 1;
                    item.Status = TransferStatus.Active;
                    item.StartedAt = DateTime.Now;
                    item.Attempts++;
                    item.Error = null;
                    item.PauseRequested = false;
                    item.Cancellation = new CancellationTokenSource();
                    starts.Add(item);
                }
            }
            foreach (var item in starts)
            {
                Notify(item, true);
                Task.Run(() => RunAsync(item));
            }
        }

        private async Task RunAsync(TransferItem item)
        {
            var pool = PoolFor(item.Site);
            var token = item.Cancellation.Token;
            IRemoteSession session = null;
            bool broken = false;
            try
            {
                session = await pool.AcquireAsync(token).ConfigureAwait(false);
                bool completed;
                switch (item.Direction)
                {
                    case TransferDirection.Upload: completed = await UploadAsync(session, item, token).ConfigureAwait(false); break;
                    case TransferDirection.Download: completed = await DownloadAsync(session, item, token).ConfigureAwait(false); break;
                    default: completed = await DeleteAsync(session, item, token).ConfigureAwait(false); break;
                }
                if (completed) Settle(item, TransferStatus.Completed, null);
            }
            catch (OperationCanceledException)
            {
                Settle(item, item.PauseRequested ? TransferStatus.Paused : TransferStatus.Cancelled, null);
            }
            catch (Exception ex)
            {
                var remote = ex as RemoteException;
                broken = remote == null || remote.Kind == RemoteErrorKind.Connection || session == null || !session.IsConnected;
                _logFor(item.Site).Error(item.FileName + ": " + ex.Message);
                Settle(item, TransferStatus.Failed, ex.Message);
            }
            finally
            {
                if (session != null) pool.Release(session, broken);
                Pump();
            }
        }

        private void Settle(TransferItem item, TransferStatus status, string error)
        {
            lock (_gate)
            {
                // A conflict that chose Skip has already settled the item.
                if (item.Status == TransferStatus.Skipped) status = TransferStatus.Skipped;
                item.Status = status;
                item.Error = error ?? item.Error;
                item.BytesPerSecond = 0;
                if (status == TransferStatus.Completed) item.TransferredBytes = item.TotalBytes;
                if (status != TransferStatus.Paused) item.FinishedAt = DateTime.Now;
                // Directories made for one batch are checked again for the next; they may have been
                // deleted or renamed in between.
                if (_items.All(i => i.IsFinished || i.Status == TransferStatus.Paused)) _knownDirectories.Clear();
            }
            Notify(item, true);
            if (status != TransferStatus.Paused) CheckBatch(item.BatchTag);
        }

        private void CheckBatch(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return;
            List<TransferItem> batch;
            lock (_gate)
            {
                batch = _items.Where(i => i.BatchTag == tag).ToList();
                if (batch.Count == 0 || batch.Any(i => !i.IsFinished)) return;
            }
            int completed = batch.Count(i => i.Status == TransferStatus.Completed);
            var args = new BatchSettledEventArgs(tag, batch[0].Site, completed == batch.Count, completed, batch.Count);
            Post(() => BatchSettled?.Invoke(this, args));
        }

        // ------------------------------------------------------------------ the transfers

        private async Task<bool> UploadAsync(IRemoteSession session, TransferItem item, CancellationToken token)
        {
            var local = new FileInfo(item.LocalPath);
            if (!local.Exists) throw new RemoteException(item.LocalPath + " no longer exists", RemoteErrorKind.NotFound);
            item.TotalBytes = local.Length;

            await EnsureDirectoryAsync(session, RemotePath.Parent(item.RemotePath), token).ConfigureAwait(false);

            bool append = false;
            long offset = 0;
            var existing = await session.StatAsync(item.RemotePath, token).ConfigureAwait(false);
            if (existing != null && existing.IsDirectory)
            {
                throw new RemoteException(item.RemotePath + " is a directory", RemoteErrorKind.AlreadyExists);
            }
            if (existing != null && item.Started && existing.Size == local.Length && ResumeTransfers)
            {
                // Everything reached the server before the pause landed.
                return true;
            }
            bool ownPartial = existing != null && item.Started && ResumeTransfers && session.SupportsResume && existing.Size > 0 && existing.Size < local.Length;
            if (ownPartial)
            {
                // This transfer wrote that partial copy before it was paused or failed.
                append = true;
                offset = existing.Size;
                _logFor(item.Site).Status("Resuming " + item.FileName + " from byte " + offset.ToString("N0"));
            }
            // A commit deploy replaces what is on the server by definition; only loose uploads ask.
            else if (existing != null && item.BatchTag == null)
            {
                var info = new ConflictInfo
                {
                    Item = item,
                    IncomingSize = local.Length,
                    IncomingModified = local.LastWriteTimeUtc,
                    ExistingSize = existing.Size,
                    ExistingModified = existing.Modified,
                    CanResume = ResumeTransfers && session.SupportsResume && existing.Size > 0 && existing.Size < local.Length,
                    RenameTo = await FreeRemoteNameAsync(session, item.RemotePath, token).ConfigureAwait(false),
                };
                var choice = await ResolveAsync(info, token).ConfigureAwait(false);
                switch (choice.Choice)
                {
                    case ConflictChoice.Skip:
                        return Skip(item, "Skipped - already on the server");
                    case ConflictChoice.Cancel:
                        throw new OperationCanceledException(token);
                    case ConflictChoice.OverwriteIfNewer:
                        if (existing.Modified.HasValue && local.LastWriteTimeUtc <= existing.Modified.Value)
                        {
                            return Skip(item, "Skipped - the server copy is as new");
                        }
                        break;
                    case ConflictChoice.Resume:
                        if (info.CanResume)
                        {
                            append = true;
                            offset = existing.Size;
                        }
                        break;
                    case ConflictChoice.Rename:
                        item.RemotePath = info.RenameTo;
                        Notify(item, true);
                        break;
                }
            }

            item.Started = true;
            using (var file = new FileStream(local.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, BufferSize, true))
            using (var throttled = new ThrottledStream(file, _limiter, token))
            {
                if (offset > 0) file.Seek(offset, SeekOrigin.Begin);
                item.TransferredBytes = offset;
                item.BeginSampling(offset);
                await session.UploadAsync(throttled, item.RemotePath, append, Reporter(item, offset), token).ConfigureAwait(false);
            }

            if (PreserveTimestamps)
            {
                try
                {
                    await session.SetModifiedTimeAsync(item.RemotePath, local.LastWriteTimeUtc, token).ConfigureAwait(false);
                }
                catch (RemoteException ex) when (ex.Kind != RemoteErrorKind.Connection)
                {
                    _logFor(item.Site).Warning("Could not keep the modification time of " + item.RemotePath + ": " + ex.Message);
                }
            }
            return true;
        }

        private async Task<bool> DownloadAsync(IRemoteSession session, TransferItem item, CancellationToken token)
        {
            var remote = await session.StatAsync(item.RemotePath, token).ConfigureAwait(false);
            if (remote == null) throw new RemoteException(item.RemotePath + ": no such file", RemoteErrorKind.NotFound);
            item.TotalBytes = remote.Size;

            var directory = Path.GetDirectoryName(item.LocalPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            long offset = 0;
            var local = new FileInfo(item.LocalPath);
            if (local.Exists && item.Started && ResumeTransfers && local.Length == remote.Size)
            {
                return true;
            }
            if (local.Exists && item.Started && ResumeTransfers && local.Length > 0 && local.Length < remote.Size)
            {
                offset = local.Length;
                _logFor(item.Site).Status("Resuming " + item.FileName + " from byte " + offset.ToString("N0"));
            }
            else if (local.Exists)
            {
                var info = new ConflictInfo
                {
                    Item = item,
                    IncomingSize = remote.Size,
                    IncomingModified = remote.Modified,
                    ExistingSize = local.Length,
                    ExistingModified = local.LastWriteTimeUtc,
                    CanResume = ResumeTransfers && local.Length > 0 && local.Length < remote.Size,
                    RenameTo = FreeLocalName(item.LocalPath),
                };
                var choice = await ResolveAsync(info, token).ConfigureAwait(false);
                switch (choice.Choice)
                {
                    case ConflictChoice.Skip:
                        return Skip(item, "Skipped - already downloaded");
                    case ConflictChoice.Cancel:
                        throw new OperationCanceledException(token);
                    case ConflictChoice.OverwriteIfNewer:
                        if (remote.Modified.HasValue && remote.Modified.Value <= local.LastWriteTimeUtc)
                        {
                            return Skip(item, "Skipped - the local copy is as new");
                        }
                        break;
                    case ConflictChoice.Resume:
                        if (info.CanResume) offset = local.Length;
                        break;
                    case ConflictChoice.Rename:
                        item.LocalPath = info.RenameTo;
                        Notify(item, true);
                        break;
                }
            }

            item.Started = true;
            using (var file = new FileStream(item.LocalPath, offset > 0 ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
            using (var throttled = new ThrottledStream(file, _limiter, token))
            {
                item.TransferredBytes = offset;
                item.BeginSampling(offset);
                await session.DownloadAsync(item.RemotePath, throttled, offset, Reporter(item, offset), token).ConfigureAwait(false);
            }

            if (PreserveTimestamps && remote.Modified.HasValue)
            {
                try { File.SetLastWriteTimeUtc(item.LocalPath, remote.Modified.Value); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return true;
        }

        private async Task<bool> DeleteAsync(IRemoteSession session, TransferItem item, CancellationToken token)
        {
            var existing = await session.StatAsync(item.RemotePath, token).ConfigureAwait(false);
            // Already gone is what a delete wants.
            if (existing == null) return true;
            if (existing.IsDirectory) await session.DeleteDirectoryAsync(item.RemotePath, token).ConfigureAwait(false);
            else await session.DeleteFileAsync(item.RemotePath, token).ConfigureAwait(false);
            return true;
        }

        private bool Skip(TransferItem item, string reason)
        {
            lock (_gate)
            {
                item.Status = TransferStatus.Skipped;
                item.Error = reason;
            }
            Settle(item, TransferStatus.Skipped, reason);
            return false;
        }

        private async Task<ConflictResolution> ResolveAsync(ConflictInfo info, CancellationToken token)
        {
            await _conflictGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                lock (_gate)
                {
                    if (_standingRule != null) return _standingRule;
                    info.RemainingConflicts = _items.Count(i => i.Status == TransferStatus.Queued && i.Direction == info.Item.Direction);
                }
                var handler = ConflictHandler;
                var resolution = handler == null
                    ? new ConflictResolution(ConflictChoice.Overwrite)
                    : await handler(info).ConfigureAwait(false);
                if (resolution.ApplyToRemaining)
                {
                    // Resume and Rename are decided per file; the standing rule falls back sensibly.
                    lock (_gate) _standingRule = resolution;
                }
                return resolution;
            }
            finally
            {
                _conflictGate.Release();
            }
        }

        private readonly Dictionary<string, HashSet<string>> _knownDirectories = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        private async Task EnsureDirectoryAsync(IRemoteSession session, string directory, CancellationToken token)
        {
            directory = RemotePath.Normalize(directory);
            if (directory == RemotePath.Root) return;
            HashSet<string> known;
            lock (_gate)
            {
                if (!_knownDirectories.TryGetValue(session.Site.Id, out known)) _knownDirectories[session.Site.Id] = known = new HashSet<string>(StringComparer.Ordinal);
                if (known.Contains(directory)) return;
            }

            var existing = await session.StatAsync(directory, token).ConfigureAwait(false);
            if (existing == null)
            {
                await EnsureDirectoryAsync(session, RemotePath.Parent(directory), token).ConfigureAwait(false);
                try
                {
                    await session.CreateDirectoryAsync(directory, token).ConfigureAwait(false);
                }
                catch (RemoteException ex) when (ex.Kind == RemoteErrorKind.AlreadyExists)
                {
                    // Another worker made it first.
                }
            }
            else if (!existing.IsDirectory)
            {
                throw new RemoteException(directory + " exists and is not a directory", RemoteErrorKind.AlreadyExists);
            }
            lock (_gate) known.Add(directory);
        }

        private static async Task<string> FreeRemoteNameAsync(IRemoteSession session, string remotePath, CancellationToken token)
        {
            var directory = RemotePath.Parent(remotePath);
            var name = RemotePath.Name(remotePath);
            for (int n = 1; n < 1000; n++)
            {
                var candidate = RemotePath.Combine(directory, NumberedName(name, n));
                if (await session.StatAsync(candidate, token).ConfigureAwait(false) == null) return candidate;
            }
            return RemotePath.Combine(directory, NumberedName(name, DateTime.Now.Ticks % 100000));
        }

        private static string FreeLocalName(string localPath)
        {
            var directory = Path.GetDirectoryName(localPath) ?? string.Empty;
            var name = Path.GetFileName(localPath);
            for (int n = 1; n < 1000; n++)
            {
                var candidate = Path.Combine(directory, NumberedName(name, n));
                if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
            }
            return Path.Combine(directory, NumberedName(name, DateTime.Now.Ticks % 100000));
        }

        /// <summary><c>index.php</c> becomes <c>index (1).php</c>; <c>archive.tar.gz</c> keeps both extensions.</summary>
        public static string NumberedName(string name, long number)
        {
            int dot = name.IndexOf('.', 1);
            if (name.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase)) dot = name.Length - 7;
            else dot = name.LastIndexOf('.');
            if (dot <= 0) return name + " (" + number + ")";
            return name.Substring(0, dot) + " (" + number + ")" + name.Substring(dot);
        }

        // ------------------------------------------------------------------ plumbing

        private IProgress<long> Reporter(TransferItem item, long offset)
        {
            return new SyncProgress(sent =>
            {
                if (item.Sample(offset + sent)) Notify(item, false);
            });
        }

        private ConnectionPool PoolFor(Site site)
        {
            lock (_gate)
            {
                if (!_pools.TryGetValue(site.Id, out var pool))
                {
                    pool = new ConnectionPool(site, () => _createSession(site, _logFor(site)));
                    _pools[site.Id] = pool;
                }
                return pool;
            }
        }

        private void Notify(TransferItem item, bool force)
        {
            var now = DateTime.UtcNow;
            lock (_lastNotified)
            {
                if (!force && _lastNotified.TryGetValue(item.Id, out var last) && now - last < NotifyInterval) return;
                _lastNotified[item.Id] = now;
            }
            Post(() => ItemChanged?.Invoke(this, item));
        }

        private void NotifyAll()
        {
            foreach (var item in Snapshot()) Notify(item, true);
        }

        private void Post(Action action)
        {
            _context.Post(_ => action(), null);
        }

        public void Dispose()
        {
            List<TransferItem> active;
            List<ConnectionPool> pools;
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                active = _items.Where(i => i.Status == TransferStatus.Active).ToList();
                pools = _pools.Values.ToList();
            }
            foreach (var item in active) item.Cancellation?.Cancel();
            foreach (var pool in pools) pool.Dispose();
        }

        /// <summary>A progress sink that reports on the calling thread instead of posting.</summary>
        private sealed class SyncProgress : IProgress<long>
        {
            private readonly Action<long> _report;
            public SyncProgress(Action<long> report) { _report = report; }
            public void Report(long value) => _report(value);
        }

        /// <summary>Connections to one site, kept open between transfers.</summary>
        private sealed class ConnectionPool : IDisposable
        {
            private readonly Func<IRemoteSession> _create;
            private readonly Stack<IRemoteSession> _idle = new Stack<IRemoteSession>();
            private readonly object _gate = new object();

            public ConnectionPool(Site site, Func<IRemoteSession> create)
            {
                Site = site;
                _create = create;
            }

            public Site Site { get; }
            public int Busy { get; private set; }

            public int Open
            {
                get { lock (_gate) return Busy + _idle.Count; }
            }

            public async Task<IRemoteSession> AcquireAsync(CancellationToken token)
            {
                while (true)
                {
                    IRemoteSession session = null;
                    lock (_gate)
                    {
                        if (_idle.Count > 0) session = _idle.Pop();
                        Busy++;
                    }
                    if (session != null && session.IsConnected) return session;
                    session?.Dispose();

                    try
                    {
                        session = _create();
                        await session.ConnectAsync(token).ConfigureAwait(false);
                        return session;
                    }
                    catch
                    {
                        session?.Dispose();
                        lock (_gate) Busy--;
                        throw;
                    }
                }
            }

            public void Release(IRemoteSession session, bool broken)
            {
                lock (_gate)
                {
                    Busy = Math.Max(0, Busy - 1);
                    if (!broken && session.IsConnected)
                    {
                        _idle.Push(session);
                        return;
                    }
                }
                session.Dispose();
            }

            public void CloseIdle()
            {
                List<IRemoteSession> idle;
                lock (_gate)
                {
                    idle = _idle.ToList();
                    _idle.Clear();
                }
                foreach (var session in idle)
                {
                    try { session.DisconnectAsync().Wait(2000); } catch { }
                    session.Dispose();
                }
            }

            public void Dispose() => CloseIdle();
        }
    }
}
