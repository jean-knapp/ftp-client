using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Remote
{
    /// <summary>
    /// One connection to a server. The browser holds one; the transfer queue opens more per site,
    /// because neither FTP nor SFTP allows two transfers on the same connection at once. Paths are
    /// absolute POSIX paths. Every member may throw <see cref="RemoteException"/>.
    /// </summary>
    public interface IRemoteSession : IDisposable
    {
        Site Site { get; }

        bool IsConnected { get; }

        /// <summary>How the connection is secured, for the status bar (e.g. "ed25519 key · fingerprint verified").</summary>
        string SecurityDescription { get; }

        /// <summary>REST / append: an interrupted upload can continue from a byte offset.</summary>
        bool SupportsResume { get; }

        /// <summary>Raw protocol commands can be sent from the log window.</summary>
        bool SupportsRawCommands { get; }

        /// <summary>Raised when the server or the network drops the connection.</summary>
        event EventHandler Disconnected;

        Task ConnectAsync(CancellationToken cancellationToken);

        Task DisconnectAsync();

        /// <summary>Sends a no-op so idle connections are not closed by the server.</summary>
        Task KeepAliveAsync(CancellationToken cancellationToken);

        /// <summary>The directory the server puts the user in after login.</summary>
        Task<string> GetHomeDirectoryAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<RemoteEntry>> ListAsync(string path, CancellationToken cancellationToken);

        /// <summary>The entry at a path, or null when nothing is there.</summary>
        Task<RemoteEntry> StatAsync(string path, CancellationToken cancellationToken);

        Task CreateDirectoryAsync(string path, CancellationToken cancellationToken);

        Task DeleteFileAsync(string path, CancellationToken cancellationToken);

        Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken);

        Task RenameAsync(string fromPath, string toPath, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a symbolic link at <paramref name="linkPath"/> pointing to <paramref name="target"/>.
        /// The target is stored as given: an absolute path, or one relative to the link's directory.
        /// </summary>
        Task CreateSymlinkAsync(string target, string linkPath, CancellationToken cancellationToken);

        Task SetPermissionsAsync(string path, int mode, CancellationToken cancellationToken);

        Task SetModifiedTimeAsync(string path, DateTime modifiedUtc, CancellationToken cancellationToken);

        /// <summary>
        /// Writes <paramref name="source"/> to <paramref name="remotePath"/>. When
        /// <paramref name="append"/> is set the stream is added to the end of the existing file;
        /// the caller has already positioned it at the resume offset.
        /// </summary>
        Task UploadAsync(Stream source, string remotePath, bool append, IProgress<long> bytesSent, CancellationToken cancellationToken);

        /// <summary>Copies the remote file into <paramref name="destination"/>, starting at <paramref name="offset"/>.</summary>
        Task DownloadAsync(string remotePath, Stream destination, long offset, IProgress<long> bytesReceived, CancellationToken cancellationToken);

        /// <summary>Sends a protocol command verbatim and returns the server's reply.</summary>
        Task<string> SendRawCommandAsync(string command, CancellationToken cancellationToken);
    }
}
