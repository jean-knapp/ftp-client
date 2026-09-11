using System;

namespace FtpClient.Remote
{
    public enum RemoteEntryKind
    {
        File,
        Directory,
        Symlink,
    }

    /// <summary>One item in a remote directory listing.</summary>
    public sealed class RemoteEntry
    {
        public string Name { get; set; }

        /// <summary>Absolute POSIX path on the server.</summary>
        public string FullPath { get; set; }

        public RemoteEntryKind Kind { get; set; }

        /// <summary>Bytes; 0 for directories.</summary>
        public long Size { get; set; }

        /// <summary>Last modification time in UTC, when the server reports one.</summary>
        public DateTime? Modified { get; set; }

        /// <summary>Unix mode bits (the 0777 part); null when the server does not say.</summary>
        public int? Mode { get; set; }

        public string Owner { get; set; }
        public string Group { get; set; }

        /// <summary>How many items a directory holds, when that is known without another listing.</summary>
        public int? ItemCount { get; set; }

        /// <summary>Where a symbolic link points, when known.</summary>
        public string LinkTarget { get; set; }

        /// <summary>For a link: whether it leads to a directory; null when the server cannot say.</summary>
        public bool? LinksToDirectory { get; set; }

        /// <summary>A real directory. A link never is one, so deleting a link never touches what it points to.</summary>
        public bool IsDirectory => Kind == RemoteEntryKind.Directory;

        /// <summary>
        /// A directory, or a link that leads to one (or may): it sorts, shows in the tree and takes
        /// drops like a folder. Deletes still go by <see cref="IsDirectory"/>.
        /// </summary>
        public bool OpensAsFolder => IsDirectory || (Kind == RemoteEntryKind.Symlink && LinksToDirectory != false);
        public bool IsHidden => Name != null && Name.StartsWith(".", StringComparison.Ordinal) && Name != ".." && Name != ".";

        /// <summary>The symbolic permission string, e.g. <c>-rw-r--r--</c>.</summary>
        public string PermissionString => Mode.HasValue ? Permissions.Symbolic(Mode.Value, Kind) : string.Empty;

        public override string ToString() => FullPath;
    }

    /// <summary>Conversions between Unix mode bits and their textual forms.</summary>
    public static class Permissions
    {
        public static string Symbolic(int mode, RemoteEntryKind kind)
        {
            var chars = new char[10];
            chars[0] = kind == RemoteEntryKind.Directory ? 'd' : kind == RemoteEntryKind.Symlink ? 'l' : '-';
            const string rwx = "rwx";
            for (int i = 0; i < 9; i++)
            {
                int bit = 1 << (8 - i);
                chars[i + 1] = (mode & bit) != 0 ? rwx[i % 3] : '-';
            }
            return new string(chars);
        }

        public static string Octal(int mode) => Convert.ToString(mode & 0x1FF, 8).PadLeft(3, '0');

        /// <summary>Parses <c>644</c> or <c>0755</c>; returns null for anything else.</summary>
        public static int? ParseOctal(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = text.Trim();
            if (text.Length > 4) return null;
            int value = 0;
            foreach (var c in text)
            {
                if (c < '0' || c > '7') return null;
                value = value * 8 + (c - '0');
            }
            return value & 0x1FF;
        }

        /// <summary>Reads the mode bits out of a listing's <c>drwxr-xr-x</c> column.</summary>
        public static int? ParseSymbolic(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < 10) return null;
            int mode = 0;
            for (int i = 0; i < 9; i++)
            {
                char c = text[i + 1];
                if (c != '-' && c != 'S' && c != 'T') mode |= 1 << (8 - i);
            }
            return mode;
        }
    }

    /// <summary>POSIX path arithmetic for server paths, which never use backslashes.</summary>
    public static class RemotePath
    {
        public const string Root = "/";

        public static string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Root;
            var parts = path.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new System.Collections.Generic.List<string>();
            foreach (var part in parts)
            {
                if (part == ".") continue;
                if (part == "..") { if (stack.Count > 0) stack.RemoveAt(stack.Count - 1); continue; }
                stack.Add(part);
            }
            return "/" + string.Join("/", stack);
        }

        public static string Combine(string directory, string name)
        {
            if (string.IsNullOrEmpty(name)) return Normalize(directory);
            if (name.StartsWith("/", StringComparison.Ordinal)) return Normalize(name);
            return Normalize((directory ?? Root).TrimEnd('/') + "/" + name);
        }

        public static string Parent(string path)
        {
            var normalized = Normalize(path);
            if (normalized == Root) return Root;
            int slash = normalized.LastIndexOf('/');
            return slash <= 0 ? Root : normalized.Substring(0, slash);
        }

        public static string Name(string path)
        {
            var normalized = Normalize(path);
            if (normalized == Root) return Root;
            return normalized.Substring(normalized.LastIndexOf('/') + 1);
        }

        /// <summary>True when <paramref name="path"/> is <paramref name="root"/> or lies beneath it.</summary>
        public static bool IsWithin(string path, string root)
        {
            var p = Normalize(path);
            var r = Normalize(root);
            if (r == Root) return true;
            return p == r || p.StartsWith(r + "/", StringComparison.Ordinal);
        }

        /// <summary>
        /// <paramref name="toPath"/> as seen from <paramref name="fromDirectory"/>, e.g.
        /// <c>../shared/uploads</c>; <c>.</c> when they are the same directory.
        /// </summary>
        public static string Relative(string fromDirectory, string toPath)
        {
            var from = Segments(fromDirectory);
            var to = Segments(toPath);
            int common = 0;
            while (common < from.Length && common < to.Length && string.Equals(from[common], to[common], StringComparison.Ordinal)) common++;
            var parts = new System.Collections.Generic.List<string>();
            for (int i = common; i < from.Length; i++) parts.Add("..");
            for (int i = common; i < to.Length; i++) parts.Add(to[i]);
            return parts.Count == 0 ? "." : string.Join("/", parts);
        }

        /// <summary>The segments of a path from the root down, e.g. <c>var</c>, <c>www</c>, <c>html</c>.</summary>
        public static string[] Segments(string path)
        {
            return Normalize(path).Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    /// <summary>A failure reported by the server or the transport, in words fit for the UI.</summary>
    public sealed class RemoteException : Exception
    {
        public RemoteException(string message, RemoteErrorKind kind = RemoteErrorKind.General, Exception inner = null)
            : base(message, inner)
        {
            Kind = kind;
        }

        public RemoteErrorKind Kind { get; }
    }

    public enum RemoteErrorKind
    {
        General,
        NotFound,
        AlreadyExists,
        PermissionDenied,
        Authentication,
        Connection,
        NotSupported,
    }
}
