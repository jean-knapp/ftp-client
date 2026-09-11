using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FtpClient.Git
{
    /// <summary>Parsers for the machine-readable git output formats used by <see cref="GitRepository"/>.</summary>
    public static class GitParsers
    {
        public const char FieldSeparator = '\u001f';
        public const char RecordSeparator = '\u001e';

        /// <summary>Format string for <c>git log</c>, see <see cref="ParseLog"/>.</summary>
        public const string LogFormat = "%H%x1f%P%x1f%an%x1f%ae%x1f%aI%x1f%cn%x1f%cI%x1f%s%x1f%b%x1e";

        public static List<CommitInfo> ParseLog(string output)
        {
            var commits = new List<CommitInfo>();
            if (string.IsNullOrEmpty(output)) return commits;
            foreach (var record in output.Split(RecordSeparator))
            {
                var fields = record.TrimStart('\n', '\r').Split(FieldSeparator);
                if (fields.Length < 9 || fields[0].Length < 7) continue;
                var commit = new CommitInfo
                {
                    Sha = fields[0].Trim(),
                    AuthorName = fields[2],
                    AuthorEmail = fields[3],
                    AuthorDate = ParseDate(fields[4]),
                    CommitterName = fields[5],
                    CommitDate = ParseDate(fields[6]),
                    Subject = fields[7],
                    Body = fields[8].Trim('\n', '\r'),
                };
                foreach (var p in fields[1].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) commit.Parents.Add(p);
                commits.Add(commit);
            }
            return commits;
        }

        /// <summary>Format for <c>git for-each-ref</c>, see <see cref="ParseRefs"/>.</summary>
        public const string RefFormat =
            "%(refname)%1f%(objecttype)%1f%(objectname)%1f%(*objectname)%1f%(upstream:short)%1f%(upstream:track,nobracket)%1f%(HEAD)%1f%(symref)";

        public static List<RefInfo> ParseRefs(string output)
        {
            var refs = new List<RefInfo>();
            if (string.IsNullOrEmpty(output)) return refs;
            foreach (var rawLine in output.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0) continue;
                var f = line.Split(FieldSeparator);
                if (f.Length < 8) continue;
                var fullName = f[0];
                if (!string.IsNullOrEmpty(f[7])) continue; // symbolic refs such as origin/HEAD

                var info = new RefInfo
                {
                    FullName = fullName,
                    Sha = string.IsNullOrEmpty(f[3]) ? f[2] : f[3],
                    IsHead = f[6] == "*",
                };

                if (fullName.StartsWith("refs/heads/", StringComparison.Ordinal))
                {
                    info.Kind = RefKind.LocalBranch;
                    info.Name = fullName.Substring("refs/heads/".Length);
                    info.Upstream = string.IsNullOrEmpty(f[4]) ? null : f[4];
                    ParseTracking(f[5], info);
                }
                else if (fullName.StartsWith("refs/remotes/", StringComparison.Ordinal))
                {
                    info.Kind = RefKind.RemoteBranch;
                    info.Name = fullName.Substring("refs/remotes/".Length);
                    var slash = info.Name.IndexOf('/');
                    info.Remote = slash > 0 ? info.Name.Substring(0, slash) : null;
                }
                else if (fullName.StartsWith("refs/tags/", StringComparison.Ordinal))
                {
                    info.Kind = RefKind.Tag;
                    info.Name = fullName.Substring("refs/tags/".Length);
                }
                else continue;

                refs.Add(info);
            }
            return refs;
        }

        private static readonly Regex AheadRegex = new Regex(@"ahead (\d+)", RegexOptions.Compiled);
        private static readonly Regex BehindRegex = new Regex(@"behind (\d+)", RegexOptions.Compiled);

        private static void ParseTracking(string track, RefInfo info)
        {
            if (string.IsNullOrEmpty(track)) return;
            if (track.IndexOf("gone", StringComparison.OrdinalIgnoreCase) >= 0) { info.UpstreamGone = true; return; }
            var a = AheadRegex.Match(track);
            var b = BehindRegex.Match(track);
            if (a.Success) info.Ahead = int.Parse(a.Groups[1].Value, CultureInfo.InvariantCulture);
            if (b.Success) info.Behind = int.Parse(b.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        /// <summary>Parses <c>git status --porcelain=v2 -z --branch</c>.</summary>
        public static RepositoryStatus ParseStatus(string output)
        {
            var status = new RepositoryStatus();
            if (string.IsNullOrEmpty(output)) return status;
            var tokens = output.Replace("\n", "\0").Split('\0');
            for (int i = 0; i < tokens.Length; i++)
            {
                var t = tokens[i];
                if (t.Length == 0) continue;
                if (t.StartsWith("# ", StringComparison.Ordinal))
                {
                    ParseStatusHeader(t, status);
                    continue;
                }
                switch (t[0])
                {
                    case '1':
                    {
                        var parts = t.Split(new[] { ' ' }, 9);
                        if (parts.Length < 9) break;
                        AddOrdinaryEntry(status, parts[1], parts[8], null);
                        break;
                    }
                    case '2':
                    {
                        var parts = t.Split(new[] { ' ' }, 10);
                        if (parts.Length < 10) break;
                        var origPath = i + 1 < tokens.Length ? tokens[++i] : null;
                        AddOrdinaryEntry(status, parts[1], parts[9], origPath);
                        break;
                    }
                    case 'u':
                    {
                        var parts = t.Split(new[] { ' ' }, 11);
                        if (parts.Length < 11) break;
                        status.Unstaged.Add(new FileChange { Path = parts[10], Kind = FileChangeKind.Conflicted, Staged = false });
                        status.ConflictCount++;
                        break;
                    }
                    case '?':
                        status.Unstaged.Add(new FileChange { Path = t.Substring(2), Kind = FileChangeKind.Untracked, Staged = false });
                        break;
                    case '!':
                        break; // ignored files are not listed
                }
            }
            status.Staged.Sort((x, y) => string.CompareOrdinal(x.Path, y.Path));
            status.Unstaged.Sort((x, y) => string.CompareOrdinal(x.Path, y.Path));
            return status;
        }

        private static void ParseStatusHeader(string line, RepositoryStatus status)
        {
            const string oid = "# branch.oid ";
            const string head = "# branch.head ";
            const string upstream = "# branch.upstream ";
            const string ab = "# branch.ab ";
            if (line.StartsWith(oid, StringComparison.Ordinal))
            {
                var v = line.Substring(oid.Length).Trim();
                if (v == "(initial)") status.IsUnborn = true; else status.HeadSha = v;
            }
            else if (line.StartsWith(head, StringComparison.Ordinal))
            {
                var v = line.Substring(head.Length).Trim();
                if (v == "(detached)") status.IsDetached = true; else status.Branch = v;
            }
            else if (line.StartsWith(upstream, StringComparison.Ordinal))
            {
                status.Upstream = line.Substring(upstream.Length).Trim();
            }
            else if (line.StartsWith(ab, StringComparison.Ordinal))
            {
                foreach (var part in line.Substring(ab.Length).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int n;
                    if (part.Length > 1 && int.TryParse(part.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    {
                        if (part[0] == '+') status.Ahead = n; else if (part[0] == '-') status.Behind = n;
                    }
                }
            }
        }

        private static void AddOrdinaryEntry(RepositoryStatus status, string xy, string path, string origPath)
        {
            if (xy.Length < 2) return;
            char x = xy[0], y = xy[1];
            if (x != '.')
            {
                status.Staged.Add(new FileChange
                {
                    Path = path,
                    OldPath = origPath,
                    Kind = FileChange.KindFromLetter(x),
                    Staged = true,
                });
            }
            if (y != '.')
            {
                status.Unstaged.Add(new FileChange
                {
                    Path = path,
                    Kind = FileChange.KindFromLetter(y),
                    Staged = false,
                });
            }
        }

        /// <summary>Parses <c>git diff --name-status -z</c> / <c>git diff-tree --name-status -z</c>.</summary>
        public static List<FileChange> ParseNameStatus(string output)
        {
            var list = new List<FileChange>();
            if (string.IsNullOrEmpty(output)) return list;
            var tokens = output.Split('\0');
            for (int i = 0; i < tokens.Length; i++)
            {
                var t = tokens[i];
                if (t.Length == 0) continue;
                char letter = t[0];
                var change = new FileChange { Kind = FileChange.KindFromLetter(letter), Staged = true };
                if ((letter == 'R' || letter == 'C') && i + 2 < tokens.Length)
                {
                    change.OldPath = tokens[++i];
                    change.Path = tokens[++i];
                }
                else if (i + 1 < tokens.Length)
                {
                    change.Path = tokens[++i];
                }
                else continue;
                list.Add(change);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return list;
        }

        /// <summary>Format for <c>git stash list</c>, see <see cref="ParseStashList"/>.</summary>
        public const string StashFormat = "%H%x1f%s%x1f%ci";

        public static List<StashInfo> ParseStashList(string output)
        {
            var list = new List<StashInfo>();
            if (string.IsNullOrEmpty(output)) return list;
            int index = 0;
            foreach (var rawLine in output.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0) continue;
                var f = line.Split(FieldSeparator);
                if (f.Length < 3) continue;
                list.Add(new StashInfo { Index = index++, Sha = f[0], Message = f[1], Date = ParseDate(f[2]) });
            }
            return list;
        }

        private static readonly Regex HunkRegex = new Regex(@"^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@", RegexOptions.Compiled);

        /// <summary>Parses unified diff text for a single file.</summary>
        public static DiffDocument ParseDiff(string output, string path)
        {
            var doc = new DiffDocument { Path = path };
            if (string.IsNullOrEmpty(output)) return doc;
            int oldLine = 0, newLine = 0;
            bool inHunk = false;
            foreach (var rawLine in output.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (!inHunk)
                {
                    if (line.StartsWith("@@", StringComparison.Ordinal))
                    {
                        inHunk = true; // fall through to hunk handling below
                    }
                    else
                    {
                        if (line.StartsWith("Binary files", StringComparison.Ordinal))
                        {
                            doc.IsBinary = true;
                            doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Info, Text = line });
                        }
                        else if (line.StartsWith("rename from", StringComparison.Ordinal) || line.StartsWith("rename to", StringComparison.Ordinal)
                              || line.StartsWith("new file mode", StringComparison.Ordinal) || line.StartsWith("deleted file mode", StringComparison.Ordinal)
                              || line.StartsWith("old mode", StringComparison.Ordinal) || line.StartsWith("new mode", StringComparison.Ordinal))
                        {
                            doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Info, Text = line });
                        }
                        continue;
                    }
                }

                if (line.StartsWith("@@", StringComparison.Ordinal))
                {
                    var m = HunkRegex.Match(line);
                    if (m.Success)
                    {
                        oldLine = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                        newLine = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                    }
                    doc.Lines.Add(new DiffLine { Kind = DiffLineKind.HunkHeader, Text = line });
                    continue;
                }
                if (line.StartsWith("diff --git", StringComparison.Ordinal))
                {
                    inHunk = false;
                    continue;
                }
                if (line.Length == 0)
                {
                    // A trailing empty line after the last hunk; git never emits blank context lines without the leading space.
                    continue;
                }
                char c = line[0];
                var text = line.Substring(1);
                switch (c)
                {
                    case '+':
                        doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Added, Text = text, NewLineNumber = newLine++ });
                        doc.Additions++;
                        break;
                    case '-':
                        doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Removed, Text = text, OldLineNumber = oldLine++ });
                        doc.Deletions++;
                        break;
                    case ' ':
                        doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Context, Text = text, OldLineNumber = oldLine++, NewLineNumber = newLine++ });
                        break;
                    case '\\':
                        doc.Lines.Add(new DiffLine { Kind = DiffLineKind.NoNewline, Text = line });
                        break;
                    default:
                        doc.Lines.Add(new DiffLine { Kind = DiffLineKind.Info, Text = line });
                        break;
                }
            }
            return doc;
        }

        public static DateTimeOffset ParseDate(string text)
        {
            DateTimeOffset value;
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value)) return value;
            return DateTimeOffset.MinValue;
        }
    }
}
