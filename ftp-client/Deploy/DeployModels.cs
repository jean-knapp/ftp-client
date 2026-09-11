using System;
using FtpClient.Git;

namespace FtpClient.Deploy
{
    /// <summary>Where a commit stands against the server.</summary>
    public enum DeployState
    {
        /// <summary>Older than the pairing; nothing is claimed about it.</summary>
        None,
        /// <summary>Not uploaded yet, or only partly.</summary>
        Pending,
        /// <summary>Its files are in the transfer queue right now.</summary>
        Deploying,
        /// <summary>Every file reached the server.</summary>
        Deployed,
    }

    /// <summary>What uploading a commit does to one of its files.</summary>
    public enum DeployAction
    {
        Overwrite,
        New,
        Delete,
        Ignored,
    }

    /// <summary>One row of the Commits list.</summary>
    public sealed class CommitEntry
    {
        public CommitInfo Commit { get; set; }
        public int FileCount { get; set; }
        public int Added { get; set; }
        public int Removed { get; set; }
        public DeployState State { get; set; }
    }

    /// <summary>One file of the selected commit, resolved to where it lands on the server.</summary>
    public sealed class DeployFile
    {
        /// <summary>Path in the repository, with forward slashes.</summary>
        public string RepoPath { get; set; }

        /// <summary>The path before a rename, whose remote copy is removed.</summary>
        public string OldRepoPath { get; set; }

        /// <summary>Absolute remote path; null when the file maps nowhere.</summary>
        public string RemotePath { get; set; }

        public string OldRemotePath { get; set; }

        public FileChangeKind Kind { get; set; }

        /// <summary>Blob size at the commit; null for deletions.</summary>
        public long? Size { get; set; }

        public DeployAction Action { get; set; }

        /// <summary>Ticked in the list; ignored files start unticked and cannot be ticked.</summary>
        public bool Included { get; set; } = true;

        /// <summary>Why the file is ignored, shown in place of its remote target.</summary>
        public string Note { get; set; }

        public string FileName
        {
            get
            {
                var path = RepoPath ?? string.Empty;
                int slash = path.LastIndexOf('/');
                return slash < 0 ? path : path.Substring(slash + 1);
            }
        }

        public bool CanInclude => Action != DeployAction.Ignored;
    }
}
