using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FtpClient.Remote;

namespace FtpClient.Services
{
    /// <summary>The number, size and time formats the design uses, in one place.</summary>
    public static class Format
    {
        private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB" };
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-US");

        /// <summary><c>8.2 KB</c>, <c>214 KB</c>, <c>1.8 GB</c>: one decimal below ten, none above.</summary>
        public static string Bytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            if (bytes < 1024) return bytes.ToString(Culture) + " B";
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < Units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return (value < 10 ? value.ToString("0.0", Culture) : Math.Round(value).ToString("0", Culture)) + " " + Units[unit];
        }

        /// <summary><c>4.2 MB/s</c>, <c>11.8 MB/s</c>.</summary>
        public static string Speed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1) return "—";
            double value = bytesPerSecond;
            int unit = 0;
            while (value >= 1024 && unit < Units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return (value < 100 ? value.ToString("0.0", Culture) : Math.Round(value).ToString("0", Culture)) + " " + Units[unit] + "/s";
        }

        /// <summary><c>4s</c>, <c>2m 41s</c>, <c>1h 05m</c>.</summary>
        public static string Eta(TimeSpan? eta)
        {
            if (!eta.HasValue) return "—";
            var t = eta.Value;
            if (t.TotalSeconds < 60) return Math.Max(1, (int)Math.Ceiling(t.TotalSeconds)) + "s";
            if (t.TotalMinutes < 60) return (int)t.TotalMinutes + "m " + t.Seconds.ToString("00") + "s";
            return (int)t.TotalHours + "h " + t.Minutes.ToString("00") + "m";
        }

        /// <summary><c>1 item</c>, <c>1,204 items</c>.</summary>
        public static string Count(int count, string singular, string plural = null)
        {
            return count.ToString("N0", Culture) + " " + (count == 1 ? singular : plural ?? singular + "s");
        }

        /// <summary><c>Sep 10, 2026 08:57</c>, in local time.</summary>
        public static string Timestamp(DateTime? utc)
        {
            if (!utc.HasValue) return string.Empty;
            return utc.Value.ToLocalTime().ToString("MMM d, yyyy HH:mm", Culture);
        }

        /// <summary><c>Today 09:41</c>, <c>Yesterday 17:55</c>, <c>Sep 9, 18:02</c>.</summary>
        public static string CommitDate(DateTimeOffset date)
        {
            var local = date.LocalDateTime;
            var today = DateTime.Today;
            if (local.Date == today) return "Today " + local.ToString("HH:mm", Culture);
            if (local.Date == today.AddDays(-1)) return "Yesterday " + local.ToString("HH:mm", Culture);
            if (local.Year == today.Year) return local.ToString("MMM d, HH:mm", Culture);
            return local.ToString("MMM d, yyyy", Culture);
        }

        /// <summary>A local path with the profile folder written as <c>~</c> and forward slashes, as the design shows paths.</summary>
        public static string HomeRelative(string localPath)
        {
            if (string.IsNullOrEmpty(localPath)) return string.Empty;
            localPath = localPath.Replace('/', '\\');
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
            if (localPath.StartsWith(home + "\\", StringComparison.OrdinalIgnoreCase) || string.Equals(localPath, home, StringComparison.OrdinalIgnoreCase))
            {
                return ("~" + localPath.Substring(home.Length)).Replace('\\', '/');
            }
            return localPath;
        }

        /// <summary><c>2 hours ago</c>, for saved sites and commits.</summary>
        public static string Relative(DateTime? local)
        {
            if (!local.HasValue) return "never";
            var span = DateTime.Now - local.Value;
            if (span.TotalSeconds < 60) return "just now";
            if (span.TotalMinutes < 60) return Count((int)span.TotalMinutes, "minute") + " ago";
            if (span.TotalHours < 24) return Count((int)span.TotalHours, "hour") + " ago";
            if (span.TotalDays < 2) return "yesterday";
            if (span.TotalDays < 30) return Count((int)span.TotalDays, "day") + " ago";
            return local.Value.ToString("MMM d, yyyy", Culture);
        }
    }

    /// <summary>The Kind column: what a file is, in words.</summary>
    public static class FileKinds
    {
        private static readonly Dictionary<string, string> ByExtension = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".php", "PHP source" }, { ".js", "JavaScript" }, { ".mjs", "JavaScript" }, { ".ts", "TypeScript" },
            { ".css", "Stylesheet" }, { ".scss", "Sass stylesheet" }, { ".html", "HTML document" }, { ".htm", "HTML document" },
            { ".json", "JSON" }, { ".xml", "XML document" }, { ".yml", "YAML" }, { ".yaml", "YAML" }, { ".toml", "TOML" },
            { ".md", "Markdown" }, { ".txt", "Text document" }, { ".csv", "CSV" }, { ".log", "Log file" },
            { ".ini", "Configuration" }, { ".conf", "Configuration" }, { ".config", "Configuration" },
            { ".sh", "Shell script" }, { ".py", "Python source" }, { ".rb", "Ruby source" }, { ".go", "Go source" },
            { ".cs", "C# source" }, { ".java", "Java source" }, { ".sql", "SQL script" },
            { ".png", "PNG image" }, { ".jpg", "JPEG image" }, { ".jpeg", "JPEG image" }, { ".gif", "GIF image" },
            { ".webp", "WebP image" }, { ".svg", "SVG image" }, { ".ico", "Icon" },
            { ".woff", "Web font" }, { ".woff2", "Web font" }, { ".ttf", "Font" },
            { ".zip", "Archive" }, { ".gz", "Archive" }, { ".tgz", "Archive" }, { ".7z", "Archive" }, { ".rar", "Archive" }, { ".tar", "Archive" },
            { ".pdf", "PDF document" }, { ".mp4", "Video" }, { ".mp3", "Audio" },
        };

        private static readonly Dictionary<string, string> ByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".env", "Environment file" }, { ".htaccess", "Apache configuration" }, { "error_log", "Log file" },
            { "access_log", "Log file" }, { ".gitignore", "Git ignore rules" }, { ".ftpignore", "FTP ignore rules" },
            { "Dockerfile", "Dockerfile" }, { "Makefile", "Makefile" }, { "composer.json", "Composer manifest" },
            { "package.json", "npm manifest" },
        };

        public static string Describe(RemoteEntry entry)
        {
            if (entry == null) return string.Empty;
            if (entry.Name == "..") return "Parent directory";
            if (entry.Kind == RemoteEntryKind.Directory) return "Folder";
            if (entry.Kind == RemoteEntryKind.Symlink)
            {
                return (entry.LinksToDirectory == true ? "Folder link" : "Symlink") + (string.IsNullOrEmpty(entry.LinkTarget) ? string.Empty : " → " + entry.LinkTarget);
            }
            return DescribeName(entry.Name);
        }

        public static string DescribeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "File";
            if (ByName.TryGetValue(name, out var byName)) return byName;
            if (name.EndsWith(".env", StringComparison.OrdinalIgnoreCase) || name.StartsWith(".env.", StringComparison.OrdinalIgnoreCase)) return "Environment file";
            var extension = Path.GetExtension(name);
            if (!string.IsNullOrEmpty(extension) && ByExtension.TryGetValue(extension, out var byExtension)) return byExtension;
            return string.IsNullOrEmpty(extension) ? "File" : extension.TrimStart('.').ToUpperInvariant() + " file";
        }
    }
}
