using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FtpClient.Git;
using FtpClient.Remote;

namespace FtpClient.Deploy
{
    /// <summary>
    /// Reads a paired repository for the Commits tab: its history with line totals, the files a
    /// commit touched resolved to remote paths, the ignore rules that exclude some of them, and the
    /// file contents exactly as the commit has them.
    /// </summary>
    public sealed class DeployPlanner
    {
        private const int PathBatch = 200;
        private static readonly string[] CommonArguments = { "--no-pager", "-c", "core.quotepath=false", "-c", "color.ui=false" };

        public DeployPlanner(Pairing pairing)
        {
            Pairing = pairing ?? throw new ArgumentNullException(nameof(pairing));
            Repository = new GitRepository(pairing.LocalRoot);
        }

        public Pairing Pairing { get; }
        public GitRepository Repository { get; }

        /// <summary>The revision the Commits tab follows: the pairing's branch, or HEAD.</summary>
        public string Revision => string.IsNullOrWhiteSpace(Pairing.Branch) ? "HEAD" : Pairing.Branch.Trim();

        // ------------------------------------------------------------------ history

        /// <summary>The sha the followed revision points at, or null when it has no commits yet.</summary>
        public async Task<string> GetRevisionShaAsync(CancellationToken token)
        {
            var result = await RunAsync(token, null, "rev-parse", "--verify", "-q", Revision).ConfigureAwait(false);
            return result.Succeeded ? result.StandardOutput.Trim() : null;
        }

        public async Task<List<CommitEntry>> GetCommitsAsync(int maxCount, CancellationToken token)
        {
            var log = await RunAsync(token, null, "log", "--max-count=" + maxCount, "--format=" + GitParsers.LogFormat, Revision, "--").ConfigureAwait(false);
            if (!log.Succeeded)
            {
                // An unborn branch has no history to show; anything else is worth reporting.
                if (!await Repository.HasHeadAsync().ConfigureAwait(false)) return new List<CommitEntry>();
                throw new GitException(log);
            }
            var commits = GitParsers.ParseLog(log.StandardOutput);

            var stats = new Dictionary<string, int[]>(StringComparer.Ordinal);
            var numstat = await RunAsync(token, null, "log", "--max-count=" + maxCount, "--format=%x1e%H", "--numstat", Revision, "--").ConfigureAwait(false);
            if (numstat.Succeeded)
            {
                foreach (var record in numstat.StandardOutput.Split(new[] { '\x1e' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var lines = record.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length == 0) continue;
                    var totals = new int[3];
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var parts = lines[i].Split('\t');
                        if (parts.Length < 3) continue;
                        totals[0]++;
                        if (int.TryParse(parts[0], out int added)) totals[1] += added;
                        if (int.TryParse(parts[1], out int removed)) totals[2] += removed;
                    }
                    stats[lines[0].Trim()] = totals;
                }
            }

            return commits.Select(c =>
            {
                stats.TryGetValue(c.Sha, out var totals);
                return new CommitEntry
                {
                    Commit = c,
                    FileCount = totals?[0] ?? 0,
                    Added = totals?[1] ?? 0,
                    Removed = totals?[2] ?? 0,
                };
            }).ToList();
        }

