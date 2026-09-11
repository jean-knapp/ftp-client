using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Remote
{
    /// <summary>
    /// A folder on this machine - a local directory or a mounted share - browsed through the same
    /// contract as a server. <see cref="Site.Host"/> holds the folder, which becomes <c>/</c>.
    /// Useful in its own right for network drives, and it lets the whole transfer path run
    /// without a server.
    /// </summary>
    public sealed class LocalFolderSession : IRemoteSession
    {
        private const int BufferSize = 81920;
        private readonly SessionLog _log;
        private string _root;

        public LocalFolderSession(Site site, SessionLog log)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            _log = log ?? new SessionLog();
        }

        public Site Site { get; }
        public bool IsConnected { get; private set; }
        public string SecurityDescription => "local file system";
        public bool SupportsResume => true;
        public bool SupportsRawCommands => false;

        public event EventHandler Disconnected;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var root = Site.Host;
                if (string.IsNullOrWhiteSpace(root)) throw new RemoteException("No folder was given for this site.", RemoteErrorKind.Connection);
                root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(root.Trim()));
                if (!Directory.Exists(root)) throw new RemoteException(root + " does not exist or cannot be reached.", RemoteErrorKind.Connection);
                _root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                IsConnected = true;
                _log.Status("Opened " + _root);
            }, cancellationToken);
        }

        public Task DisconnectAsync()
        {
            if (IsConnected)
            {
                IsConnected = false;
                _log.Status("Closed " + _root);
                Disconnected?.Invoke(this, EventArgs.Empty);
            }
            return Task.CompletedTask;
        }

        public Task KeepAliveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string> GetHomeDirectoryAsync(CancellationToken cancellationToken) => Task.FromResult(RemotePath.Root);

        public Task<IReadOnlyList<RemoteEntry>> ListAsync(string path, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var directory = new DirectoryInfo(ToLocal(path));
                if (!directory.Exists) throw new RemoteException(RemotePath.Normalize(path) + ": no such directory", RemoteErrorKind.NotFound);
                var entries = new List<RemoteEntry>();
                foreach (var info in directory.EnumerateFileSystemInfos())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entries.Add(ToEntry(info, RemotePath.Combine(path, info.Name)));
                }
                _log.Status("Listed " + RemotePath.Normalize(path) + " - " + entries.Count + (entries.Count == 1 ? " item" : " items"));
                return (IReadOnlyList<RemoteEntry>)entries;
            });
        }

        public Task<RemoteEntry> StatAsync(string path, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var local = ToLocal(path);
                if (Directory.Exists(local)) return ToEntry(new DirectoryInfo(local), RemotePath.Normalize(path));
                if (File.Exists(local)) return ToEntry(new FileInfo(local), RemotePath.Normalize(path));
                return null;
            });
        }

        public Task CreateDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var local = ToLocal(path);
                if (File.Exists(local) || Directory.Exists(local)) throw new RemoteException(RemotePath.Normalize(path) + " already exists", RemoteErrorKind.AlreadyExists);
                Directory.CreateDirectory(local);
                _log.Status("Created directory " + RemotePath.Normalize(path));
                return true;
            });
        }

        public Task DeleteFileAsync(string path, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var local = ToLocal(path);
                if (Directory.Exists(local) && (File.GetAttributes(local) & FileAttributes.ReparsePoint) != 0)
                {
                    // A link to a folder goes like a file: the link is removed, never the folder behind it.
                    Directory.Delete(local, false);
                    _log.Status("Deleted link " + RemotePath.Normalize(path));
                    return true;
                }
                if (!File.Exists(local)) throw new RemoteException(RemotePath.Normalize(path) + ": no such file", RemoteErrorKind.NotFound);
                File.SetAttributes(local, FileAttributes.Normal);
                File.Delete(local);
                _log.Status("Deleted " + RemotePath.Normalize(path));
                return true;
            });
        }

        public Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var local = ToLocal(path);
                if (RemotePath.Normalize(path) == RemotePath.Root) throw new RemoteException("The root folder cannot be deleted.", RemoteErrorKind.PermissionDenied);
                if (!Directory.Exists(local)) throw new RemoteException(RemotePath.Normalize(path) + ": no such directory", RemoteErrorKind.NotFound);
                Directory.Delete(local, false);
                _log.Status("Removed directory " + RemotePath.Normalize(path));
                return true;
            });
        }

        public Task RenameAsync(string fromPath, string toPath, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var from = ToLocal(fromPath);
                var to = ToLocal(toPath);
                if (File.Exists(to) || Directory.Exists(to)) throw new RemoteException(RemotePath.Normalize(toPath) + " already exists", RemoteErrorKind.AlreadyExists);
                if (Directory.Exists(from)) Directory.Move(from, to);
                else if (File.Exists(from)) File.Move(from, to);
                else throw new RemoteException(RemotePath.Normalize(fromPath) + ": no such file or directory", RemoteErrorKind.NotFound);
                _log.Status("Renamed " + RemotePath.Normalize(fromPath) + " to " + RemotePath.Normalize(toPath));
                return true;
            });
        }

        public Task CreateSymlinkAsync(string target, string linkPath, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var link = ToLocal(linkPath);
                if (File.Exists(link) || Directory.Exists(link)) throw new RemoteException(RemotePath.Normalize(linkPath) + " already exists", RemoteErrorKind.AlreadyExists);
                if (string.IsNullOrWhiteSpace(target)) throw new RemoteException("The link needs a target.", RemoteErrorKind.General);

                // An absolute target is a path inside this folder, stored as the full local path;
                // a relative one is stored as it is and resolved from the link's directory.
                bool absolute = target.StartsWith("/", StringComparison.Ordinal);
                var stored = absolute ? ToLocal(target) : target.Replace('/', '\\');
                var resolved = absolute ? stored : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(link), stored));
                bool directory = Directory.Exists(resolved);
                if (!directory && !File.Exists(resolved)) throw new RemoteException(target + ": no such file or directory", RemoteErrorKind.NotFound);

                if (!TryCreateSymbolicLink(link, stored, directory, out int error))
                {
                    // Without Developer Mode or elevation Windows refuses symbolic links; a junction
                    // needs neither, but only joins folders and always stores the full path.
                    if (error == ErrorPrivilegeNotHeld && directory) CreateJunction(link, resolved);
                    else throw new RemoteException(RemotePath.Normalize(linkPath) + ": " + new System.ComponentModel.Win32Exception(error).Message,
                        error == ErrorPrivilegeNotHeld || error == ErrorAccessDenied ? RemoteErrorKind.PermissionDenied : RemoteErrorKind.General);
                }
                _log.Status("Linked " + RemotePath.Normalize(linkPath) + " to " + target);
                return true;
            });
        }

        private const int SymbolicLinkFlagDirectory = 0x1;
        private const int SymbolicLinkFlagAllowUnprivilegedCreate = 0x2;
        private const int ErrorAccessDenied = 5;
        private const int ErrorInvalidParameter = 87;
        private const int ErrorPrivilegeNotHeld = 1314;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        private static extern bool CreateSymbolicLink(string symlinkFileName, string targetFileName, int flags);

        private static bool TryCreateSymbolicLink(string link, string target, bool directory, out int error)
        {
            int flags = directory ? SymbolicLinkFlagDirectory : 0;
            error = 0;
            if (CreateSymbolicLink(link, target, flags | SymbolicLinkFlagAllowUnprivilegedCreate)) return true;
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            // Windows before 10 1703 does not know the unprivileged flag.
            if (error != ErrorInvalidParameter) return false;
            if (CreateSymbolicLink(link, target, flags)) return true;
            error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return false;
        }

        private static void CreateJunction(string link, string target)
        {
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using (var process = System.Diagnostics.Process.Start(start))
            {
                var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0 || !Directory.Exists(link))
                {
                    throw new RemoteException("Windows refused to create the link. Turn on Developer Mode or run as administrator. " + output.Trim(), RemoteErrorKind.PermissionDenied);
                }
            }
        }

        public Task SetPermissionsAsync(string path, int mode, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                // Windows only knows read-only; it follows the owner's write bit.
                var local = ToLocal(path);
                if (!File.Exists(local)) return true;
                var attributes = File.GetAttributes(local);
                attributes = (mode & 0x80) == 0 ? attributes | FileAttributes.ReadOnly : attributes & ~FileAttributes.ReadOnly;
                File.SetAttributes(local, attributes);
                _log.Status("Permissions of " + RemotePath.Normalize(path) + " set to " + Permissions.Octal(mode));
                return true;
            });
        }

        public Task SetModifiedTimeAsync(string path, DateTime modifiedUtc, CancellationToken cancellationToken)
        {
            return Run(() =>
            {
                var local = ToLocal(path);
                if (File.Exists(local)) File.SetLastWriteTimeUtc(local, modifiedUtc);
                else if (Directory.Exists(local)) Directory.SetLastWriteTimeUtc(local, modifiedUtc);
                return true;
            });
        }

        public async Task UploadAsync(Stream source, string remotePath, bool append, IProgress<long> bytesSent, CancellationToken cancellationToken)
        {
            var local = ToLocal(remotePath);
            _log.Status((append ? "Appending to " : "Writing ") + RemotePath.Normalize(remotePath));
            try
            {
                using (var target = new FileStream(local, append ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                {
                    await CopyAsync(source, target, bytesSent, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw Translate(ex, remotePath);
            }
        }

        public async Task DownloadAsync(string remotePath, Stream destination, long offset, IProgress<long> bytesReceived, CancellationToken cancellationToken)
        {
            var local = ToLocal(remotePath);
            _log.Status("Reading " + RemotePath.Normalize(remotePath) + (offset > 0 ? " from byte " + offset.ToString("N0") : string.Empty));
            try
            {
                using (var source = new FileStream(local, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, BufferSize, true))
                {
                    if (offset > 0) source.Seek(offset, SeekOrigin.Begin);
                    await CopyAsync(source, destination, bytesReceived, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                throw Translate(ex, remotePath);
            }
        }

        public Task<string> SendRawCommandAsync(string command, CancellationToken cancellationToken)
        {
            throw new RemoteException("A local folder has no protocol to send commands to.", RemoteErrorKind.NotSupported);
        }

        public void Dispose()
        {
            IsConnected = false;
        }

        // ------------------------------------------------------------------ helpers

        private string ToLocal(string remotePath)
        {
            if (!IsConnected) throw new RemoteException("Not connected.", RemoteErrorKind.Connection);
            var segments = RemotePath.Segments(remotePath);
            var local = segments.Length == 0 ? _root : Path.Combine(_root, Path.Combine(segments));
            // Normalize already folded "..", so this only guards against rooted segments.
            var full = Path.GetFullPath(local);
            if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            {
                throw new RemoteException(RemotePath.Normalize(remotePath) + " is outside the folder", RemoteErrorKind.PermissionDenied);
            }
            return full;
        }

        private RemoteEntry ToEntry(FileSystemInfo info, string remotePath)
        {
            bool directory = (info.Attributes & FileAttributes.Directory) != 0;
            bool link = (info.Attributes & FileAttributes.ReparsePoint) != 0;
            bool readOnly = (info.Attributes & FileAttributes.ReadOnly) != 0;
            int mode = directory ? 0x1ED : readOnly ? 0x124 : 0x1A4;   // 755, 444, 644
            int? itemCount = null;
            string linkTarget = null;
            if (directory && !link)
            {
                try { itemCount = Directory.EnumerateFileSystemEntries(info.FullName).Count(); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return new RemoteEntry
            {
                Name = info.Name,
                FullPath = remotePath,
                Kind = link ? RemoteEntryKind.Symlink : directory ? RemoteEntryKind.Directory : RemoteEntryKind.File,
                Size = directory ? 0 : ((FileInfo)info).Length,
                Modified = info.LastWriteTimeUtc,
                Mode = mode,
                Owner = Environment.UserName,
                Group = "users",
                ItemCount = itemCount,
                LinkTarget = linkTarget,
                // A link to a folder carries the directory attribute of what it points to.
                LinksToDirectory = link ? directory : (bool?)null,
            };
        }

        private static async Task CopyAsync(Stream source, Stream target, IProgress<long> progress, CancellationToken cancellationToken)
        {
            var buffer = new byte[BufferSize];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await target.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                total += read;
                progress?.Report(total);
            }
        }

        /// <summary>
        /// Runs file-system work on the thread pool and throws its failure after the await, so the
        /// debugger does not stop on errors every caller handles.
        /// </summary>
        private async Task<T> Run<T>(Func<T> work)
        {
            var outcome = await Task.Run(() =>
            {
                try
                {
                    return new CallOutcome<T>(work(), null);
                }
                catch (Exception ex)
                {
                    return new CallOutcome<T>(default(T), ex);
                }
            }).ConfigureAwait(false);

            if (outcome.Error == null) return outcome.Value;
            if (outcome.Error is OperationCanceledException) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(outcome.Error).Throw();
            var translated = Translate(outcome.Error, null);
            if (translated.Kind != RemoteErrorKind.AlreadyExists) _log.Error(translated.Message);
            throw translated;
        }

        private static RemoteException Translate(Exception ex, string remotePath)
        {
            var prefix = remotePath == null ? string.Empty : RemotePath.Normalize(remotePath) + ": ";
            if (ex is RemoteException remote) return remote;
            if (ex is FileNotFoundException || ex is DirectoryNotFoundException) return new RemoteException(prefix + "no such file or directory", RemoteErrorKind.NotFound, ex);
            if (ex is UnauthorizedAccessException) return new RemoteException(prefix + "permission denied", RemoteErrorKind.PermissionDenied, ex);
            return new RemoteException(prefix + ex.Message, RemoteErrorKind.General, ex);
        }
    }
}
