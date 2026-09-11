using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Remote
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Failed,
    }

    /// <summary>
    /// A tab's browsing connection. Commands run one at a time - FTP control connections and the
    /// SFTP request stream both expect that - and a command that finds the connection gone
    /// reconnects once and tries again. Transfers use their own pooled connections.
    /// </summary>
    public sealed class SiteConnection : IDisposable
    {
        private readonly Func<Site, SessionLog, IRemoteSession> _factory;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private IRemoteSession _session;
        private ConnectionState _state = ConnectionState.Disconnected;
        private bool _disposed;

        public SiteConnection(Site site, SessionLog log, Func<Site, SessionLog, IRemoteSession> factory)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Log = log ?? new SessionLog();
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public Site Site { get; }
        public SessionLog Log { get; }

        public ConnectionState State => _state;

        /// <summary>Raised on the thread that changed the state; the UI marshals it.</summary>
        public event EventHandler StateChanged;

        public string LastError { get; private set; }

        /// <summary>What kind of failure <see cref="LastError"/> was, e.g. a refused password.</summary>
        public RemoteErrorKind LastErrorKind { get; private set; }

        /// <summary>Where the server put us after signing in.</summary>
        public string HomeDirectory { get; private set; } = RemotePath.Root;

        public string SecurityDescription => _session?.SecurityDescription;
        public bool SupportsRawCommands => _session != null && _session.SupportsRawCommands;

        /// <summary>When the connection last did something, for the "Idle 4s" status.</summary>
        public DateTime LastActivity { get; private set; } = DateTime.Now;

        public bool IsBusy => _gate.CurrentCount == 0;

        private void SetState(ConnectionState state)
        {
            if (_state == state) return;
            _state = state;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                await OpenAsync(token).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task OpenAsync(CancellationToken token)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(SiteConnection));
            CloseSession();
            SetState(ConnectionState.Connecting);
            IRemoteSession session = null;
            try
            {
                session = _factory(Site, Log);
                await session.ConnectAsync(token).ConfigureAwait(false);
                HomeDirectory = await session.GetHomeDirectoryAsync(token).ConfigureAwait(false) ?? RemotePath.Root;
                session.Disconnected += Session_Disconnected;
                _session = session;
                LastError = null;
                LastActivity = DateTime.Now;
                SetState(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                session?.Dispose();
                LastError = ex is OperationCanceledException ? "Cancelled" : ex.Message;
                LastErrorKind = (ex as RemoteException)?.Kind ?? RemoteErrorKind.General;
                SetState(ex is OperationCanceledException ? ConnectionState.Disconnected : ConnectionState.Failed);
                throw;
            }
        }

        private void Session_Disconnected(object sender, EventArgs e)
        {
            if (!ReferenceEquals(sender, _session)) return;
            if (_state == ConnectionState.Connected) SetState(ConnectionState.Disconnected);
        }

        public async Task<T> RunAsync<T>(Func<IRemoteSession, Task<T>> work, CancellationToken token)
        {
            await _gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_session == null || !_session.IsConnected)
                {
                    Log.Status("Reconnecting to " + Site.Endpoint);
                    await OpenAsync(token).ConfigureAwait(false);
                }
                try
                {
                    var result = await work(_session).ConfigureAwait(false);
                    LastActivity = DateTime.Now;
                    return result;
                }
                catch (RemoteException ex) when (ex.Kind == RemoteErrorKind.Connection && !token.IsCancellationRequested)
                {
                    Log.Warning("Connection lost (" + ex.Message + ") — reconnecting");
                    await OpenAsync(token).ConfigureAwait(false);
                    var result = await work(_session).ConfigureAwait(false);
                    LastActivity = DateTime.Now;
                    return result;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task RunAsync(Func<IRemoteSession, Task> work, CancellationToken token)
        {
            return RunAsync<bool>(async session =>
            {
                await work(session).ConfigureAwait(false);
                return true;
            }, token);
        }

        /// <summary>Sends a keepalive when the connection is idle; skips it when a command is running.</summary>
        public async Task KeepAliveAsync()
        {
            if (_session == null || _state != ConnectionState.Connected) return;
            if (!await _gate.WaitAsync(0).ConfigureAwait(false)) return;
            try
            {
                await _session.KeepAliveAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Warning("Keepalive failed: " + ex.Message);
                if (!_session.IsConnected) SetState(ConnectionState.Disconnected);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_session != null)
                {
                    try { await _session.DisconnectAsync().ConfigureAwait(false); } catch { }
                }
                CloseSession();
                SetState(ConnectionState.Disconnected);
            }
            finally
            {
                _gate.Release();
            }
        }

        private void CloseSession()
        {
            var session = _session;
            _session = null;
            if (session == null) return;
            session.Disconnected -= Session_Disconnected;
            session.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CloseSession();
        }
    }
}