        /// <summary>
        /// Marks each commit: queued ones Deploying, recorded ones and everything at or below the
        /// last deploy Deployed, the rest Pending.
        /// </summary>
        public static void ApplyStates(IList<CommitEntry> entries, Pairing pairing, ICollection<string> deploying)
        {
            if (entries == null || pairing == null) return;
            var deployed = new HashSet<string>(pairing.DeployedShas ?? new List<string>(), StringComparer.Ordinal);
            int last = -1;
            if (!string.IsNullOrEmpty(pairing.LastDeployedSha))
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i].Commit.Sha == pairing.LastDeployedSha) { last = i; break; }
                }
            }
            for (int i = 0; i < entries.Count; i++)
            {
                var sha = entries[i].Commit.Sha;
                if (deploying != null && deploying.Contains(sha)) entries[i].State = DeployState.Deploying;
                else if (deployed.Contains(sha) || (last >= 0 && i >= last)) entries[i].State = DeployState.Deployed;
                else entries[i].State = DeployState.Pending;
            }
        }

        private static int IndexOf(IList<CommitEntry> entries, string sha)
        {
            if (entries == null || string.IsNullOrEmpty(sha)) return -1;
            for (int i = 0; i < entries.Count; i++) if (entries[i].Commit.Sha == sha) return i;
            return -1;
        }

        /// <summary>Records one commit as deployed, leaving every other commit as it was.</summary>
        public static void MarkDeployed(Pairing pairing, IList<CommitEntry> entries, string sha)
        {
            if (pairing == null || string.IsNullOrEmpty(sha)) return;
            if (!pairing.DeployedShas.Contains(sha)) pairing.DeployedShas.Add(sha);
            if (pairing.DeployedShas.Count > 1000) pairing.DeployedShas.RemoveRange(0, pairing.DeployedShas.Count - 1000);
        }

        /// <summary>
        /// Moves the last-deploy mark to a commit, so it and everything older counts as deployed.
        /// Newer commits keep their state; a mark already newer than this commit stays.
        /// </summary>
        public static void MarkDeployedThrough(Pairing pairing, IList<CommitEntry> entries, string sha)
        {
            if (pairing == null) return;
            int index = IndexOf(entries, sha);
            if (index < 0) return;
            int last = IndexOf(entries, pairing.LastDeployedSha);
            if (last < 0 || index < last) pairing.LastDeployedSha = sha;
        }

        /// <summary>
        /// Takes one commit back to pending. When it was covered by the last-deploy mark, the
        /// commits between it and that mark stay deployed on their own and the mark moves to the
        /// next older commit.
        /// </summary>
        public static void MarkPending(Pairing pairing, IList<CommitEntry> entries, string sha)
        {
            if (pairing == null || string.IsNullOrEmpty(sha)) return;
            pairing.DeployedShas.Remove(sha);
            int index = IndexOf(entries, sha);
            int last = IndexOf(entries, pairing.LastDeployedSha);
            if (index < 0 || last < 0 || index < last) return;
            for (int i = last; i < index; i++) MarkDeployed(pairing, entries, entries[i].Commit.Sha);
            pairing.LastDeployedSha = index + 1 < entries.Count ? entries[index + 1].Commit.Sha : null;
        }

        /// <summary>Commits newer than the last full deploy.</summary>
        public static int CommitsAhead(IList<CommitEntry> entries, Pairing pairing)
        {
            if (entries == null || pairing == null) return 0;
            // Commits marked deployed one at a time don't count, wherever they sit.
            var deployed = new HashSet<string>(pairing.DeployedShas ?? new List<string>(), StringComparer.Ordinal);
            int last = IndexOf(entries, pairing.LastDeployedSha);
            int ahead = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (last >= 0 && i >= last) break;
                if (!deployed.Contains(entries[i].Commit.Sha)) ahead++;
            }
            return ahead;
        }

        // ------------------------------------------------------------------ files of a commit

        public async Task<List<DeployFile>> GetFilesAsync(CommitInfo commit, CancellationToken token)
        {
            var changes = await Repository.GetCommitFilesAsync(commit).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var build = BuildPrefix(Pairing.BuildOutputDirectory);
            var files = new List<DeployFile>();

            foreach (var change in changes)
            {
                var file = new DeployFile
                {
                    RepoPath = change.Path,
                    OldRepoPath = change.OldPath,
                    Kind = change.Kind,
                    RemotePath = MapToRemote(change.Path, build),
                    OldRemotePath = string.IsNullOrEmpty(change.OldPath) ? null : MapToRemote(change.OldPath, build),
                };

                if (file.RemotePath == null)
                {
                    file.Action = DeployAction.Ignored;
                    file.Note = "outside the build output directory " + build;
                }
                else if (change.Kind == FileChangeKind.Deleted)
                {
                    file.Action = Pairing.ApplyDeletions ? DeployAction.Delete : DeployAction.Ignored;
                    if (!Pairing.ApplyDeletions) file.Note = "deletions are not applied for this pairing";
                }
                else
                {
                    file.Action = change.Kind == FileChangeKind.Added || change.Kind == FileChangeKind.Copied ? DeployAction.New : DeployAction.Overwrite;
                }
                files.Add(file);
            }

            if (Pairing.HonourIgnoreFiles && files.Count > 0)
            {
                var ignored = await CheckIgnoredAsync(files.Where(f => f.Action != DeployAction.Ignored).Select(f => f.RepoPath), token).ConfigureAwait(false);
                foreach (var file in files)
                {
                    if (file.Action == DeployAction.Ignored || !ignored.TryGetValue(file.RepoPath, out var source)) continue;
                    file.Action = DeployAction.Ignored;
                    file.Note = "ignored by " + source;
                }
            }

            var sizes = await GetBlobSizesAsync(commit.Sha, files.Where(f => f.Kind != FileChangeKind.Deleted).Select(f => f.RepoPath), token).ConfigureAwait(false);
            bool restoring = string.Equals(Pairing.SelectedSha, commit.Sha, StringComparison.Ordinal);
            var deselected = new HashSet<string>(Pairing.DeselectedPaths ?? new List<string>(), StringComparer.Ordinal);
            foreach (var file in files)
            {
                if (sizes.TryGetValue(file.RepoPath, out long size)) file.Size = size;
                file.Included = file.CanInclude && !(restoring && deselected.Contains(file.RepoPath));
            }
            return files;
        }

        private static string BuildPrefix(string buildDirectory)
        {
            if (string.IsNullOrWhiteSpace(buildDirectory)) return string.Empty;
            var prefix = buildDirectory.Trim().Replace('\\', '/').Trim('/');
            return prefix.Length == 0 || prefix == "." ? string.Empty : prefix + "/";
        }

        /// <summary>Where a repository path lands under the remote root, or null when it is outside the build output.</summary>
        public string MapToRemote(string repoPath, string buildPrefix = null)
        {
            if (repoPath == null) return null;
            buildPrefix = buildPrefix ?? BuildPrefix(Pairing.BuildOutputDirectory);
            var relative = repoPath.Replace('\\', '/');
            if (buildPrefix.Length > 0)
            {
                if (!relative.StartsWith(buildPrefix, StringComparison.Ordinal)) return null;
                relative = relative.Substring(buildPrefix.Length);
            }
            return RemotePath.Combine(Pairing.RemoteRoot, relative);
        }

        /// <summary>Which of the paths the ignore files match, with the file that matched.</summary>
        private async Task<Dictionary<string, string>> CheckIgnoredAsync(IEnumerable<string> paths, CancellationToken token)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var list = paths.ToList();
            if (list.Count == 0) return result;

            // .ftpignore joins the rules as the excludes file, so one check covers both.
            var extra = new List<string>();
            var ftpIgnore = Path.Combine(Pairing.LocalRoot, ".ftpignore");
            if (File.Exists(ftpIgnore))
            {
                extra.Add("-c");
                extra.Add("core.excludesFile=" + ftpIgnore.Replace('\\', '/'));
            }

            var args = new List<string>(extra) { "check-ignore", "--no-index", "--verbose", "--non-matching", "--stdin" };
            var input = string.Join("\n", list) + "\n";
            var run = await RunAsync(token, new GitRunOptions { StandardInput = input }, args.ToArray()).ConfigureAwait(false);
            // Exit code 1 means nothing matched.
            if (!run.Succeeded && run.ExitCode != 1) return result;

            // --verbose lines: <source>:<line>:<pattern>\t<path>; non-matching paths have "::".
            foreach (var line in run.StandardOutput.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int tab = line.IndexOf('\t');
                if (tab < 0) continue;
                var header = line.Substring(0, tab);
                var path = line.Substring(tab + 1).TrimEnd('\r');
                if (header == "::" || header.Length == 0) continue;
                var pattern = header.Substring(header.LastIndexOf(':') + 1);
                // Negated patterns ("!keep.me") match but re-include the path.
                if (pattern.StartsWith("!", StringComparison.Ordinal)) continue;
                var source = header.Substring(0, header.IndexOf(':'));
                result[path] = string.IsNullOrEmpty(source) ? "an ignore rule" : Path.GetFileName(source.Replace('/', '\\')) + " (" + pattern + ")";
            }
            return result;
        }

        private async Task<Dictionary<string, long>> GetBlobSizesAsync(string sha, IEnumerable<string> paths, CancellationToken token)
        {
            var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
            var list = paths.ToList();
            for (int start = 0; start < list.Count; start += PathBatch)
            {
                var args = new List<string> { "ls-tree", "-l", "-z", sha, "--" };
                args.AddRange(list.Skip(start).Take(PathBatch));
                var run = await RunAsync(token, null, args.ToArray()).ConfigureAwait(false);
                if (!run.Succeeded) continue;
                foreach (var record in run.StandardOutput.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    // <mode> SP <type> SP <object> SP+ <size> TAB <path>
                    int tab = record.IndexOf('\t');
                    if (tab < 0) continue;
                    var fields = record.Substring(0, tab).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length >= 4 && long.TryParse(fields[3], out long size)) sizes[record.Substring(tab + 1)] = size;
                }
            }
            return sizes;
        }

        // ------------------------------------------------------------------ contents

        /// <summary>
        /// Writes a file as the commit has it - after the checkout filters, so line endings and
        /// LFS pointers come out the way a checkout would produce them - to <paramref name="destination"/>.
        /// </summary>
        public async Task ExtractAsync(string sha, string repoPath, string destination, CancellationToken token)
        {
            var directory = Path.GetDirectoryName(destination);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var args = new List<string>(CommonArguments) { "cat-file", "--filters", sha + ":" + repoPath };
            var start = new ProcessStartInfo(GitRunner.GitExecutable ?? "git", GitRunner.BuildArguments(args))
            {
                WorkingDirectory = Pairing.LocalRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using (var process = Process.Start(start))
            {
                if (process == null) throw new GitException("git could not be started.");
                var error = process.StandardError.ReadToEndAsync();
                try
                {
                    using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                    {
                        await process.StandardOutput.BaseStream.CopyToAsync(output, 81920, token).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    try { process.Kill(); } catch { }
                    throw;
                }
                await Task.Run(() => process.WaitForExit(), token).ConfigureAwait(false);
                var message = await error.ConfigureAwait(false);
                if (process.ExitCode != 0)
                {
                    throw new GitException("Could not read " + repoPath + " at " + sha.Substring(0, Math.Min(7, sha.Length)) + ": " + message.Trim());
                }
            }
        }

        /// <summary>A fresh folder for one deploy's extracted files.</summary>
        public static string NewStagingFolder(string sha)
        {
            var name = (sha ?? "commit").Substring(0, Math.Min(12, (sha ?? "commit").Length)) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            return Path.Combine(Path.GetTempPath(), "FtpClient", "deploy", name);
        }

        /// <summary>Commits and repository facts the pairing dialog describes, e.g. <c>master · 412 commits</c>.</summary>
        public static async Task<RepositorySummary> DescribeAsync(string localRoot)
        {
            var summary = new RepositorySummary();
            if (string.IsNullOrEmpty(localRoot) || !Directory.Exists(localRoot)) return summary;
            try
            {
                var root = await GitRepository.DiscoverAsync(localRoot).ConfigureAwait(false);
                if (root == null) return summary;
                summary.Root = root;
                var repository = new GitRepository(root);
                summary.Branch = await repository.GetCurrentBranchAsync().ConfigureAwait(false);
                var count = await GitRunner.RunAsync(root, "rev-list", "--count", "HEAD").ConfigureAwait(false);
                if (count.Succeeded && int.TryParse(count.StandardOutput.Trim(), out int commits)) summary.CommitCount = commits;
            }
            catch
            {
                // Not a repository, or git is missing: the dialog says so.
            }
            return summary;
        }

        private Task<GitResult> RunAsync(CancellationToken token, GitRunOptions options, params string[] args)
        {
            var all = new List<string>(CommonArguments.Length + args.Length);
            all.AddRange(CommonArguments);
            all.AddRange(args);
            return GitRunner.RunAsync(Pairing.LocalRoot, all, options, token);
        }
    }

    public sealed class RepositorySummary
    {
        /// <summary>The working tree root, or null when the folder is not inside a repository.</summary>
        public string Root { get; set; }
        public string Branch { get; set; }
        public int CommitCount { get; set; }
        public bool IsRepository => Root != null;
    }
}
