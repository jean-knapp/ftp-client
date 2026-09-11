using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FtpClient.Services;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace FtpClient.Remote
{
    /// <summary>The result of a call made on the thread pool: its value, or what it threw.</summary>
    internal struct CallOutcome<T>
    {
        public CallOutcome(T value, Exception error)
        {
            Value = value;
            Error = error;
        }

        public T Value { get; }
        public Exception Error { get; }
    }

    /// <summary>
    /// SFTP over SSH.NET. The library's calls are synchronous, so each runs on the thread pool;
    /// cancellation reaches transfers through the throttled streams the queue hands in, which check
    /// the token on every buffer.
    /// </summary>
    public sealed class SftpSession : IRemoteSession
    {
        private const int CopyBuffer = 64 * 1024;

        private readonly SessionLog _log;
        private readonly string _secret;
        private SftpClient _client;
        private string _hostKeyType;
        private bool _hostKeyRejected;

        public SftpSession(Site site, SessionLog log, string secret)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            _log = log ?? new SessionLog();
            _secret = secret;
        }

        public Site Site { get; }
        public bool IsConnected => _client != null && _client.IsConnected;
        public string SecurityDescription => (_hostKeyType ?? "SSH") + " key · fingerprint verified";
        public bool SupportsResume => true;
        public bool SupportsRawCommands => false;

        public event EventHandler Disconnected;

        // ------------------------------------------------------------------ connection

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            var user = string.IsNullOrEmpty(Site.User) ? Environment.UserName : Site.User;
            var methods = new List<AuthenticationMethod>();
            if (Site.Auth == AuthMethod.KeyFile)
            {
                if (string.IsNullOrEmpty(Site.KeyFile) || !File.Exists(Site.KeyFile))
                {
                    throw new RemoteException("The key file " + Site.KeyFile + " does not exist.", RemoteErrorKind.Authentication);
                }
                try
                {
                    var key = string.IsNullOrEmpty(_secret) ? new PrivateKeyFile(Site.KeyFile) : new PrivateKeyFile(Site.KeyFile, _secret);
                    methods.Add(new PrivateKeyAuthenticationMethod(user, key));
                }
                catch (SshPassPhraseNullOrEmptyException ex)
                {
                    throw new RemoteException("The key file is protected by a passphrase. Enter it as the site's password in the site manager.", RemoteErrorKind.Authentication, ex);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    throw new RemoteException("The key file could not be read: " + ex.Message, RemoteErrorKind.Authentication, ex);
                }
            }
            else
            {
                var password = _secret ?? string.Empty;
                methods.Add(new PasswordAuthenticationMethod(user, password));
                var interactive = new KeyboardInteractiveAuthenticationMethod(user);
                interactive.AuthenticationPrompt += (s, e) =>
                {
                    foreach (var prompt in e.Prompts) prompt.Response = password;
                };
                methods.Add(interactive);
            }

            var info = new ConnectionInfo(Site.Host, Site.Port, user, methods.ToArray())
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(5, AppSettings.Current.ConnectTimeoutSeconds)),
            };
            var client = new SftpClient(info);
            client.HostKeyReceived += Client_HostKeyReceived;
            client.ErrorOccurred += Client_ErrorOccurred;

            _log.Status("Connecting to " + Site.Host + ":" + Site.Port + "…");
            try
            {
                // Off the UI thread, so the host key question can be marshalled back to it.
                await Task.Run(() => client.ConnectAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
                client.OperationTimeout = TimeSpan.FromSeconds(Math.Max(30, AppSettings.Current.ConnectTimeoutSeconds * 2));
            }
            catch (Exception ex)
            {
                client.Dispose();
                if (ex is OperationCanceledException) throw;
                if (_hostKeyRejected) throw new RemoteException("The host key of " + Site.Host + " was not trusted, so the connection was closed.", RemoteErrorKind.Connection, ex);
                var translated = Translate(ex, null);
                _log.Error(translated.Message);
                throw translated;
            }

            _client = client;
            _log.Success("Authenticated as " + user + (Site.Auth == AuthMethod.KeyFile ? " with publickey (" + Format.HomeRelative(Site.KeyFile) + ")" : " with password"));
        }

        private void Client_HostKeyReceived(object sender, HostKeyEventArgs e)
        {
            _hostKeyType = ShortAlgorithm(e.HostKeyName);
            var fingerprint = "SHA256:" + e.FingerPrintSHA256;
            var request = new TrustRequest { Site = Site, Kind = TrustKind.HostKey, Algorithm = _hostKeyType, Fingerprint = fingerprint };
            bool trusted = HostTrust.Verify(request);
            e.CanTrust = trusted;
            _hostKeyRejected = !trusted;
            if (trusted) _log.Status("Host key " + _hostKeyType + " " + fingerprint + (request.IsChange ? " trusted in place of the previous key" : " matches"));
            else _log.Error("Host key " + _hostKeyType + " " + fingerprint + " was not trusted");
        }

        private void Client_ErrorOccurred(object sender, ExceptionEventArgs e)
        {
            _log.Error(e.Exception?.Message ?? "The connection failed");
            if (sender is SftpClient client && !client.IsConnected) Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private static string ShortAlgorithm(string name)
        {
            if (string.IsNullOrEmpty(name)) return "SSH";
            if (name.Contains("ed25519")) return "ed25519";
            if (name.Contains("ed448")) return "ed448";
            if (name.StartsWith("ecdsa", StringComparison.Ordinal)) return "ECDSA";
            if (name.Contains("rsa")) return "RSA";
            if (name.Contains("dss")) return "DSA";
            return name;
        }

        public Task DisconnectAsync()
        {
            var client = _client;
            if (client == null) return Task.CompletedTask;
            return Task.Run(() =>
            {
                try
                {
                    if (client.IsConnected) client.Disconnect();
                    _log.Status("Disconnected from " + Site.Host);
                }
                catch (Exception ex)
                {
                    _log.Warning("Disconnect: " + ex.Message);
                }
                Disconnected?.Invoke(this, EventArgs.Empty);
            });
        }

        public Task KeepAliveAsync(CancellationToken cancellationToken) => Run(null, c => { c.SendKeepAlive(); return true; }, cancellationToken, quiet: true);

        public Task<string> GetHomeDirectoryAsync(CancellationToken cancellationToken) => Run(null, c => c.WorkingDirectory, cancellationToken);

        // ------------------------------------------------------------------ directory operations

        public Task<IReadOnlyList<RemoteEntry>> ListAsync(string path, CancellationToken cancellationToken)
        {
            return Run(path, c =>
            {
                _log.Send("SSH_FXP_OPENDIR " + path);
                var entries = new List<RemoteEntry>();
                int links = 0;
                foreach (var file in c.ListDirectory(path, null))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (file.Name == "." || file.Name == "..") continue;
                    var entry = ToEntry(file.Name, RemotePath.Combine(path, file.Name), file.Attributes);
                    // The listing says what is a link; where it points takes one request each.
                    if (entry.Kind == RemoteEntryKind.Symlink && links++ < MaxLinkTargetsPerListing)
                    {
                        entry.LinkTarget = ReadLink(c, entry.FullPath);
                        entry.LinksToDirectory = LeadsToDirectory(c, entry.FullPath);
                    }
                    entries.Add(entry);
                }
                _log.Receive("SSH_FXP_NAME " + entries.Count + (entries.Count == 1 ? " entry" : " entries"));
                return (IReadOnlyList<RemoteEntry>)entries;
            }, cancellationToken);
        }

        public Task<RemoteEntry> StatAsync(string path, CancellationToken cancellationToken)
        {
            return Run(path, c =>
            {
                try
                {
                    var attributes = c.GetAttributes(path);
                    return ToEntry(RemotePath.Name(path), RemotePath.Normalize(path), attributes);
                }
                catch (SftpPathNotFoundException)
                {
                    return null;
                }
            }, cancellationToken, quiet: true);
        }

        public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            return Run(path, c =>
            {
                _log.Send("SSH_FXP_MKDIR " + path);
                try
                {
                    c.CreateDirectory(path);
                }
                catch (SftpException) when (c.Exists(path))
                {
                    throw new RemoteException(path + " already exists", RemoteErrorKind.AlreadyExists);
                }
                return true;
            }, cancellationToken);
        }

        public Task DeleteFileAsync(string path, CancellationToken cancellationToken)
        {
            return Run(path, c =>
            {
                _log.Send("SSH_FXP_REMOVE " + path);
                try
                {
                    c.DeleteFile(path);
                }
                catch (SshException ex) when (IsLinkToDirectory(c, path))
                {
                    // Some servers - Windows ones - refuse to unlink a link to a folder. Removing it as
                    // a directory instead is not safe: a server may resolve the link and remove the
                    // folder it points to. Say what happened and leave both alone.
                    throw new RemoteException(path + ": the server would not remove this link to a folder (" + ex.Message + "). Nothing was deleted.", RemoteErrorKind.PermissionDenied, ex);
                }
                return true;
            }, cancellationToken);
        }

        /// <summary>True when the parent's listing says the entry is a link and it leads to a directory.</summary>
        private static bool IsLinkToDirectory(SftpClient client, string path)
        {
            try
            {
                var name = RemotePath.Name(path);
                var entry = client.ListDirectory(RemotePath.Parent(path), null).FirstOrDefault(f => f.Name == name);
                return entry != null && entry.IsSymbolicLink && client.GetAttributes(path).IsDirectory;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            return Run(path, c => { _log.Send("SSH_FXP_RMDIR " + path); c.DeleteDirectory(path); return true; }, cancellationToken);
        }

        public Task RenameAsync(string fromPath, string toPath, CancellationToken cancellationToken)
        {
            return Run(fromPath, c =>
            {
                if (c.Exists(toPath)) throw new RemoteException(toPath + " already exists", RemoteErrorKind.AlreadyExists);
                _log.Send("SSH_FXP_RENAME " + fromPath + " " + toPath);
                c.RenameFile(fromPath, toPath);
                return true;
            }, cancellationToken);
        }

        public Task CreateSymlinkAsync(string target, string linkPath, CancellationToken cancellationToken)
        {
            var link = RemotePath.Normalize(linkPath);
            return Run(link, c =>
            {
                if (string.IsNullOrWhiteSpace(target)) throw new RemoteException("The link needs a target.", RemoteErrorKind.General);
                if (c.Exists(link)) throw new RemoteException(link + " already exists", RemoteErrorKind.AlreadyExists);
                // SftpClient.SymbolicLink resolves both paths to absolute ones first, which loses a
                // relative target, so the request is sent through the session directly. OpenSSH -
                // and the servers that copy it - read the target first, against the SFTP draft;
                // AsyncSSH follows the draft for clients it does not recognise.
                bool draftOrder = (c.ConnectionInfo?.ServerVersion ?? string.Empty).IndexOf("AsyncSSH", StringComparison.OrdinalIgnoreCase) >= 0;
                var session = SessionOf(c);
                var request = SessionMethod(session, "RequestSymLink", 2);
                if (session == null || request == null) throw new RemoteException("This version of the SSH library cannot create symbolic links.", RemoteErrorKind.NotSupported);
                _log.Send("SSH_FXP_SYMLINK " + link + " -> " + target);
                try
                {
                    request.Invoke(session, draftOrder ? new object[] { link, target } : new object[] { target, link });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
                _log.Receive("SSH_FXP_STATUS 0 (OK)");
                return true;
            }, cancellationToken);
        }

        public Task SetPermissionsAsync(string path, int mode, CancellationToken cancellationToken)
        {
            return Run(path, c =>
            {
                var octal = Permissions.Octal(mode);
                _log.Send("SSH_FXP_SETSTAT " + path + " mode " + octal);
                // SSH.NET reads the three digits as octal.
                c.ChangePermissions(path, short.Parse(octal));
                _log.Receive("SSH_FXP_STATUS 0 (OK)");
                return true;
            }, cancellationToken);
        }

        public Task SetModifiedTimeAsync(string path, DateTime modifiedUtc, CancellationToken cancellationToken)
        {
            return Run(path, c => { c.SetLastWriteTimeUtc(path, DateTime.SpecifyKind(modifiedUtc, DateTimeKind.Utc)); return true; }, cancellationToken, quiet: true);
        }

        // ------------------------------------------------------------------ transfers

        public Task UploadAsync(Stream source, string remotePath, bool append, IProgress<long> bytesSent, CancellationToken cancellationToken)
        {
            return Transfer(remotePath, c =>
            {
                _log.Send("SSH_FXP_OPEN " + remotePath + (append ? " (WRITE|APPEND)" : " (WRITE|CREAT|TRUNC)"));
                if (!append)
                {
                    // UploadFile pipelines its write requests, which is much faster than a stream.
                    c.UploadFile(source, remotePath, true, sent => bytesSent?.Report((long)sent));
                }
                else
                {
                    using (var remote = c.Open(remotePath, FileMode.Append, FileAccess.Write))
                    {
                        var buffer = new byte[CopyBuffer];
                        long total = 0;
                        int read;
                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            remote.Write(buffer, 0, read);
                            total += read;
                            bytesSent?.Report(total);
                        }
                    }
                }
                _log.Receive("SSH_FXP_STATUS 0 (OK) " + remotePath);
                return true;
            }, cancellationToken);
        }

        public Task DownloadAsync(string remotePath, Stream destination, long offset, IProgress<long> bytesReceived, CancellationToken cancellationToken)
        {
            return Transfer(remotePath, c =>
            {
                _log.Send("SSH_FXP_OPEN " + remotePath + " (READ)" + (offset > 0 ? " from byte " + offset.ToString("N0") : string.Empty));
                if (offset == 0)
                {
                    c.DownloadFile(remotePath, destination, received => bytesReceived?.Report((long)received));
                }
                else
                {
                    using (var remote = c.Open(remotePath, FileMode.Open, FileAccess.Read))
                    {
                        remote.Seek(offset, SeekOrigin.Begin);
                        var buffer = new byte[CopyBuffer];
                        long total = 0;
                        int read;
                        while ((read = remote.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            destination.Write(buffer, 0, read);
                            total += read;
                            bytesReceived?.Report(total);
                        }
                    }
                }
                _log.Receive("SSH_FXP_STATUS 1 (EOF) " + remotePath);
                return true;
            }, cancellationToken);
        }

        public Task<string> SendRawCommandAsync(string command, CancellationToken cancellationToken)
        {
            throw new RemoteException("SFTP has no raw command channel; use an SSH terminal for shell commands.", RemoteErrorKind.NotSupported);
        }

        // ------------------------------------------------------------------ plumbing

        private SftpClient Client
        {
            get
            {
                var client = _client;
                if (client == null || !client.IsConnected) throw new RemoteException("Not connected to " + Site.Host + ".", RemoteErrorKind.Connection);
                return client;
            }
        }

        /// <summary>
        /// Runs a synchronous SSH.NET call on the thread pool. A failure is carried out of the pool
        /// delegate and thrown here, after the await: an exception leaving a Task.Run delegate makes
        /// the Visual Studio debugger stop on it as user-unhandled even though every caller handles it.
        /// </summary>
        private async Task<T> Run<T>(string path, Func<SftpClient, T> work, CancellationToken cancellationToken, bool quiet = false)
        {
            var outcome = await Task.Run(() =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return new CallOutcome<T>(work(Client), null);
                }
                catch (Exception ex)
                {
                    return new CallOutcome<T>(default(T), ex);
                }
            }, cancellationToken).ConfigureAwait(false);

            if (outcome.Error == null) return outcome.Value;
            if (outcome.Error is OperationCanceledException) ExceptionDispatchInfo.Capture(outcome.Error).Throw();
            var translated = Translate(outcome.Error, path);
            // An existing directory is an answer callers expect, not a failure worth logging.
            bool expected = translated.Kind == RemoteErrorKind.AlreadyExists;
            if (!expected && (!quiet || translated.Kind == RemoteErrorKind.Connection)) _log.Error(translated.Message);
            throw translated;
        }

        /// <summary>
        /// Runs a transfer; if it fails or is cancelled part-way the connection is dropped. SSH.NET
        /// does not close the remote handle of an interrupted upload or download, and the server keeps
        /// the file open - and on Windows locked - until the connection goes. The queue opens a
        /// fresh one to resume.
        /// </summary>
        private async Task Transfer(string path, Func<SftpClient, bool> work, CancellationToken cancellationToken)
        {
            try
            {
                await Run(path, work, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                var client = _client;
                _client = null;
                if (client != null)
                {
                    client.HostKeyReceived -= Client_HostKeyReceived;
                    client.ErrorOccurred -= Client_ErrorOccurred;
                    try { client.Dispose(); } catch { }
                }
                throw;
            }
        }

        private const int MaxLinkTargetsPerListing = 500;
        private static readonly FieldInfo SessionField = typeof(SftpClient).GetField("_sftpSession", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo _readLinkMethod;

        /// <summary>SSH.NET's SFTP session, whose requests the public client does not all expose.</summary>
        private static object SessionOf(SftpClient client) => SessionField?.GetValue(client);

        private static MethodInfo SessionMethod(object session, string name, int parameterCount)
        {
            if (session == null) return null;
            foreach (var type in new[] { session.GetType() }.Concat(session.GetType().GetInterfaces()))
            {
                var method = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .FirstOrDefault(m => (m.Name == name || m.Name.EndsWith("." + name, StringComparison.Ordinal)) && m.GetParameters().Length == parameterCount);
                if (method != null) return method;
            }
            return null;
        }

        /// <summary>Whether a link resolves to a directory: false for a broken link, null when it cannot be told.</summary>
        private static bool? LeadsToDirectory(SftpClient client, string path)
        {
            try
            {
                // GetAttributes is STAT, which follows the link; the listing's attributes are the link's own.
                return client.GetAttributes(path).IsDirectory;
            }
            catch (SftpPathNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Where a link points, as the server stores it; null when it cannot be read.</summary>
        private static string ReadLink(SftpClient client, string path)
        {
            try
            {
                var session = SessionOf(client);
                var method = _readLinkMethod ?? (_readLinkMethod = SessionMethod(session, "RequestReadLink", 2));
                if (session == null || method == null) return null;
                // RequestReadLink(path, nullOnError) answers with name/attribute pairs.
                if (!(method.Invoke(session, new object[] { path, true }) is Array names) || names.Length == 0) return null;
                var first = names.GetValue(0);
                return first?.GetType().GetProperty("Key")?.GetValue(first) as string;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static RemoteEntry ToEntry(string name, string fullPath, SftpFileAttributes attributes)
        {
            int mode = 0;
            if (attributes.OwnerCanRead) mode |= 0x100;
            if (attributes.OwnerCanWrite) mode |= 0x80;
            if (attributes.OwnerCanExecute) mode |= 0x40;
            if (attributes.GroupCanRead) mode |= 0x20;
            if (attributes.GroupCanWrite) mode |= 0x10;
            if (attributes.GroupCanExecute) mode |= 0x8;
            if (attributes.OthersCanRead) mode |= 0x4;
            if (attributes.OthersCanWrite) mode |= 0x2;
            if (attributes.OthersCanExecute) mode |= 0x1;
            var kind = attributes.IsSymbolicLink ? RemoteEntryKind.Symlink : attributes.IsDirectory ? RemoteEntryKind.Directory : RemoteEntryKind.File;
            return new RemoteEntry
            {
                Name = name,
                FullPath = fullPath,
                Kind = kind,
                Size = kind == RemoteEntryKind.File ? Math.Max(0, attributes.Size) : 0,
                Modified = attributes.LastWriteTimeUtc == DateTime.MinValue ? (DateTime?)null : DateTime.SpecifyKind(attributes.LastWriteTimeUtc, DateTimeKind.Utc),
                Mode = mode,
                Owner = attributes.UserId.ToString(),
                Group = attributes.GroupId.ToString(),
            };
        }

        private static RemoteException Translate(Exception ex, string path)
        {
            if (ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1) ex = aggregate.InnerException;
            var prefix = string.IsNullOrEmpty(path) ? string.Empty : path + ": ";
            switch (ex)
            {
                case RemoteException remote:
                    return remote;
                case SftpPathNotFoundException _:
                    return new RemoteException(prefix + "no such file or directory", RemoteErrorKind.NotFound, ex);
                case SftpPermissionDeniedException _:
                    return new RemoteException(prefix + "permission denied", RemoteErrorKind.PermissionDenied, ex);
                case SshAuthenticationException _:
                    return new RemoteException("Sign-in was refused: " + ex.Message, RemoteErrorKind.Authentication, ex);
                case SshOperationTimeoutException _:
                    return new RemoteException(prefix + "the server did not answer in time", RemoteErrorKind.Connection, ex);
                case SshConnectionException _:
                    return new RemoteException("The SSH connection failed: " + ex.Message, RemoteErrorKind.Connection, ex);
                case SocketException socket:
                    return new RemoteException("Could not reach the server: " + socket.Message, RemoteErrorKind.Connection, ex);
                case ObjectDisposedException _:
                    return new RemoteException("The connection was closed.", RemoteErrorKind.Connection, ex);
                case SftpException _:
                    return new RemoteException(prefix + ex.Message, RemoteErrorKind.General, ex);
                default:
                    return new RemoteException(prefix + ex.Message, RemoteErrorKind.General, ex);
            }
        }

        public void Dispose()
        {
            var client = _client;
            _client = null;
            if (client == null) return;
            client.HostKeyReceived -= Client_HostKeyReceived;
            client.ErrorOccurred -= Client_ErrorOccurred;
            try { client.Dispose(); } catch { }
        }
    }
}
