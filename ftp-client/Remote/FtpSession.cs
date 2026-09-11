using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;
using FtpClient.Services;

namespace FtpClient.Remote
{
    /// <summary>
    /// FTP and explicit FTPS over FluentFTP. Transfers use the library's data streams directly, so
    /// progress, throttling, resume and cancellation all go through the queue's own copy loop.
    /// </summary>
    public sealed class FtpSession : IRemoteSession
    {
        private const int CopyBuffer = 81920;

        private static readonly HashSet<string> TextExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".html", ".htm", ".css", ".js", ".mjs", ".php", ".json", ".xml", ".md", ".csv", ".ini", ".conf",
            ".config", ".sh", ".py", ".rb", ".pl", ".sql", ".yml", ".yaml", ".htaccess", ".svg",
        };

        private readonly SessionLog _log;
        private readonly string _secret;
        private AsyncFtpClient _client;
        private bool _certificateRejected;
        private bool _certificateTrustedManually;

        public FtpSession(Site site, SessionLog log, string secret)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            _log = log ?? new SessionLog();
            _secret = secret;
        }

        public Site Site { get; }
        public bool IsConnected => _client != null && _client.IsConnected;

        public string SecurityDescription
        {
            get
            {
                if (_client == null || !_client.IsEncrypted) return "not encrypted";
                return "TLS · certificate " + (_certificateTrustedManually ? "trusted for this site" : "verified");
            }
        }

        public bool SupportsResume => true;
        public bool SupportsRawCommands => true;

        public event EventHandler Disconnected;

        // ------------------------------------------------------------------ connection

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            bool anonymous = Site.Auth == AuthMethod.Anonymous || string.IsNullOrEmpty(Site.User);
            var client = new AsyncFtpClient
            {
                Host = Site.Host,
                Port = Site.Port,
                Credentials = new NetworkCredential(anonymous ? "anonymous" : Site.User, anonymous ? "anonymous@" : _secret ?? string.Empty),
            };
            var settings = AppSettings.Current;
            int timeout = Math.Max(5, settings.ConnectTimeoutSeconds) * 1000;
            client.Config.EncryptionMode = Site.Protocol == RemoteProtocol.Ftps ? FtpEncryptionMode.Explicit : FtpEncryptionMode.None;
            client.Config.DataConnectionType = settings.PassiveMode ? FtpDataConnectionType.AutoPassive : FtpDataConnectionType.AutoActive;
            client.Config.ConnectTimeout = timeout;
            client.Config.ReadTimeout = Math.Max(timeout, 30000);
            client.Config.DataConnectionConnectTimeout = timeout;
            client.Config.DataConnectionReadTimeout = Math.Max(timeout, 30000);
            client.Config.SocketKeepAlive = true;
            client.Config.RetryAttempts = 1;
            client.Config.LogToConsole = false;
            client.Config.LogPassword = false;
            client.Logger = new LogAdapter(_log);
            client.ValidateCertificate += (control, e) => OnValidateCertificate(e);

            try
            {
                await Task.Run(() => client.Connect(cancellationToken), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                client.Dispose();
                if (ex is OperationCanceledException) throw;
                if (_certificateRejected) throw new RemoteException("The certificate of " + Site.Host + " was not trusted, so the connection was closed.", RemoteErrorKind.Connection, ex);
                var translated = Translate(ex, null);
                _log.Error(translated.Message);
                throw translated;
            }
            _client = client;
            _log.Success("Signed in to " + Site.Host + " as " + (anonymous ? "anonymous" : Site.User) + (client.IsEncrypted ? " over TLS" : string.Empty));
        }

        private void OnValidateCertificate(FtpSslValidationEventArgs e)
        {
            if (e.PolicyErrors == SslPolicyErrors.None)
            {
                e.Accept = true;
                return;
            }
            var certificate = new X509Certificate2(e.Certificate);
            var request = new TrustRequest
            {
                Site = Site,
                Kind = TrustKind.Certificate,
                Fingerprint = certificate.Thumbprint,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer,
                Problem = DescribePolicyErrors(e.PolicyErrors),
            };
            e.Accept = HostTrust.Verify(request);
            _certificateRejected = !e.Accept;
            _certificateTrustedManually = e.Accept;
            if (e.Accept) _log.Warning("Certificate " + certificate.Thumbprint + " accepted for this site (" + request.Problem + ")");
            else _log.Error("Certificate " + certificate.Thumbprint + " was not trusted (" + request.Problem + ")");
        }

        private static string DescribePolicyErrors(SslPolicyErrors errors)
        {
            var reasons = new List<string>();
            if ((errors & SslPolicyErrors.RemoteCertificateNameMismatch) != 0) reasons.Add("the name does not match the host");
            if ((errors & SslPolicyErrors.RemoteCertificateChainErrors) != 0) reasons.Add("it is not issued by an authority Windows trusts");
            if ((errors & SslPolicyErrors.RemoteCertificateNotAvailable) != 0) reasons.Add("the server sent none");
            return reasons.Count == 0 ? errors.ToString() : string.Join(" and ", reasons);
        }

        public async Task DisconnectAsync()
        {
            var client = _client;
            if (client == null) return;
            try
            {
                if (client.IsConnected) await client.Disconnect(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warning("Disconnect: " + ex.Message);
            }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public Task KeepAliveAsync(CancellationToken cancellationToken) => Guard(null, () => Client.Execute("NOOP", cancellationToken));

        public Task<string> GetHomeDirectoryAsync(CancellationToken cancellationToken) => Guard(null, () => Client.GetWorkingDirectory(cancellationToken));

        // ------------------------------------------------------------------ directory operations

        public async Task<IReadOnlyList<RemoteEntry>> ListAsync(string path, CancellationToken cancellationToken)
        {
            var directory = RemotePath.Normalize(path);
            var items = await Guard(path, () => Client.GetListing(directory, FtpListOption.AllFiles, cancellationToken)).ConfigureAwait(false);
            var entries = items
                .Where(i => i.Name != "." && i.Name != "..")
                .Select(i => ToEntry(i, RemotePath.Combine(directory, i.Name)))
                .ToList();
            _log.Status("Listed " + directory + " - " + entries.Count + (entries.Count == 1 ? " item" : " items"));
            return entries;
        }

        public async Task<RemoteEntry> StatAsync(string path, CancellationToken cancellationToken)
        {
            var normalized = RemotePath.Normalize(path);
            if (normalized == RemotePath.Root) return new RemoteEntry { Name = RemotePath.Root, FullPath = RemotePath.Root, Kind = RemoteEntryKind.Directory };
            var client = Client;

            FtpListItem item = null;
            try
            {
                item = await client.GetObjectInfo(normalized, true, cancellationToken).ConfigureAwait(false);
            }
            catch (FtpCommandException)
            {
                // Missing objects answer 550; servers without MLST fall through below.
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw Translate(ex, normalized);
            }
            if (item != null) return ToEntry(item, normalized);

            return await Guard(normalized, async () =>
            {
                if (await client.DirectoryExists(normalized, cancellationToken).ConfigureAwait(false))
                {
                    return new RemoteEntry { Name = RemotePath.Name(normalized), FullPath = normalized, Kind = RemoteEntryKind.Directory };
                }
                if (!await client.FileExists(normalized, cancellationToken).ConfigureAwait(false)) return null;
                long size = await client.GetFileSize(normalized, -1, cancellationToken).ConfigureAwait(false);
                DateTime? modified = null;
                try
                {
                    var time = await client.GetModifiedTime(normalized, cancellationToken).ConfigureAwait(false);
                    if (time != DateTime.MinValue) modified = ToUtc(time);
                }
                catch (FtpCommandException)
                {
                }
                return new RemoteEntry { Name = RemotePath.Name(normalized), FullPath = normalized, Kind = RemoteEntryKind.File, Size = Math.Max(0, size), Modified = modified };
            }).ConfigureAwait(false);
        }

        public async Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            var directory = RemotePath.Normalize(path);
            var client = Client;
            bool created;
            try
            {
                created = await Guard(path, () => client.CreateDirectory(directory, false, cancellationToken)).ConfigureAwait(false);
            }
            catch (RemoteException ex) when (ex.Kind != RemoteErrorKind.Connection)
            {
                // FluentFTP checks for the directory and then sends MKD; a parallel upload that
                // creates it in between gets "550 File exists" instead of false.
                bool exists;
                try { exists = await client.DirectoryExists(directory, cancellationToken).ConfigureAwait(false); }
                catch (Exception) when (!cancellationToken.IsCancellationRequested) { exists = false; }
                if (!exists) throw;
                created = false;
            }
            if (!created) throw new RemoteException(path + " already exists", RemoteErrorKind.AlreadyExists);
        }

        public Task DeleteFileAsync(string path, CancellationToken cancellationToken) =>
            Guard(path, async () => { await Client.DeleteFile(RemotePath.Normalize(path), cancellationToken).ConfigureAwait(false); return true; });

        public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) =>
            Guard(path, async () => { await Client.DeleteDirectory(RemotePath.Normalize(path), cancellationToken).ConfigureAwait(false); return true; });

        public async Task RenameAsync(string fromPath, string toPath, CancellationToken cancellationToken)
        {
            var client = Client;
            var target = RemotePath.Normalize(toPath);
            await Guard(fromPath, async () =>
            {
                if (await client.FileExists(target, cancellationToken).ConfigureAwait(false) || await client.DirectoryExists(target, cancellationToken).ConfigureAwait(false))
                {
                    throw new RemoteException(target + " already exists", RemoteErrorKind.AlreadyExists);
                }
                await client.Rename(RemotePath.Normalize(fromPath), target, cancellationToken).ConfigureAwait(false);
                return true;
            }).ConfigureAwait(false);
        }

        public async Task CreateSymlinkAsync(string target, string linkPath, CancellationToken cancellationToken)
        {
            var link = RemotePath.Normalize(linkPath);
            if (string.IsNullOrWhiteSpace(target)) throw new RemoteException("The link needs a target.", RemoteErrorKind.General);
            // FTP has no symlink command; ProFTPD's mod_site_misc adds SITE SYMLINK <target> <link>,
            // whose two arguments are split at the space.
            if (target.Contains(" ") || link.Contains(" ")) throw new RemoteException("SITE SYMLINK cannot take paths with spaces; connect over SFTP to create this link.", RemoteErrorKind.NotSupported);
            var reply = await Guard(link, () => Client.Execute("SITE SYMLINK " + target + " " + link, cancellationToken)).ConfigureAwait(false);
            if (reply.Success) return;
            if (reply.Code == "500" || reply.Code == "501" || reply.Code == "502" || reply.Code == "504")
            {
                throw new RemoteException("This FTP server cannot create symbolic links (it does not offer SITE SYMLINK). Connect over SFTP to create them.", RemoteErrorKind.NotSupported);
            }
            throw new RemoteException(link + ": " + reply.Code + " " + (reply.ErrorMessage ?? reply.Message), KindForReply(reply.Code, reply.Message));
        }

        public Task SetPermissionsAsync(string path, int mode, CancellationToken cancellationToken) =>
            Guard(path, async () =>
            {
                // SITE CHMOD takes the three octal digits; FluentFTP wants them as that decimal number.
                await Client.SetFilePermissions(RemotePath.Normalize(path), int.Parse(Permissions.Octal(mode)), cancellationToken).ConfigureAwait(false);
                return true;
            });

        public Task SetModifiedTimeAsync(string path, DateTime modifiedUtc, CancellationToken cancellationToken) =>
            Guard(path, async () =>
            {
                await Client.SetModifiedTime(RemotePath.Normalize(path), DateTime.SpecifyKind(modifiedUtc, DateTimeKind.Utc), cancellationToken).ConfigureAwait(false);
                return true;
            });

        // ------------------------------------------------------------------ transfers

        private static FtpDataType DataTypeFor(string path)
        {
            switch (AppSettings.Current.TransferMode)
            {
                case TransferMode.Ascii: return FtpDataType.ASCII;
                case TransferMode.Auto:
                    var name = RemotePath.Name(path);
                    return TextExtensions.Contains(Path.GetExtension(name)) || TextExtensions.Contains(name) ? FtpDataType.ASCII : FtpDataType.Binary;
                default: return FtpDataType.Binary;
            }
        }

        public async Task UploadAsync(Stream source, string remotePath, bool append, IProgress<long> bytesSent, CancellationToken cancellationToken)
        {
            var client = Client;
            var target = RemotePath.Normalize(remotePath);
            var type = DataTypeFor(target);
            Stream remote = null;
            try
            {
                remote = append
                    ? await client.OpenAppend(target, type, false, cancellationToken).ConfigureAwait(false)
                    : await client.OpenWrite(target, type, false, cancellationToken).ConfigureAwait(false);
                var buffer = new byte[CopyBuffer];
                long total = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await remote.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    total += read;
                    bytesSent?.Report(total);
                }
                remote.Dispose();
                remote = null;
                var reply = await client.GetReply(cancellationToken).ConfigureAwait(false);
                if (!reply.Success) throw new RemoteException(target + ": " + reply.Code + " " + (reply.ErrorMessage ?? reply.Message), KindForReply(reply.Code, reply.Message));
            }
            catch (Exception ex)
            {
                remote?.Dispose();
                // A transfer cut off halfway leaves the control connection out of step; start over.
                if (!(ex is RemoteException)) Abort();
                if (ex is OperationCanceledException) throw;
                var translated = Translate(ex, target);
                _log.Error(translated.Message);
                throw translated;
            }
        }

        public async Task DownloadAsync(string remotePath, Stream destination, long offset, IProgress<long> bytesReceived, CancellationToken cancellationToken)
        {
            var client = Client;
            var target = RemotePath.Normalize(remotePath);
            Stream remote = null;
            try
            {
                remote = await client.OpenRead(target, DataTypeFor(target), offset, false, cancellationToken).ConfigureAwait(false);
                var buffer = new byte[CopyBuffer];
                long total = 0;
                int read;
                while ((read = await remote.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                    total += read;
                    bytesReceived?.Report(total);
                }
                remote.Dispose();
                remote = null;
                var reply = await client.GetReply(cancellationToken).ConfigureAwait(false);
                if (!reply.Success) throw new RemoteException(target + ": " + reply.Code + " " + (reply.ErrorMessage ?? reply.Message), KindForReply(reply.Code, reply.Message));
            }
            catch (Exception ex)
            {
                remote?.Dispose();
                if (!(ex is RemoteException)) Abort();
                if (ex is OperationCanceledException) throw;
                var translated = Translate(ex, target);
                _log.Error(translated.Message);
                throw translated;
            }
        }

        public async Task<string> SendRawCommandAsync(string command, CancellationToken cancellationToken)
        {
            var reply = await Guard(null, () => Client.Execute(command, cancellationToken)).ConfigureAwait(false);
            var text = reply.Code + " " + reply.Message;
            if (!reply.Success) throw new RemoteException(text, KindForReply(reply.Code, reply.Message));
            return text;
        }

        // ------------------------------------------------------------------ plumbing

        private AsyncFtpClient Client
        {
            get
            {
                var client = _client;
                if (client == null || !client.IsConnected) throw new RemoteException("Not connected to " + Site.Host + ".", RemoteErrorKind.Connection);
                return client;
            }
        }

        private async Task<T> Guard<T>(string path, Func<Task<T>> work)
        {
            try
            {
                return await work().ConfigureAwait(false);
            }
            catch (RemoteException)
            {
                throw;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                var translated = Translate(ex, path);
                if (translated.Kind == RemoteErrorKind.Connection) Abort();
                throw translated;
            }
        }

        /// <summary>Drops a connection that can no longer be trusted to be in step with the server.</summary>
        private void Abort()
        {
            var client = _client;
            _client = null;
            if (client == null) return;
            try { client.Dispose(); } catch { }
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private static DateTime ToUtc(DateTime time)
        {
            if (time.Kind == DateTimeKind.Local) return time.ToUniversalTime();
            return DateTime.SpecifyKind(time, DateTimeKind.Utc);
        }

        private static RemoteEntry ToEntry(FtpListItem item, string fullPath)
        {
            var kind = item.Type == FtpObjectType.Directory ? RemoteEntryKind.Directory : item.Type == FtpObjectType.Link ? RemoteEntryKind.Symlink : RemoteEntryKind.File;
            int? mode = null;
            if (item.Chmod > 0)
            {
                try { mode = Convert.ToInt32(item.Chmod.ToString(), 8); } catch (FormatException) { }
            }
            if (mode == null && !string.IsNullOrEmpty(item.RawPermissions)) mode = Permissions.ParseSymbolic(item.RawPermissions);

            // MLSD servers put the Unix details in facts FluentFTP does not map: unix.mode=0644;unix.owner=deploy;
            var owner = item.RawOwner;
            var group = item.RawGroup;
            var facts = MlsdFacts(item.Input);
            if (mode == null && facts.TryGetValue("unix.mode", out var factMode)) mode = Permissions.ParseOctal(factMode.TrimStart('0').PadLeft(3, '0'));
            if (string.IsNullOrEmpty(owner)) owner = facts.TryGetValue("unix.owner", out var o) ? o : facts.TryGetValue("unix.uid", out var uid) ? uid : null;
            if (string.IsNullOrEmpty(group)) group = facts.TryGetValue("unix.group", out var g) ? g : facts.TryGetValue("unix.gid", out var gid) ? gid : null;

            return new RemoteEntry
            {
                Name = RemotePath.Name(fullPath),
                FullPath = fullPath,
                Kind = kind,
                Size = kind == RemoteEntryKind.File ? Math.Max(0, item.Size) : 0,
                Modified = item.Modified == DateTime.MinValue ? (DateTime?)null : ToUtc(item.Modified),
                Mode = mode,
                Owner = owner,
                Group = group,
                LinkTarget = item.LinkTarget,
            };
        }

        /// <summary>The <c>name=value;</c> facts at the start of an MLSD or MLST line.</summary>
        private static Dictionary<string, string> MlsdFacts(string line)
        {
            var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(line)) return facts;
            int space = line.IndexOf(' ');
            var head = space > 0 ? line.Substring(0, space) : line;
            foreach (var part in head.Split(';'))
            {
                int equals = part.IndexOf('=');
                if (equals > 0) facts[part.Substring(0, equals).Trim()] = part.Substring(equals + 1).Trim();
            }
            return facts;
        }

        private static bool Contains(string text, string part) => text.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0;

        private static RemoteErrorKind KindForReply(string code, string message)
        {
            message = message ?? string.Empty;
            if (code == "530") return RemoteErrorKind.Authentication;
            if (message.IndexOf("denied", StringComparison.OrdinalIgnoreCase) >= 0 || message.IndexOf("permission", StringComparison.OrdinalIgnoreCase) >= 0) return RemoteErrorKind.PermissionDenied;
            if (code == "550" || code == "553")
            {
                if (Contains(message, "no such") || Contains(message, "not exist") || Contains(message, "not found") || Contains(message, "cannot find")) return RemoteErrorKind.NotFound;
                if (Contains(message, "exists") || Contains(message, "already")) return RemoteErrorKind.AlreadyExists;
            }
            if (code == "500" || code == "502" || code == "504") return RemoteErrorKind.NotSupported;
            if (code == "421") return RemoteErrorKind.Connection;
            return RemoteErrorKind.General;
        }

        private static RemoteException Translate(Exception ex, string path)
        {
            if (ex is AggregateException aggregate && aggregate.InnerExceptions.Count == 1) ex = aggregate.InnerException;
            var prefix = string.IsNullOrEmpty(path) ? string.Empty : path + ": ";
            switch (ex)
            {
                case RemoteException remote:
                    return remote;
                case FtpAuthenticationException _:
                    return new RemoteException("Sign-in was refused: " + ex.Message, RemoteErrorKind.Authentication, ex);
                case FtpCommandException command:
                    return new RemoteException(prefix + command.Message, KindForReply(command.CompletionCode, command.Message), ex);
                case FtpSecurityNotAvailableException _:
                    return new RemoteException("The server does not offer TLS, so an FTPS connection is not possible.", RemoteErrorKind.Connection, ex);
                case FtpInvalidCertificateException _:
                    return new RemoteException("The server's certificate was not accepted: " + ex.Message, RemoteErrorKind.Connection, ex);
                case FtpMissingSocketException _:
                case TimeoutException _:
                case SocketException _:
                case IOException _:
                case ObjectDisposedException _:
                    return new RemoteException(prefix + "the connection was lost (" + ex.Message + ")", RemoteErrorKind.Connection, ex);
                default:
                    return new RemoteException(prefix + ex.Message, RemoteErrorKind.General, ex);
            }
        }

        public void Dispose()
        {
            var client = _client;
            _client = null;
            try { client?.Dispose(); } catch { }
        }

        /// <summary>Turns FluentFTP's trace into the protocol log's commands, responses and status lines.</summary>
        private sealed class LogAdapter : IFtpLogger
        {
            private readonly SessionLog _log;

            public LogAdapter(SessionLog log) { _log = log; }

            public void Log(FtpLogEntry entry)
            {
                var message = entry.Message ?? string.Empty;
                foreach (var raw in message.Split('\n'))
                {
                    var line = raw.TrimEnd('\r');
                    if (line.Trim().Length == 0 || line.StartsWith(">", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("Command:", StringComparison.Ordinal)) _log.Send(line.Substring(8).Trim());
                    else if (line.StartsWith("Response:", StringComparison.Ordinal)) _log.Receive(line.Substring(9).Trim());
                    else if (line.StartsWith("Status:", StringComparison.Ordinal)) _log.Status(line.Substring(7).Trim());
                    else if (entry.Severity == FtpTraceLevel.Error) _log.Error(line.Trim());
                    else if (entry.Severity == FtpTraceLevel.Warn) _log.Warning(line.Trim());
                    else if (entry.Severity == FtpTraceLevel.Info) _log.Status(line.Trim());
                }
                if (entry.Exception != null && entry.Severity == FtpTraceLevel.Error) _log.Error(entry.Exception.Message);
            }
        }
    }
}
