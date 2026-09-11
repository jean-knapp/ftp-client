using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FtpClient.Git
{
    /// <summary>High-level, asynchronous git operations for one working directory.</summary>
    public sealed class GitRepository
    {
        /// <summary>The well-known empty tree, used to diff a root commit.</summary>
        private const string EmptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";

        private static readonly string[] CommonArguments = { "--no-pager", "-c", "core.quotepath=false", "-c", "color.ui=false" };

        /// <summary>Environment that suppresses any editor git might try to open (rebase/cherry-pick --continue).</summary>
        private static readonly Dictionary<string, string> NoEditorEnvironment = new Dictionary<string, string>
        {
            { "GIT_EDITOR", "true" },
            { "GIT_SEQUENCE_EDITOR", "true" },
        };

        public GitRepository(string workingDirectory)
        {
            WorkingDirectory = Path.GetFullPath(workingDirectory).TrimEnd('\\', '/');
            Name = Path.GetFileName(WorkingDirectory);
            if (string.IsNullOrEmpty(Name)) Name = WorkingDirectory;
        }

        public string WorkingDirectory { get; }
        public string Name { get; }

        // ------------------------------------------------------------------ static helpers

        /// <summary>Returns the repository root containing <paramref name="path"/>, or null when it is not inside a git work tree.</summary>
        public static async Task<string> DiscoverAsync(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return null;
            var result = await GitRunner.RunAsync(path, "rev-parse", "--show-toplevel").ConfigureAwait(false);
            if (!result.Succeeded) return null;
            var top = result.StandardOutput.Trim();
            if (top.Length == 0) return null;
            return Path.GetFullPath(top.Replace('/', Path.DirectorySeparatorChar));
        }

        public static Task<GitResult> InitAsync(string path)
        {
            Directory.CreateDirectory(path);
            return GitRunner.RunAsync(path, "init");
        }

        public static Task<GitResult> CloneAsync(string url, string destination, bool recurseSubmodules, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var parent = Path.GetDirectoryName(Path.GetFullPath(destination));
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            var args = new List<string> { "clone", "--progress" };
            if (recurseSubmodules) args.Add("--recurse-submodules");
            args.Add(url);
            args.Add(destination);
            return GitRunner.RunAsync(parent, args, new GitRunOptions { Progress = progress }, cancellationToken);
        }

        public static Task<string> GetGlobalConfigAsync(string key) => GetConfigValueAsync(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), key, true);

        /// <summary>Reads a machine-wide config value, e.g. a credential helper set by the installer.</summary>
        public static async Task<string> GetSystemConfigAsync(string key)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var result = await GitRunner.RunAsync(home, "config", "--system", "--get", key).ConfigureAwait(false);
            if (!result.Succeeded) return null;
            var value = result.StandardOutput.Trim();
            return value.Length == 0 ? null : value;
        }

        public static async Task SetGlobalConfigAsync(string key, string value)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var result = await GitRunner.RunAsync(home, "config", "--global", key, value ?? string.Empty).ConfigureAwait(false);
            result.ThrowIfFailed();
        }

        private static async Task<string> GetConfigValueAsync(string directory, string key, bool global)
        {
            var args = global ? new[] { "config", "--global", "--get", key } : new[] { "config", "--get", key };
            var result = await GitRunner.RunAsync(directory, args).ConfigureAwait(false);
            if (!result.Succeeded) return null;
            var value = result.StandardOutput.Trim();
            return value.Length == 0 ? null : value;
        }

        // ------------------------------------------------------------------ plumbing

        private Task<GitResult> RunAsync(params string[] args) => RunAsync(null, CancellationToken.None, args);

        private Task<GitResult> RunAsync(GitRunOptions options, CancellationToken cancellationToken, params string[] args)
        {
            var all = new List<string>(CommonArguments.Length + args.Length);
            all.AddRange(CommonArguments);
            all.AddRange(args);
            return GitRunner.RunAsync(WorkingDirectory, all, options, cancellationToken);
        }

        private async Task<string> QueryAsync(params string[] args)
        {
            var result = await RunAsync(args).ConfigureAwait(false);
            result.ThrowIfFailed();
            return result.StandardOutput;
        }

        private async Task<GitResult> RunNoEditorAsync(params string[] args)
        {
            return await RunAsync(new GitRunOptions { Environment = NoEditorEnvironment }, CancellationToken.None, args).ConfigureAwait(false);
        }

        // ------------------------------------------------------------------ queries

        public async Task<string> GetGitDirectoryAsync()
        {
            var dir = (await QueryAsync("rev-parse", "--git-dir").ConfigureAwait(false)).Trim();
            if (!Path.IsPathRooted(dir)) dir = Path.Combine(WorkingDirectory, dir);
            return Path.GetFullPath(dir);
        }

        public async Task<RepositoryState> GetStateAsync()
        {
            string gitDir;
            try { gitDir = await GetGitDirectoryAsync().ConfigureAwait(false); }
            catch (GitException) { return RepositoryState.None; }

            if (Directory.Exists(Path.Combine(gitDir, "rebase-merge")) || Directory.Exists(Path.Combine(gitDir, "rebase-apply"))) return RepositoryState.Rebasing;
            if (File.Exists(Path.Combine(gitDir, "MERGE_HEAD"))) return RepositoryState.Merging;
            if (File.Exists(Path.Combine(gitDir, "CHERRY_PICK_HEAD"))) return RepositoryState.CherryPicking;
            if (File.Exists(Path.Combine(gitDir, "REVERT_HEAD"))) return RepositoryState.Reverting;
            if (File.Exists(Path.Combine(gitDir, "BISECT_LOG"))) return RepositoryState.Bisecting;
            return RepositoryState.None;
        }

        public async Task<bool> HasHeadAsync()
        {
            var result = await RunAsync("rev-parse", "--verify", "-q", "HEAD").ConfigureAwait(false);
            return result.Succeeded;
        }

        public async Task<string> GetHeadShaAsync()
        {
            var result = await RunAsync("rev-parse", "--verify", "-q", "HEAD").ConfigureAwait(false);
            return result.Succeeded ? result.StandardOutput.Trim() : null;
        }

        /// <summary>Current branch name, or null when detached / unborn without a symbolic ref.</summary>
        public async Task<string> GetCurrentBranchAsync()
        {
            var result = await RunAsync("symbolic-ref", "--short", "-q", "HEAD").ConfigureAwait(false);
            if (!result.Succeeded) return null;
            var name = result.StandardOutput.Trim();
            return name.Length == 0 ? null : name;
        }

        public async Task<List<RefInfo>> GetRefsAsync()
        {
            var output = await QueryAsync("for-each-ref", "--format=" + GitParsers.RefFormat, "refs/heads", "refs/remotes", "refs/tags").ConfigureAwait(false);
            var refs = GitParsers.ParseRefs(output);

            var branch = await GetCurrentBranchAsync().ConfigureAwait(false);
            if (branch == null)
            {
                var head = await GetHeadShaAsync().ConfigureAwait(false);
                if (head != null)
                {
                    refs.Insert(0, new RefInfo { FullName = "HEAD", Name = "HEAD", Kind = RefKind.DetachedHead, Sha = head, IsHead = true });
                }
            }
            return refs;
        }

        public async Task<List<CommitInfo>> GetLogAsync(int maxCount, bool allBranches = true)
        {
            var args = new List<string> { "log", "--date-order", "--max-count=" + maxCount, "--format=" + GitParsers.LogFormat };
            if (allBranches)
            {
                args.Add("--branches");
                args.Add("--remotes");
                args.Add("--tags");
            }

            if (!await HasHeadAsync().ConfigureAwait(false))
            {
                // Unborn branch: there may still be remote/tag refs (e.g. right after cloning an empty repository).
                if (!allBranches) return new List<CommitInfo>();
                var probe = await RunAsync(args.ToArray()).ConfigureAwait(false);
                return probe.Succeeded ? GitParsers.ParseLog(probe.StandardOutput) : new List<CommitInfo>();
            }

            args.Add("HEAD");
            var output = await QueryAsync(args.ToArray()).ConfigureAwait(false);
            return GitParsers.ParseLog(output);
        }

        public async Task<RepositoryStatus> GetStatusAsync()
        {
            var output = await QueryAsync("status", "--porcelain=v2", "-z", "--branch", "--untracked-files=all").ConfigureAwait(false);
            return GitParsers.ParseStatus(output);
        }

        public async Task<List<StashInfo>> GetStashesAsync()
        {
            var result = await RunAsync("stash", "list", "--format=" + GitParsers.StashFormat).ConfigureAwait(false);
            return result.Succeeded ? GitParsers.ParseStashList(result.StandardOutput) : new List<StashInfo>();
        }

        public async Task<List<string>> GetRemotesAsync()
        {
            var result = await RunAsync("remote").ConfigureAwait(false);
            if (!result.Succeeded) return new List<string>();
            return result.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        }

        /// <summary>Every remote with its fetch and push URLs.</summary>
        public async Task<List<RemoteInfo>> GetRemoteListAsync()
        {
            var result = await RunAsync("remote", "-v").ConfigureAwait(false);
            var list = new List<RemoteInfo>();
            if (!result.Succeeded) return list;
            foreach (var line in result.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                // name<TAB>url (fetch|push)
                var tab = line.IndexOf('\t');
                if (tab <= 0) continue;
                var name = line.Substring(0, tab).Trim();
                var rest = line.Substring(tab + 1).Trim();
                bool push = rest.EndsWith("(push)", StringComparison.Ordinal);
                int space = rest.LastIndexOf(" (", StringComparison.Ordinal);
                var url = space > 0 ? rest.Substring(0, space).Trim() : rest;

                var remote = list.Find(r => string.Equals(r.Name, name, StringComparison.Ordinal));
                if (remote == null)
                {
                    remote = new RemoteInfo { Name = name };
                    list.Add(remote);
                }
                if (push) remote.PushUrl = url;
                else remote.FetchUrl = url;
            }
            return list;
        }

        public Task<GitResult> AddRemoteAsync(string name, string url) => RunAsync("remote", "add", name, url);

        public Task<GitResult> SetRemoteUrlAsync(string name, string url) => RunAsync("remote", "set-url", name, url);

        public Task<GitResult> RenameRemoteAsync(string oldName, string newName) => RunAsync("remote", "rename", oldName, newName);

        public Task<GitResult> RemoveRemoteAsync(string name) => RunAsync("remote", "remove", name);

        /// <summary>Points a local branch at <c>&lt;remote&gt;/&lt;branch&gt;</c>.</summary>
        public Task<GitResult> SetUpstreamAsync(string branch, string remote, string remoteBranch) =>
            RunAsync("branch", "--set-upstream-to=" + remote + "/" + (remoteBranch ?? branch), branch);

        public async Task<string> GetRemoteUrlAsync(string remote)
        {
            var result = await RunAsync("remote", "get-url", remote).ConfigureAwait(false);
            if (!result.Succeeded) return null;
            var url = result.StandardOutput.Trim();
            return url.Length == 0 ? null : url;
        }

        public Task<string> GetConfigAsync(string key) => GetConfigValueAsync(WorkingDirectory, key, false);

        public async Task SetConfigAsync(string key, string value)
        {
            (await RunAsync("config", key, value ?? string.Empty).ConfigureAwait(false)).ThrowIfFailed();
        }

        /// <summary>Full message of the HEAD commit (used to pre-fill the amend editor).</summary>
        public async Task<string> GetHeadMessageAsync()
        {
            var result = await RunAsync("log", "-1", "--format=%B").ConfigureAwait(false);
            return result.Succeeded ? result.StandardOutput.TrimEnd('\n', '\r') : string.Empty;
        }

        public async Task<List<string>> GetRecentSubjectsAsync(int count)
        {
            var result = await RunAsync("log", "--max-count=" + count, "--format=%s").ConfigureAwait(false);
            if (!result.Succeeded) return new List<string>();
            return result.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public async Task<bool> LocalBranchExistsAsync(string name)
        {
            var result = await RunAsync("show-ref", "--verify", "--quiet", "refs/heads/" + name).ConfigureAwait(false);
            return result.Succeeded;
        }

        // ------------------------------------------------------------------ staging & commits

        public async Task StageAsync(IEnumerable<string> paths)
        {
            var list = paths.ToList();
            if (list.Count == 0) return;
            var args = new List<string> { "add", "-A", "--" };
            args.AddRange(list);
            (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task StageAllAsync()
        {
            (await RunAsync("add", "-A").ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task UnstageAsync(IEnumerable<string> paths)
        {
            var list = paths.ToList();
            if (list.Count == 0) return;
            var args = new List<string>();
            if (await HasHeadAsync().ConfigureAwait(false))
            {
                args.Add("reset"); args.Add("-q"); args.Add("HEAD"); args.Add("--");
            }
            else
            {
                args.Add("rm"); args.Add("--cached"); args.Add("-r"); args.Add("-q"); args.Add("--");
            }
            args.AddRange(list);
            (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task UnstageAllAsync()
        {
            if (await HasHeadAsync().ConfigureAwait(false))
                (await RunAsync("reset", "-q", "HEAD", "--").ConfigureAwait(false)).ThrowIfFailed();
            else
                (await RunAsync("rm", "--cached", "-r", "-q", "--", ".").ConfigureAwait(false)).ThrowIfFailed();
        }

        /// <summary>Throws away working-tree changes for the given entries (untracked files are deleted).</summary>
        public async Task DiscardAsync(IEnumerable<FileChange> changes)
        {
            var tracked = new List<string>();
            var untracked = new List<string>();
            foreach (var c in changes)
            {
                if (c.Kind == FileChangeKind.Untracked) untracked.Add(c.Path); else tracked.Add(c.Path);
            }
            if (tracked.Count > 0)
            {
                var args = new List<string> { "checkout", "--" };
                args.AddRange(tracked);
                (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
            }
            if (untracked.Count > 0)
            {
                var args = new List<string> { "clean", "-f", "-q", "--" };
                args.AddRange(untracked);
                (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
            }
        }

        public async Task<string> CommitAsync(string message, bool amend)
        {
            var args = new List<string> { "commit", "-F", "-" };
            if (amend) args.Add("--amend");
            var result = await RunAsync(new GitRunOptions { StandardInput = message }, CancellationToken.None, args.ToArray()).ConfigureAwait(false);
            result.ThrowIfFailed();
            return await GetHeadShaAsync().ConfigureAwait(false);
        }

        // ------------------------------------------------------------------ diffs

        public async Task<DiffDocument> GetWorkingDiffAsync(FileChange change)
        {
            if (change.Kind == FileChangeKind.Untracked && !change.Staged)
            {
                return BuildUntrackedDiff(change.Path);
            }
            var args = new List<string> { "diff", "--no-color", "--no-ext-diff", "-M" };
            if (change.Staged) args.Add("--cached");
            args.Add("--");
            if (!string.IsNullOrEmpty(change.OldPath)) args.Add(change.OldPath);
            args.Add(change.Path);
            var result = await RunAsync(args.ToArray()).ConfigureAwait(false);
            if (!result.Succeeded && result.ExitCode != 1) throw new GitException(result);
            var doc = GitParsers.ParseDiff(result.StandardOutput, change.Path);
            if (doc.IsEmpty && change.Kind == FileChangeKind.Conflicted) doc.Note = "Conflicted file. Resolve the conflict markers, then stage the file.";
            return doc;
        }

        public async Task<DiffDocument> GetCommitFileDiffAsync(CommitInfo commit, FileChange change)
        {
            var args = new List<string> { "diff", "--no-color", "--no-ext-diff", "-M" };
            if (commit.Parents.Count > 0)
            {
                args.Add(commit.Parents[0]);
                args.Add(commit.Sha);
            }
            else
            {
                // Root commit: compare against the empty tree.
                args.Add(EmptyTreeSha);
                args.Add(commit.Sha);
            }
            args.Add("--");
            if (!string.IsNullOrEmpty(change.OldPath)) args.Add(change.OldPath);
            args.Add(change.Path);
            var result = await RunAsync(args.ToArray()).ConfigureAwait(false);
            if (!result.Succeeded && result.ExitCode != 1) throw new GitException(result);
            return GitParsers.ParseDiff(result.StandardOutput, change.Path);
        }

        public async Task<List<FileChange>> GetCommitFilesAsync(CommitInfo commit)
        {
            var args = new List<string> { "diff", "--name-status", "-z", "-M" };
            if (commit.Parents.Count > 0)
            {
                args.Add(commit.Parents[0]);
                args.Add(commit.Sha);
            }
            else
            {
                args.Add(EmptyTreeSha);
                args.Add(commit.Sha);
            }
            var output = await QueryAsync(args.ToArray()).ConfigureAwait(false);
            return GitParsers.ParseNameStatus(output);
        }

        /// <summary>Per-file added/removed line counts for the working tree or the index.</summary>
        public async Task<Dictionary<string, LineDelta>> GetWorkingNumstatAsync(bool staged)
        {
            var args = new List<string> { "diff", "--numstat", "-z", "-M" };
            if (staged) args.Add("--cached");
            var result = await RunAsync(args.ToArray()).ConfigureAwait(false);
            return result.Succeeded ? ParseNumstat(result.StandardOutput) : new Dictionary<string, LineDelta>(StringComparer.Ordinal);
        }

        /// <summary>Per-file added/removed line counts for one commit against its first parent.</summary>
        public async Task<Dictionary<string, LineDelta>> GetCommitNumstatAsync(CommitInfo commit)
        {
            var args = new List<string> { "diff", "--numstat", "-z", "-M" };
            args.Add(commit.Parents.Count > 0 ? commit.Parents[0] : EmptyTreeSha);
            args.Add(commit.Sha);
            var result = await RunAsync(args.ToArray()).ConfigureAwait(false);
            return result.Succeeded ? ParseNumstat(result.StandardOutput) : new Dictionary<string, LineDelta>(StringComparer.Ordinal);
        }

        /// <summary>
        /// Parses <c>--numstat -z</c>. Records are NUL separated; a rename spends two extra records
        /// on the old and new paths.
        /// </summary>
        private static Dictionary<string, LineDelta> ParseNumstat(string output)
        {
            var map = new Dictionary<string, LineDelta>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(output)) return map;
            var tokens = output.Split('\0');
            for (int i = 0; i < tokens.Length; i++)
            {
                var record = tokens[i];
                if (record.Length == 0) continue;
                var parts = record.Split('\t');
                if (parts.Length < 2) continue;

                int added, removed;
                if (!int.TryParse(parts[0], out added)) added = 0;
                if (!int.TryParse(parts[1], out removed)) removed = 0;

                string path;
                if (parts.Length >= 3 && parts[2].Length > 0)
                {
                    path = parts[2];
                }
                else if (i + 2 < tokens.Length)
                {
                    i++;            // old path
                    path = tokens[++i];
                }
                else continue;

                map[path] = new LineDelta(added, removed);
            }
            return map;
        }

        /// <summary>Resolves a conflicted file by taking one side, then stages it.</summary>
        public async Task ResolveConflictAsync(string path, bool ours)
        {
            (await RunAsync("checkout", ours ? "--ours" : "--theirs", "--", path).ConfigureAwait(false)).ThrowIfFailed();
            (await RunAsync("add", "--", path).ConfigureAwait(false)).ThrowIfFailed();
        }

        /// <summary>Unified diff of everything staged (fed to the commit-message composer).</summary>
        public async Task<string> GetStagedPatchAsync()
        {
            var result = await RunAsync("diff", "--cached", "--no-color", "--no-ext-diff", "-M", "--stat=120", "--patch").ConfigureAwait(false);
            return result.Succeeded ? result.StandardOutput : string.Empty;
        }

        private DiffDocument BuildUntrackedDiff(string relativePath)
        {
            var doc = new DiffDocument { Path = relativePath };
            var fullPath = Path.Combine(WorkingDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (Directory.Exists(fullPath))
                {
                    doc.Note = "Untracked directory.";
                    return doc;
                }
                var info = new FileInfo(fullPath);
                if (!info.Exists) { doc.Note = "File not found."; return doc; }
                if (info.Length > 4 * 1024 * 1024) { doc.Note = "File is too large to preview (" + (info.Length / 1024) + " KB)."; return doc; }
                var bytes = File.ReadAllBytes(fullPath);
                int probe = Math.Min(bytes.Length, 8000);
                for (int i = 0; i < probe; i++)
                {
                    if (bytes[i] == 0) { doc.IsBinary = true; doc.Note = "Binary file (" + info.Length + " bytes)."; return doc; }
                }
                var text = DecodeText(bytes);
                var lines = text.Split('\n');
                int count = lines.Length;
                if (count > 0 && lines[count - 1].Length == 0) count--;
                doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Info, Text = "new file (untracked)" });
                doc.Lines.Add(new DiffLine { Kind = DiffLineKind.HunkHeader, Text = "@@ -0,0 +1," + count + " @@" });
                for (int i = 0; i < count; i++)
                {
                    doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Added, Text = lines[i].TrimEnd('\r'), NewLineNumber = i + 1 });
                }
                doc.Additions = count;
            }
            catch (Exception ex)
            {
                doc.Note = ex.Message;
            }
            return doc;
        }

        private static string DecodeText(byte[] bytes)
        {
            try { return new UTF8Encoding(false, true).GetString(bytes); }
            catch (DecoderFallbackException) { return Encoding.Default.GetString(bytes); }
        }

        // ------------------------------------------------------------------ branches & tags

        public async Task CheckoutAsync(string target)
        {
            (await RunAsync("checkout", target).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task CheckoutDetachedAsync(string sha)
        {
            (await RunAsync("checkout", "--detach", sha).ConfigureAwait(false)).ThrowIfFailed();
        }

        /// <summary>Checks out a remote branch, creating a local tracking branch when needed.</summary>
        public async Task CheckoutRemoteBranchAsync(RefInfo remoteBranch)
        {
            var local = remoteBranch.ShortName;
            if (await LocalBranchExistsAsync(local).ConfigureAwait(false))
            {
                (await RunAsync("checkout", local).ConfigureAwait(false)).ThrowIfFailed();
                return;
            }
            (await RunAsync("checkout", "-b", local, "--track", remoteBranch.Name).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task CreateBranchAsync(string name, string startPoint, bool checkout)
        {
            var args = new List<string>();
            if (checkout) { args.Add("checkout"); args.Add("-b"); args.Add(name); }
            else { args.Add("branch"); args.Add(name); }
            if (!string.IsNullOrEmpty(startPoint)) args.Add(startPoint);
            (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task DeleteBranchAsync(string name, bool force)
        {
            (await RunAsync("branch", force ? "-D" : "-d", name).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task<GitResult> DeleteRemoteBranchAsync(string remote, string name)
        {
            return await RunAsync("push", remote, "--delete", name).ConfigureAwait(false);
        }

        public async Task RenameBranchAsync(string oldName, string newName)
        {
            (await RunAsync("branch", "-m", oldName, newName).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task CreateTagAsync(string name, string sha, string message)
        {
            var args = new List<string> { "tag" };
            if (!string.IsNullOrWhiteSpace(message)) { args.Add("-a"); args.Add("-m"); args.Add(message); }
            args.Add(name);
            if (!string.IsNullOrEmpty(sha)) args.Add(sha);
            (await RunAsync(args.ToArray()).ConfigureAwait(false)).ThrowIfFailed();
        }

        public async Task DeleteTagAsync(string name)
        {
            (await RunAsync("tag", "-d", name).ConfigureAwait(false)).ThrowIfFailed();
        }

        // ------------------------------------------------------------------ history surgery

        public Task<GitResult> MergeAsync(string target) => RunNoEditorAsync("merge", "--no-edit", target);

        public Task<GitResult> RebaseAsync(string onto) => RunNoEditorAsync("rebase", onto);

        public async Task<GitResult> CherryPickAsync(CommitInfo commit)
        {
            var args = new List<string> { "cherry-pick" };
            if (commit.IsMerge) { args.Add("-m"); args.Add("1"); }
            args.Add(commit.Sha);
            return await RunNoEditorAsync(args.ToArray()).ConfigureAwait(false);
        }

        public async Task<GitResult> RevertAsync(CommitInfo commit)
        {
            var args = new List<string> { "revert", "--no-edit" };
            if (commit.IsMerge) { args.Add("-m"); args.Add("1"); }
            args.Add(commit.Sha);
            return await RunNoEditorAsync(args.ToArray()).ConfigureAwait(false);
        }

        public async Task ResetAsync(string sha, ResetMode mode)
        {
            string flag;
            switch (mode)
            {
                case ResetMode.Soft: flag = "--soft"; break;
                case ResetMode.Hard: flag = "--hard"; break;
                default: flag = "--mixed"; break;
            }
            (await RunAsync("reset", flag, sha).ConfigureAwait(false)).ThrowIfFailed();
        }

        /// <summary>
        /// Removes a commit from the current branch. The HEAD commit is dropped with a reset; older commits are
        /// removed by replaying everything after them onto their parent (<c>git rebase --onto</c>).
        /// </summary>
        public async Task<GitResult> DropCommitAsync(CommitInfo commit, bool keepChangesForHead)
        {
            if (commit.IsRoot) throw new GitException("The root commit cannot be deleted.");
            var head = await GetHeadShaAsync().ConfigureAwait(false);
            if (string.Equals(head, commit.Sha, StringComparison.OrdinalIgnoreCase))
            {
                var flag = keepChangesForHead ? "--mixed" : "--hard";
                return await RunAsync("reset", flag, commit.Sha + "~1").ConfigureAwait(false);
            }
            return await RunNoEditorAsync("rebase", "--onto", commit.Sha + "~1", commit.Sha).ConfigureAwait(false);
        }

        public async Task<GitResult> ContinueOperationAsync(RepositoryState state)
        {
            switch (state)
            {
                case RepositoryState.Merging: return await RunNoEditorAsync("commit", "--no-edit").ConfigureAwait(false);
                case RepositoryState.Rebasing: return await RunNoEditorAsync("rebase", "--continue").ConfigureAwait(false);
                case RepositoryState.CherryPicking: return await RunNoEditorAsync("cherry-pick", "--continue").ConfigureAwait(false);
                case RepositoryState.Reverting: return await RunNoEditorAsync("revert", "--continue").ConfigureAwait(false);
                default: throw new GitException("No operation in progress.");
            }
        }

        public async Task<GitResult> AbortOperationAsync(RepositoryState state)
        {
            switch (state)
            {
                case RepositoryState.Merging: return await RunAsync("merge", "--abort").ConfigureAwait(false);
                case RepositoryState.Rebasing: return await RunAsync("rebase", "--abort").ConfigureAwait(false);
                case RepositoryState.CherryPicking: return await RunAsync("cherry-pick", "--abort").ConfigureAwait(false);
                case RepositoryState.Reverting: return await RunAsync("revert", "--abort").ConfigureAwait(false);
                case RepositoryState.Bisecting: return await RunAsync("bisect", "reset").ConfigureAwait(false);
                default: throw new GitException("No operation in progress.");
            }
        }

        // ------------------------------------------------------------------ remotes

        public Task<GitResult> FetchAsync(IProgress<string> progress, CancellationToken cancellationToken)
        {
            return RunAsync(new GitRunOptions { Progress = progress }, cancellationToken, "fetch", "--all", "--prune", "--progress");
        }

        public Task<GitResult> PullAsync(PullMode mode, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var args = new List<string> { "pull", "--progress", "--no-edit" };
            if (mode == PullMode.FastForwardOnly) args.Add("--ff-only");
            else if (mode == PullMode.Rebase) args.Add("--rebase");
            var options = new GitRunOptions { Progress = progress, Environment = NoEditorEnvironment };
            return RunAsync(options, cancellationToken, args.ToArray());
        }

        public Task<GitResult> PushAsync(string remote, string branch, bool setUpstream, bool forceWithLease, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var args = new List<string> { "push", "--progress" };
            if (forceWithLease) args.Add("--force-with-lease");
            if (setUpstream)
            {
                args.Add("--set-upstream");
                args.Add(remote ?? "origin");
                args.Add(branch ?? "HEAD");
            }
            return RunAsync(new GitRunOptions { Progress = progress }, cancellationToken, args.ToArray());
        }

        public Task<GitResult> PushBranchAsync(string remote, string branch, IProgress<string> progress, CancellationToken cancellationToken)
        {
            return RunAsync(new GitRunOptions { Progress = progress }, cancellationToken, "push", "--progress", remote, branch);
        }

        // ------------------------------------------------------------------ stashes

        public async Task<GitResult> StashAsync(string message, bool includeUntracked)
        {
            var args = new List<string> { "stash", "push" };
            if (includeUntracked) args.Add("--include-untracked");
            if (!string.IsNullOrWhiteSpace(message)) { args.Add("-m"); args.Add(message); }
            return await RunAsync(args.ToArray()).ConfigureAwait(false);
        }

        public Task<GitResult> StashPopAsync(int index) => RunAsync("stash", "pop", "stash@{" + index + "}");

        public Task<GitResult> StashApplyAsync(int index) => RunAsync("stash", "apply", "stash@{" + index + "}");

        public Task<GitResult> StashDropAsync(int index) => RunAsync("stash", "drop", "stash@{" + index + "}");

        // ------------------------------------------------------------------ misc

        /// <summary>Turns a remote URL into a browsable web URL (GitHub/GitLab style), or null.</summary>
        public static string ToWebUrl(string remoteUrl)
        {
            if (string.IsNullOrWhiteSpace(remoteUrl)) return null;
            var url = remoteUrl.Trim();
            if (url.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
            {
                // git@github.com:owner/repo.git
                var colon = url.IndexOf(':');
                if (colon < 0) return null;
                var host = url.Substring(4, colon - 4);
                var path = url.Substring(colon + 1);
                url = "https://" + host + "/" + path;
            }
            else if (url.StartsWith("ssh://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url.Substring(6);
                var at = url.IndexOf('@');
                if (at > 0) url = "https://" + url.Substring(at + 1);
            }
            if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase)) url = url.Substring(0, url.Length - 4);
            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
            return null;
        }
    }
}
