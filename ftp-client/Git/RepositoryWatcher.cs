using System;
using System.ComponentModel;
using System.IO;

namespace FtpClient.Git
{
    /// <summary>
    /// Watches a repository's git directory and says when its commits may have moved on: a commit, a
    /// checkout, a pull, a fetch or a rebase, made here or in another program. Object files, locks and
    /// the index churn during every git command and are ignored, so the event means "read it again",
    /// not "something was written".
    /// </summary>
    public sealed class RepositoryWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;

        private RepositoryWatcher(string gitDirectory, FileSystemWatcher watcher)
        {
            GitDirectory = gitDirectory;
            _watcher = watcher;
        }

        /// <summary>Raised on the synchronizing object's thread when HEAD or a ref changed.</summary>
        public event EventHandler Changed;

        public string GitDirectory { get; }

        /// <summary>
        /// Starts watching <paramref name="gitDirectory"/>, raising <see cref="Changed"/> on
        /// <paramref name="synchronizingObject"/>'s thread. Null when it cannot be watched, e.g. a
        /// repository on a share that does not report changes.
        /// </summary>
        public static RepositoryWatcher Start(string gitDirectory, ISynchronizeInvoke synchronizingObject)
        {
            if (string.IsNullOrEmpty(gitDirectory) || !Directory.Exists(gitDirectory)) return null;
            FileSystemWatcher watcher = null;
            try
            {
                watcher = new FileSystemWatcher(gitDirectory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    InternalBufferSize = 64 * 1024,
                    SynchronizingObject = synchronizingObject,
                };
                var result = new RepositoryWatcher(gitDirectory, watcher);
                watcher.Changed += result.OnFileEvent;
                watcher.Created += result.OnFileEvent;
                watcher.Deleted += result.OnFileEvent;
                watcher.Renamed += (s, e) => result.OnFileEvent(s, e);
                // A flooded buffer drops events, so treat that as a change and read the repository again.
                watcher.Error += (s, e) => result.Changed?.Invoke(result, EventArgs.Empty);
                watcher.EnableRaisingEvents = true;
                return result;
            }
            catch (Exception)
            {
                watcher?.Dispose();
                return null;
            }
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            bool matters = Matters(e.FullPath) || (e is RenamedEventArgs renamed && Matters(renamed.OldFullPath));
            if (matters) Changed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>HEAD, the refs and their logs move when commits do; everything else is noise.</summary>
        private static bool Matters(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            // Git writes a lock first and renames it into place; the rename is the event that counts.
            if (path.EndsWith(".lock", StringComparison.OrdinalIgnoreCase)) return false;
            if (path.IndexOf("\\refs\\", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (path.IndexOf("\\logs\\", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            var name = Path.GetFileName(path);
            return string.Equals(name, "HEAD", StringComparison.Ordinal)
                || string.Equals(name, "ORIG_HEAD", StringComparison.Ordinal)
                || string.Equals(name, "MERGE_HEAD", StringComparison.Ordinal)
                || string.Equals(name, "packed-refs", StringComparison.Ordinal);
        }

        public void Dispose()
        {
            var watcher = _watcher;
            _watcher = null;
            if (watcher == null) return;
            try
            {
                watcher.EnableRaisingEvents = false;
                watcher.SynchronizingObject = null;
                watcher.Dispose();
            }
            catch (Exception)
            {
                // A watcher that is already gone needs nothing.
            }
        }
    }
}
