using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FtpClient.Services
{
    /// <summary>How text files cross the wire. SFTP is always binary.</summary>
    public enum TransferMode
    {
        Binary,
        Ascii,
        Auto,
    }

    /// <summary>How the transfer queue groups its rows.</summary>
    public enum QueueGrouping
    {
        Flat,
        BySite,
        ByDirection,
    }

    /// <summary>
    /// A session tab to reopen: a saved site by id, or a quick-connect site in full - never its
    /// password - plus the directory the tab was showing.
    /// </summary>
    public sealed class OpenTab
    {
        public string SiteId { get; set; }
        public Remote.Site Site { get; set; }
        public string Path { get; set; }
    }

    /// <summary>User preferences and window state, persisted as XML under %APPDATA%\FtpClient.</summary>
    public sealed class AppSettings
    {
        private static AppSettings _current;

        public static AppSettings Current => _current ?? (_current = Load());

        /// <summary>%APPDATA%\FtpClient, or the FTPCLIENT_DATA folder when set, so tests keep their state apart.</summary>
        public static string Folder
        {
            get
            {
                var overridden = Environment.GetEnvironmentVariable("FTPCLIENT_DATA");
                return string.IsNullOrWhiteSpace(overridden)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FtpClient")
                    : overridden;
            }
        }

        public static string FilePath => Path.Combine(Folder, "settings.xml");

        // ------------------------------------------------------------------ appearance

        /// <summary>Dark or light palette.</summary>
        public ThemeMode Theme { get; set; } = ThemeMode.Dark;

        /// <summary>File list row height, 26 to 38 px.</summary>
        public int FileRowHeight { get; set; } = 32;

        /// <summary>Dotfiles such as <c>.htaccess</c>; on by default, since on a web server they matter.</summary>
        public bool ShowHiddenFiles { get; set; } = true;

        public QueueGrouping QueueGrouping { get; set; } = QueueGrouping.Flat;

        // ------------------------------------------------------------------ transfers

        /// <summary>Parallel connections per site.</summary>
        public int ConcurrentTransfers { get; set; } = 4;

        public TransferMode TransferMode { get; set; } = TransferMode.Binary;

        public bool SpeedLimitEnabled { get; set; }

        /// <summary>Throughput cap in KiB/s when the limit is on.</summary>
        public int SpeedLimitKilobytes { get; set; } = 2048;

        /// <summary>Continue from the last confirmed byte instead of starting over.</summary>
        public bool ResumeTransfers { get; set; } = true;

        /// <summary>Opened on connect when a site has no remote directory of its own.</summary>
        public string DefaultRemoteDirectory { get; set; } = "/var/www";

        /// <summary>Set the remote modification time to match the local file.</summary>
        public bool PreserveTimestamps { get; set; } = true;

        // ------------------------------------------------------------------ connection

        public int ConnectTimeoutSeconds { get; set; } = 30;

        /// <summary>Seconds between keepalives on an idle session; 0 turns them off.</summary>
        public int KeepAliveSeconds { get; set; } = 30;

        /// <summary>Passive data connections for plain FTP.</summary>
        public bool PassiveMode { get; set; } = true;

        // ------------------------------------------------------------------ editors

        /// <summary>Program that opens remote files for editing; empty uses the Windows association.</summary>
        public string EditorPath { get; set; }

        /// <summary>git.exe used for paired repositories; empty finds it on PATH.</summary>
        public string GitExecutable { get; set; }

        // ------------------------------------------------------------------ window and session

        public int WindowX { get; set; } = -1;
        public int WindowY { get; set; } = -1;
        public int WindowWidth { get; set; } = 1440;
        public int WindowHeight { get; set; } = 900;
        public bool WindowMaximized { get; set; }

        /// <summary>The session tabs, in order. Stored whenever they change, not only on a clean exit.</summary>
        public List<OpenTab> OpenTabs { get; set; } = new List<OpenTab>();

        /// <summary>Index into <see cref="OpenTabs"/> of the tab in front; -1 for the connect screen.</summary>
        public int ActiveTab { get; set; } = -1;

        /// <summary>Quick-connect targets, newest first, as <c>protocol://user@host:port</c>.</summary>
        public List<string> RecentHosts { get; set; } = new List<string>();

        public bool RememberRecentHosts { get; set; } = true;

        /// <summary>
        /// Closed quick-connect sites that had pairings, bookmarks or a trusted key, newest first, so
        /// connecting to the server again or saving it as a site brings them back. Never a password.
        /// </summary>
        public List<Remote.Site> QuickConnectHistory { get; set; } = new List<Remote.Site>();

        public int TreePaneWidth { get; set; } = 280;
        public int CommitDetailWidth { get; set; } = 560;

        /// <summary>0 for the list, 1 for the grid.</summary>
        public int FileViewMode { get; set; }
        public int QueueHeight { get; set; } = 324;
        public bool QueueCollapsed { get; set; }

        public void AddRecentHost(string target)
        {
            if (string.IsNullOrEmpty(target) || !RememberRecentHosts) return;
            RecentHosts.RemoveAll(h => string.Equals(h, target, StringComparison.OrdinalIgnoreCase));
            RecentHosts.Insert(0, target);
            if (RecentHosts.Count > 20) RecentHosts.RemoveRange(20, RecentHosts.Count - 20);
        }

        public void RememberQuickConnect(Remote.Site site)
        {
            if (site == null || !site.HasHistory) return;
            ForgetQuickConnect(site);
            var copy = site.Clone();
            copy.IsTransient = true;
            QuickConnectHistory.Insert(0, copy);
            if (QuickConnectHistory.Count > 20) QuickConnectHistory.RemoveRange(20, QuickConnectHistory.Count - 20);
        }

        public Remote.Site FindQuickConnect(Remote.Site site) => site == null ? null : QuickConnectHistory.Find(s => s.SameEndpoint(site));

        public void ForgetQuickConnect(Remote.Site site)
        {
            if (site != null) QuickConnectHistory.RemoveAll(s => s.SameEndpoint(site));
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var serializer = new XmlSerializer(typeof(AppSettings));
                    using (var stream = File.OpenRead(FilePath))
                    {
                        var loaded = (AppSettings)serializer.Deserialize(stream);
                        loaded.FileRowHeight = Math.Max(26, Math.Min(38, loaded.FileRowHeight));
                        loaded.ConcurrentTransfers = Math.Max(1, Math.Min(16, loaded.ConcurrentTransfers));
                        return loaded;
                    }
                }
            }
            catch
            {
                // Corrupt settings are not worth crashing over; fall back to defaults.
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                var serializer = new XmlSerializer(typeof(AppSettings));
                using (var stream = File.Create(FilePath))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch
            {
                // Ignore persistence failures (read-only profile, etc.).
            }
        }
    }
}
