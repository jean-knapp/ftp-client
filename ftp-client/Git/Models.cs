using System;
using System.Collections.Generic;

namespace FtpClient.Git
{
    public enum RefKind
    {
        LocalBranch,
        RemoteBranch,
        Tag,
        DetachedHead,
    }

    /// <summary>A branch, tag or detached HEAD marker pointing at a commit.</summary>
    public sealed class RefInfo
    {
        /// <summary>Full ref name, e.g. <c>refs/heads/main</c>.</summary>
        public string FullName { get; set; }

        /// <summary>Display name: <c>main</c>, <c>origin/main</c>, <c>v1.0</c>.</summary>
        public string Name { get; set; }

        public RefKind Kind { get; set; }

        /// <summary>Commit the ref points at (annotated tags are peeled).</summary>
        public string Sha { get; set; }

        /// <summary>True for the checked-out branch.</summary>
        public bool IsHead { get; set; }

        /// <summary>Tracking branch such as <c>origin/main</c>, or null.</summary>
        public string Upstream { get; set; }

        public int Ahead { get; set; }
        public int Behind { get; set; }
        public bool UpstreamGone { get; set; }

        /// <summary>Remote name for remote branches (<c>origin</c>).</summary>
        public string Remote { get; set; }

        /// <summary>Branch name without the remote prefix.</summary>
        public string ShortName
        {
            get
            {
                if (Kind == RefKind.RemoteBranch && Remote != null && Name.StartsWith(Remote + "/", StringComparison.Ordinal))
                    return Name.Substring(Remote.Length + 1);
                return Name;
            }
        }

        public override string ToString() => Name;
    }

    public sealed class CommitInfo
    {
        public string Sha { get; set; }
        public string ShortSha => Sha != null && Sha.Length >= 7 ? Sha.Substring(0, 7) : Sha;
        public List<string> Parents { get; set; } = new List<string>();
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public DateTimeOffset AuthorDate { get; set; }
        public string CommitterName { get; set; }
        public DateTimeOffset CommitDate { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<RefInfo> Refs { get; } = new List<RefInfo>();
        public bool IsMerge => Parents.Count > 1;
        public bool IsRoot => Parents.Count == 0;

        public string FullMessage => string.IsNullOrEmpty(Body) ? Subject : Subject + "\n\n" + Body;

        public override string ToString() => ShortSha + " " + Subject;
    }

    public enum FileChangeKind
    {
        Modified,
        Added,
        Deleted,
        Renamed,
        Copied,
        TypeChanged,
        Untracked,
        Ignored,
        Conflicted,
        Unknown,
    }

    /// <summary>A changed file in the working tree, the index, or a commit.</summary>
    public sealed class FileChange
    {
        public string Path { get; set; }
        public string OldPath { get; set; }
        public FileChangeKind Kind { get; set; }

        /// <summary>True when this entry describes the index (staged) side.</summary>
        public bool Staged { get; set; }

        public string FileName => System.IO.Path.GetFileName(Path);

        public string StatusLetter
        {
            get
            {
                switch (Kind)
                {
                    case FileChangeKind.Modified: return "M";
                    case FileChangeKind.Added: return "A";
                    case FileChangeKind.Deleted: return "D";
                    case FileChangeKind.Renamed: return "R";
                    case FileChangeKind.Copied: return "C";
                    case FileChangeKind.TypeChanged: return "T";
                    case FileChangeKind.Untracked: return "?";
                    case FileChangeKind.Ignored: return "!";
                    case FileChangeKind.Conflicted: return "!";
                    default: return "?";
                }
            }
        }

        public static FileChangeKind KindFromLetter(char letter)
        {
            switch (letter)
            {
                case 'M': return FileChangeKind.Modified;
                case 'A': return FileChangeKind.Added;
                case 'D': return FileChangeKind.Deleted;
                case 'R': return FileChangeKind.Renamed;
                case 'C': return FileChangeKind.Copied;
                case 'T': return FileChangeKind.TypeChanged;
                case 'U': return FileChangeKind.Conflicted;
                case '?': return FileChangeKind.Untracked;
                case '!': return FileChangeKind.Ignored;
                default: return FileChangeKind.Unknown;
            }
        }

        public override string ToString() => StatusLetter + " " + Path;
    }

    public sealed class RepositoryStatus
    {
        /// <summary>Current branch, or null when HEAD is detached.</summary>
        public string Branch { get; set; }

        /// <summary>HEAD commit, or null for an unborn branch (no commits yet).</summary>
        public string HeadSha { get; set; }

        public string Upstream { get; set; }
        public int Ahead { get; set; }
        public int Behind { get; set; }
        public bool IsDetached { get; set; }
        public bool IsUnborn { get; set; }

        public List<FileChange> Staged { get; } = new List<FileChange>();

        /// <summary>Working-tree changes, including untracked and conflicted files.</summary>
        public List<FileChange> Unstaged { get; } = new List<FileChange>();

        public int ConflictCount { get; set; }
        public bool HasChanges => Staged.Count > 0 || Unstaged.Count > 0;
        public int TotalChanges => Staged.Count + Unstaged.Count;
    }

    public enum RepositoryState
    {
        None,
        Merging,
        Rebasing,
        CherryPicking,
        Reverting,
        Bisecting,
    }

    public enum ResetMode
    {
        Soft,
        Mixed,
        Hard,
    }

    public enum PullMode
    {
        Default,
        FastForwardOnly,
        Rebase,
    }

    /// <summary>A configured remote and the URLs git fetches from and pushes to.</summary>
    public sealed class RemoteInfo
    {
        public string Name { get; set; }
        public string FetchUrl { get; set; }
        public string PushUrl { get; set; }

        public override string ToString() => Name;
    }

    public sealed class StashInfo
    {
        public int Index { get; set; }
        public string Selector => "stash@{" + Index + "}";
        public string Sha { get; set; }
        public string Message { get; set; }
        public DateTimeOffset Date { get; set; }

        public override string ToString() => Selector + ": " + Message;
    }

    /// <summary>Added and removed line counts for one file, from <c>git diff --numstat</c>.</summary>
    public struct LineDelta
    {
        public LineDelta(int added, int removed)
        {
            Added = added;
            Removed = removed;
        }

        public int Added { get; }
        public int Removed { get; }
    }

    public enum DiffLineKind
    {
        Info,
        HunkHeader,
        Context,
        Added,
        Removed,
        NoNewline,
    }

    public sealed class DiffLine
    {
        public DiffLineKind Kind { get; set; }
        public string Text { get; set; }
        public int OldLineNumber { get; set; } = -1;
        public int NewLineNumber { get; set; } = -1;
    }

    public sealed class DiffDocument
    {
        public string Path { get; set; }
        public bool IsBinary { get; set; }
        public string Note { get; set; }
        public List<DiffLine> Lines { get; } = new List<DiffLine>();
        public int Additions { get; set; }
        public int Deletions { get; set; }
        public bool IsEmpty => Lines.Count == 0;
    }
}
