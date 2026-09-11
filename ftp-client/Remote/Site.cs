using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace FtpClient.Remote
{
    public enum RemoteProtocol
    {
        Sftp,
        Ftp,
        /// <summary>FTP upgraded to TLS with AUTH TLS on the control port.</summary>
        Ftps,
        /// <summary>A folder on this machine or a mounted share, browsed as if it were a server.</summary>
        Local,
    }

    public enum AuthMethod
    {
        Password,
        KeyFile,
        Anonymous,
    }

    /// <summary>A saved server: where it is, how to sign in, and what is paired with it.</summary>
    public sealed class Site
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name { get; set; }

        /// <summary>The site manager's group, e.g. PRODUCTION or STAGING.</summary>
        public string Group { get; set; }

        public RemoteProtocol Protocol { get; set; } = RemoteProtocol.Sftp;

        /// <summary>Host name, or the root folder for <see cref="RemoteProtocol.Local"/>.</summary>
        public string Host { get; set; }

        public int Port { get; set; } = 22;

        public string User { get; set; }

        public AuthMethod Auth { get; set; } = AuthMethod.Password;

        public string KeyFile { get; set; }

        /// <summary>Opened on connect; empty uses the server's home directory.</summary>
        public string RemoteDirectory { get; set; }

        public DateTime? LastUsed { get; set; }

        /// <summary>SHA-256 fingerprint of the SSH host key the user accepted.</summary>
        public string TrustedHostKey { get; set; }

        public List<string> Bookmarks { get; set; } = new List<string>();

        public List<Pairing> Pairings { get; set; } = new List<Pairing>();

        /// <summary>True for a quick-connect target that was never saved.</summary>
        [XmlIgnore]
        public bool IsTransient { get; set; }

        [XmlIgnore]
        public string ProtocolName => NameOf(Protocol);

        /// <summary>The connection URI shown in the breadcrumb, e.g. <c>sftp://deploy@web-prod</c>.</summary>
        [XmlIgnore]
        public string Uri
        {
            get
            {
                // A folder reads by its name; the full path is in the status bar.
                if (Protocol == RemoteProtocol.Local) return System.IO.Path.GetFileName((Host ?? string.Empty).TrimEnd('\\', '/'));
                var scheme = Protocol == RemoteProtocol.Sftp ? "sftp" : Protocol == RemoteProtocol.Ftps ? "ftps" : "ftp";
                var user = string.IsNullOrEmpty(User) ? string.Empty : User + "@";
                var port = Port == DefaultPort(Protocol) ? string.Empty : ":" + Port;
                return scheme + "://" + user + Host + port;
            }
        }

        /// <summary><c>user@host:port</c>, as the status bar writes it.</summary>
        [XmlIgnore]
        public string Endpoint =>
            Protocol == RemoteProtocol.Local
                ? Services.Format.HomeRelative(Host)
                : (string.IsNullOrEmpty(User) ? string.Empty : User + "@") + Host + ":" + Port;

        /// <summary>The pairing whose remote root holds <paramref name="remotePath"/>, deepest first.</summary>
        public Pairing PairingFor(string remotePath)
        {
            Pairing best = null;
            foreach (var pairing in Pairings)
            {
                if (!RemotePath.IsWithin(remotePath, pairing.RemoteRoot)) continue;
                if (best == null || pairing.RemoteRoot.Length > best.RemoteRoot.Length) best = pairing;
            }
            return best;
        }

        public Pairing PairingAt(string remotePath)
        {
            var normalized = RemotePath.Normalize(remotePath);
            return Pairings.Find(p => RemotePath.Normalize(p.RemoteRoot) == normalized);
        }

        /// <summary>Something worth keeping learned while connected: pairings, bookmarks or a trusted key.</summary>
        [XmlIgnore]
        public bool HasHistory => Pairings.Count > 0 || Bookmarks.Count > 0 || !string.IsNullOrEmpty(TrustedHostKey);

        /// <summary>True when both reach the same account on the same server, or the same folder, whatever they are called.</summary>
        public bool SameEndpoint(Site other)
        {
            if (other == null || other.Protocol != Protocol) return false;
            if (Protocol == RemoteProtocol.Local) return string.Equals(FolderKey(Host), FolderKey(other.Host), StringComparison.OrdinalIgnoreCase);
            return string.Equals(Host ?? string.Empty, other.Host ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                && Port == other.Port
                && string.Equals(User ?? string.Empty, other.User ?? string.Empty, StringComparison.Ordinal);
        }

        private static string FolderKey(string folder)
        {
            folder = folder ?? string.Empty;
            try
            {
                folder = System.IO.Path.GetFullPath(folder);
            }
            catch (Exception)
            {
                // Compare as typed.
            }
            return folder.TrimEnd('\\', '/');
        }

        /// <summary>
        /// Takes the pairings (with their deploy history), bookmarks and trusted key of another site
        /// for the same server. What this site already has wins.
        /// </summary>
        public void AdoptHistory(Site from)
        {
            if (from == null || ReferenceEquals(from, this)) return;
            foreach (var pairing in from.Pairings)
            {
                if (PairingAt(pairing.RemoteRoot) == null) Pairings.Add(pairing.Clone());
            }
            foreach (var bookmark in from.Bookmarks)
            {
                if (!Bookmarks.Contains(bookmark)) Bookmarks.Add(bookmark);
            }
            if (string.IsNullOrEmpty(TrustedHostKey)) TrustedHostKey = from.TrustedHostKey;
        }

        public static string NameOf(RemoteProtocol protocol)
        {
            switch (protocol)
            {
                case RemoteProtocol.Ftp: return "FTP";
                case RemoteProtocol.Ftps: return "FTPS";
                case RemoteProtocol.Local: return "Local";
                default: return "SFTP";
            }
        }

        public static int DefaultPort(RemoteProtocol protocol)
        {
            switch (protocol)
            {
                case RemoteProtocol.Sftp: return 22;
                case RemoteProtocol.Local: return 0;
                default: return 21;
            }
        }

        public Site Clone()
        {
            var copy = (Site)MemberwiseClone();
            copy.Bookmarks = new List<string>(Bookmarks);
            copy.Pairings = Pairings.ConvertAll(p => p.Clone());
            return copy;
        }
    }

    /// <summary>
    /// A local git working directory bound to a remote directory. Stored per site and keyed by
    /// the remote root, so it survives reconnects.
    /// </summary>
    public sealed class Pairing
    {
        public string RemoteRoot { get; set; }

        public string LocalRoot { get; set; }

        /// <summary>Branch the Commits tab follows; empty follows whatever is checked out.</summary>
        public string Branch { get; set; }

        public bool HonourIgnoreFiles { get; set; } = true;

        /// <summary>Files a commit deleted are deleted on the server too.</summary>
        public bool ApplyDeletions { get; set; } = true;

        /// <summary>Repository-relative folder whose contents map onto the remote root, e.g. <c>dist/</c>.</summary>
        public string BuildOutputDirectory { get; set; }

        public bool ConfirmBeforeDeploy { get; set; } = true;

        /// <summary>The newest commit whose every file reached the server.</summary>
        public string LastDeployedSha { get; set; }

        public List<string> DeployedShas { get; set; } = new List<string>();

        /// <summary>The Commits tab's selection, restored when the tab is shown again.</summary>
        public string SelectedSha { get; set; }

        public List<string> DeselectedPaths { get; set; } = new List<string>();

        public Pairing Clone()
        {
            var copy = (Pairing)MemberwiseClone();
            copy.DeployedShas = new List<string>(DeployedShas);
            copy.DeselectedPaths = new List<string>(DeselectedPaths);
            return copy;
        }
    }
}
