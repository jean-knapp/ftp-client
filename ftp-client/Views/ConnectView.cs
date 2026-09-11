using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;
using RemoteSite = FtpClient.Remote.Site;

namespace FtpClient.Views
{
    /// <summary>What the quick-connect card was filled in with.</summary>
    public sealed class QuickConnectEventArgs : EventArgs
    {
        public QuickConnectEventArgs(Site site, string secret, bool remember)
        {
            Site = site;
            Secret = secret;
            Remember = remember;
        }

        public Site Site { get; }

        /// <summary>The password, or null when the site signs in with a key file or anonymously.</summary>
        public string Secret { get; }

        public bool Remember { get; }
    }

    /// <summary>
    /// The no-session screen: "Connect to a server", the quick-connect card (host, protocol, port,
    /// user, key or password), and the saved sites below it.
    /// </summary>
    public partial class ConnectView : ModernUserControl
    {
        private const int MaxContentWidth = 920;
        private const int SidePadding = 80;
        private const int FieldHeight = 34;
        private const int CardPadding = 20;
        private const int FieldGap = 12;

        private static readonly RemoteProtocol[] ProtocolOrder = { RemoteProtocol.Sftp, RemoteProtocol.Ftp, RemoteProtocol.Ftps, RemoteProtocol.Local };

        private bool _busy;

        public event EventHandler<QuickConnectEventArgs> QuickConnectRequested;
        public event EventHandler<QuickConnectEventArgs> SaveSiteRequested;
        public event EventHandler<SiteEventArgs> SiteActivated;
        public event EventHandler ManageRequested;

        /// <summary>The saved site's menu asked to open it in the site manager.</summary>
        public event EventHandler<SiteEventArgs> SiteEditRequested;

        public event EventHandler<SiteEventArgs> SiteRenameRequested;

        /// <summary>Delete was chosen for a saved site; the main window confirms it.</summary>
        public event EventHandler<SiteEventArgs> SiteDeleteRequested;

        /// <summary>The saved site the open menu belongs to.</summary>
        private Site _menuSite;

        public ConnectView()
        {
            InitializeComponent();
            siteConnectItem.SvgIcon = Icons.Server;
            siteEditItem.SvgIcon = Icons.Settings;
            siteRenameItem.SvgIcon = Icons.Rename;
            siteCopyAddressItem.SvgIcon = Icons.Copy;
            siteDeleteItem.SvgIcon = Icons.Delete;
            secretBox.LeadingSvgIcon = Icons.Key;
            browseKeyButton.SvgIcon = Icons.Folder;
            browseKeyButton.SvgIconSize = 13;
            recentHostsButton.SvgIcon = Icons.ChevronDown;
            recentHostsButton.SvgIconSize = 10;
            portBox.Text = "22";
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // ModernTextBox keeps text only once its edit box exists.
            if (portBox.Text.Length == 0) portBox.Text = RemoteSite.DefaultPort(SelectedProtocol).ToString();
            if (protocolBox.SelectedIndex < 0) protocolBox.SelectedIndex = 0;
            rememberBox.Checked = AppSettings.Current.RememberRecentHosts;
            LayoutContent();
        }

        // ------------------------------------------------------------------ public surface

        public void SetSites(IEnumerable<Site> sites, Func<Site, bool> isConnected)
        {
            savedList.IsConnected = isConnected;
            savedList.SetSites(sites);
            bool any = savedList.RowsHeight > 0;
            LayoutContent();
        }

        public void RefreshConnectedState() => savedList.Invalidate();

        /// <summary>Fills the card from a site, e.g. a recent host or a site whose sign-in failed.</summary>
        public void Prefill(Site site)
        {
            if (site == null) return;
            protocolBox.SelectedIndex = Math.Max(0, Array.IndexOf(ProtocolOrder, site.Protocol));
            nameBox.Text = site.Name ?? string.Empty;
            hostBox.Text = site.Host ?? string.Empty;
            portBox.Text = site.Protocol == RemoteProtocol.Local ? string.Empty : site.Port.ToString();
            userBox.Text = site.User ?? string.Empty;
            secretBox.Text = site.Auth == AuthMethod.KeyFile ? site.KeyFile ?? string.Empty : string.Empty;
            UpdateSecretMask();
        }

        /// <summary>Disables the card while a connection attempt runs and shows progress in the status bar.</summary>
        public void SetBusy(bool busy, string status)
        {
            _busy = busy;
            connectButton.Enabled = !busy;
            saveButton.Enabled = !busy;
            connectButton.Text = busy ? "Connecting…" : "Connect";
            statusBar.State = busy ? SessionState.Connecting : SessionState.Disconnected;
            statusBar.StatusText = status ?? "Not connected";
            LayoutContent();
        }

        public void SetStatus(SessionState state, string text)
        {
            statusBar.State = state;
            statusBar.StatusText = text;
        }

        public void FocusHost()
        {
            hostBox.Focus();
        }

        // ------------------------------------------------------------------ reading the card

        private RemoteProtocol SelectedProtocol =>
            protocolBox.SelectedIndex >= 0 && protocolBox.SelectedIndex < ProtocolOrder.Length ? ProtocolOrder[protocolBox.SelectedIndex] : RemoteProtocol.Sftp;

        private static bool LooksLikeKeyPath(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (text.StartsWith("~/", StringComparison.Ordinal) || text.StartsWith("~\\", StringComparison.Ordinal)) return true;
            if (text.Length > 2 && text[1] == ':' && (text[2] == '\\' || text[2] == '/')) return true;
            if (text.StartsWith("\\\\", StringComparison.Ordinal)) return true;
            return (text.Contains("/") || text.Contains("\\")) && File.Exists(ExpandHome(text));
        }

        private static string ExpandHome(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.StartsWith("~", StringComparison.Ordinal))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                path = home + path.Substring(1);
            }
            return path.Replace('/', '\\');
        }

        /// <summary>The typed name, or the fallback when the Name field is empty.</summary>
        private string NameOr(string fallback)
        {
            var name = nameBox.Text.Trim();
            return name.Length > 0 ? name : fallback;
        }

        /// <summary>Builds a transient site from the card; accepts <c>user@host:port</c> and <c>sftp://…</c> in the host field.</summary>
        private Site BuildSite(out string secret, out string problem)
        {
            secret = null;
            problem = null;
            var protocol = SelectedProtocol;
            var host = hostBox.Text.Trim();
            var user = userBox.Text.Trim();
            int port = RemoteSite.DefaultPort(protocol);

            if (protocol == RemoteProtocol.Local)
            {
                if (host.Length == 0) { problem = "Choose the folder to open."; return null; }
                var folder = ExpandHome(host);
                if (!Directory.Exists(folder)) { problem = folder + " does not exist."; return null; }
                return new Site { Name = NameOr(new DirectoryInfo(folder).Name), Protocol = protocol, Host = folder, Port = 0, Auth = AuthMethod.Anonymous, IsTransient = true };
            }

            // sftp://user@host:port/path
            int scheme = host.IndexOf("://", StringComparison.Ordinal);
            string remoteDirectory = null;
            if (scheme > 0)
            {
                var name = host.Substring(0, scheme).ToLowerInvariant();
                if (name == "sftp" || name == "ssh") protocol = RemoteProtocol.Sftp;
                else if (name == "ftps") protocol = RemoteProtocol.Ftps;
                else if (name == "ftp") protocol = RemoteProtocol.Ftp;
                port = RemoteSite.DefaultPort(protocol);
                host = host.Substring(scheme + 3);
            }
            int slash = host.IndexOf('/');
            if (slash >= 0)
            {
                remoteDirectory = host.Substring(slash);
                host = host.Substring(0, slash);
            }
            int at = host.LastIndexOf('@');
            if (at >= 0)
            {
                if (user.Length == 0) user = host.Substring(0, at);
                host = host.Substring(at + 1);
            }
            int colon = host.LastIndexOf(':');
            if (colon > 0 && int.TryParse(host.Substring(colon + 1), out int parsedPort))
            {
                port = parsedPort;
                host = host.Substring(0, colon);
            }
            else if (int.TryParse(portBox.Text.Trim(), out int typedPort))
            {
                port = typedPort;
            }

            if (host.Length == 0) { problem = "Enter the host to connect to."; return null; }
            if (port <= 0 || port > 65535) { problem = "The port must be between 1 and 65535."; return null; }

            var site = new Site
            {
                Name = NameOr(host),
                Protocol = protocol,
                Host = host,
                Port = port,
                User = user,
                RemoteDirectory = remoteDirectory,
                IsTransient = true,
            };

            var typed = secretBox.Text;
            if (LooksLikeKeyPath(typed))
            {
                var key = ExpandHome(typed.Trim());
                if (!File.Exists(key)) { problem = "The key file " + key + " does not exist."; return null; }
                site.Auth = AuthMethod.KeyFile;
                site.KeyFile = key;
            }
            else if (typed.Length == 0 && protocol != RemoteProtocol.Sftp && (user.Length == 0 || string.Equals(user, "anonymous", StringComparison.OrdinalIgnoreCase)))
            {
                site.Auth = AuthMethod.Anonymous;
                site.User = "anonymous";
            }
            else
            {
                site.Auth = AuthMethod.Password;
                // Left empty, the password is asked for when connecting.
                secret = typed.Length == 0 ? null : typed;
            }
            return site;
        }

        private void RaiseWithSite(EventHandler<QuickConnectEventArgs> handler)
        {
            if (_busy) return;
            var site = BuildSite(out var secret, out var problem);
            if (site == null)
            {
                statusBar.State = SessionState.Failed;
                statusBar.StatusText = problem;
                return;
            }
            handler?.Invoke(this, new QuickConnectEventArgs(site, secret, rememberBox.Checked));
        }

        // ------------------------------------------------------------------ handlers

        private void connectButton_Click(object sender, EventArgs e) => RaiseWithSite(QuickConnectRequested);

        private void saveButton_Click(object sender, EventArgs e) => RaiseWithSite(SaveSiteRequested);

        private void manageButton_Click(object sender, EventArgs e) => ManageRequested?.Invoke(this, EventArgs.Empty);

        private void savedList_SiteActivated(object sender, SiteEventArgs e) => SiteActivated?.Invoke(this, e);

        // ------------------------------------------------------------------ saved site menu

        private void savedList_SiteMenuRequested(object sender, SiteMenuEventArgs e)
        {
            if (_busy || e.Site == null) return;
            _menuSite = e.Site;
            bool open = savedList.IsConnected != null && savedList.IsConnected(e.Site);
            siteConnectItem.Text = open ? "Go to its tab" : "Connect";
            siteMenu.Show(savedList, e.ScreenLocation);
        }

        private void savedList_KeyDown(object sender, KeyEventArgs e)
        {
            var site = savedList.SiteAt(savedList.SelectedIndex);
            if (_busy || site == null) return;
            if (e.KeyData == Keys.Delete)
            {
                SiteDeleteRequested?.Invoke(this, new SiteEventArgs(site));
                e.Handled = true;
            }
            else if (e.KeyData == Keys.F2)
            {
                SiteRenameRequested?.Invoke(this, new SiteEventArgs(site));
                e.Handled = true;
            }
        }

        private void siteConnectItem_Click(object sender, EventArgs e) => RaiseForMenuSite(SiteActivated);

        private void siteEditItem_Click(object sender, EventArgs e) => RaiseForMenuSite(SiteEditRequested);

        private void siteRenameItem_Click(object sender, EventArgs e) => RaiseForMenuSite(SiteRenameRequested);

        private void siteDeleteItem_Click(object sender, EventArgs e) => RaiseForMenuSite(SiteDeleteRequested);

        private void siteCopyAddressItem_Click(object sender, EventArgs e)
        {
            var site = _menuSite;
            if (site == null) return;
            var address = site.Protocol == RemoteProtocol.Local ? site.Host : site.Uri + (site.RemoteDirectory ?? string.Empty);
            try
            {
                Clipboard.SetText(address);
                SetStatus(SessionState.Disconnected, "Copied " + address);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                // Another program holds the clipboard; nothing to copy into.
            }
        }

        private void RaiseForMenuSite(EventHandler<SiteEventArgs> handler)
        {
            var site = _menuSite;
            _menuSite = null;
            if (site != null) handler?.Invoke(this, new SiteEventArgs(site));
        }

        private void rememberBox_CheckedChanged(object sender, EventArgs e)
        {
            AppSettings.Current.RememberRecentHosts = rememberBox.Checked;
        }

        private void protocolBox_SelectedIndexChanged(object sender, SelectedIndexChangedEventArgs e)
        {
            var protocol = SelectedProtocol;
            bool local = protocol == RemoteProtocol.Local;
            hostLabel.Text = local ? "Folder" : "Host";
            hostBox.PlaceholderText = local ? @"C:\Sites\my-site or \\server\share" : "host name or IP address";
            portBox.Enabled = !local;
            userBox.Enabled = !local;
            secretBox.Enabled = !local;
            browseKeyButton.ToolTipText = local ? "Choose a folder…" : "Choose a key file…";

            // A port that was one protocol's default follows the protocol; a typed one stays.
            if (!local && (portBox.Text.Length == 0 || portBox.Text == "22" || portBox.Text == "21"))
            {
                portBox.Text = RemoteSite.DefaultPort(protocol).ToString();
            }
            if (local) portBox.Text = string.Empty;
        }

        private void secretBox_TextChanged(object sender, EventArgs e) => UpdateSecretMask();

        /// <summary>A key path stays readable; anything else is a password and is masked.</summary>
        private void UpdateSecretMask()
        {
            char mask = LooksLikeKeyPath(secretBox.Text) || secretBox.Text.Length == 0 ? '\0' : '●';
            if (secretBox.PasswordChar != mask) secretBox.PasswordChar = mask;
        }

        private void secretBox_ButtonClick(object sender, ModernTextBoxButtonEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a private key";
                dialog.Filter = "Private keys|id_*;*.pem;*.key;*.ppk|All files|*.*";
                var ssh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
                if (Directory.Exists(ssh)) dialog.InitialDirectory = ssh;
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                secretBox.Text = Format.HomeRelative(dialog.FileName);
                UpdateSecretMask();
            }
        }

        private void hostBox_ButtonClick(object sender, ModernTextBoxButtonEventArgs e)
        {
            if (SelectedProtocol == RemoteProtocol.Local)
            {
                var folder = FolderPicker.Show(FindForm(), "Choose the folder to open", Directory.Exists(hostBox.Text) ? hostBox.Text : null);
                if (folder != null) hostBox.Text = folder;
                return;
            }

            recentMenu.Items.Clear();
            var recent = AppSettings.Current.RecentHosts;
            if (recent.Count == 0)
            {
                recentMenu.Items.Add(new ModernContextMenuItem { Text = "No recent hosts", Enabled = false });
            }
            foreach (var target in recent.Take(12))
            {
                var item = new ModernContextMenuItem { Text = target };
                var chosen = target;
                item.Click += (s, args) => ApplyRecentHost(chosen);
                recentMenu.Items.Add(item);
            }
            recentMenu.Show(hostBox, hostBox.PointToScreen(new Point(0, hostBox.Height + 2)));
        }

        private void ApplyRecentHost(string target)
        {
            if (string.IsNullOrEmpty(target)) return;
            // Stored as protocol://user@host:port
            int scheme = target.IndexOf("://", StringComparison.Ordinal);
            if (scheme > 0)
            {
                var name = target.Substring(0, scheme).ToLowerInvariant();
                protocolBox.SelectedIndex = name == "ftp" ? 1 : name == "ftps" ? 2 : name == "file" ? 3 : 0;
                target = target.Substring(scheme + 3);
            }
            int at = target.LastIndexOf('@');
            userBox.Text = at >= 0 ? target.Substring(0, at) : string.Empty;
            var rest = at >= 0 ? target.Substring(at + 1) : target;
            int colon = rest.LastIndexOf(':');
            if (colon > 0 && int.TryParse(rest.Substring(colon + 1), out int port))
            {
                portBox.Text = port.ToString();
                rest = rest.Substring(0, colon);
            }
            hostBox.Text = rest;
            secretBox.Focus();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && (hostBox.ContainsFocus || portBox.ContainsFocus || userBox.ContainsFocus || secretBox.ContainsFocus || nameBox.ContainsFocus))
            {
                connectButton_Click(connectButton, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ------------------------------------------------------------------ layout

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutContent();
        }

        /// <summary>Centres the column and shares the card's rows between their fields.</summary>
        private void LayoutContent()
        {
            if (contentPanel == null) return;
            int available = Math.Max(420, Width - SidePadding * 2);
            int width = Math.Min(MaxContentWidth, available);
            int inner = width - CardPadding * 2;

            headingLabel.SetBounds(0, 0, width, 42);
            subheadLabel.SetBounds(0, 48, width, 22);

            // Row 1: Host | Protocol 148 | Port 92
            const int protocolWidth = 148;
            const int portWidth = 92;
            int hostWidth = inner - protocolWidth - portWidth - FieldGap * 2;
            int x = CardPadding;
            hostLabel.SetBounds(x, 18, hostWidth, 18);
            hostBox.SetBounds(x, 40, hostWidth, FieldHeight);
            x += hostWidth + FieldGap;
            protocolLabel.SetBounds(x, 18, protocolWidth, 18);
            protocolBox.SetBounds(x, 40, protocolWidth, FieldHeight);
            x += protocolWidth + FieldGap;
            portLabel.SetBounds(x, 18, portWidth, 18);
            portBox.SetBounds(x, 40, portWidth, FieldHeight);

            // Row 2: User | Key or password
            int pairWidth = (inner - FieldGap) / 2;
            x = CardPadding;
            userLabel.SetBounds(x, 90, pairWidth, 18);
            userBox.SetBounds(x, 112, pairWidth, FieldHeight);
            x += pairWidth + FieldGap;
            secretLabel.SetBounds(x, 90, inner - pairWidth - FieldGap, 18);
            secretBox.SetBounds(x, 112, inner - pairWidth - FieldGap, FieldHeight);

            // Row 3: Name | Connect | Save as site…
            int connectWidth = Math.Max(94, connectButton.PreferredWidth);
            int saveWidth = Math.Max(110, saveButton.PreferredWidth);
            int nameWidth = inner - connectWidth - saveWidth - FieldGap - 8;
            x = CardPadding;
            nameLabel.SetBounds(x, 162, nameWidth, 18);
            nameBox.SetBounds(x, 184, nameWidth, FieldHeight);
            x += nameWidth + FieldGap;
            connectButton.SetBounds(x, 184, connectWidth, FieldHeight);
            x += connectWidth + 8;
            saveButton.SetBounds(x, 184, saveWidth, FieldHeight);

            rememberBox.SetBounds(CardPadding, 234, 320, 22);
            quickCard.SetBounds(0, 98, width, 276);

            const int savedTop = 402;
            manageButton.Width = manageButton.PreferredWidth;
            manageButton.SetBounds(width - manageButton.Width, savedTop - 7, manageButton.Width, 28);
            savedLabel.Width = Math.Max(60, savedLabel.PreferredWidth + 4);
            savedLabel.SetBounds(0, savedTop - 3, savedLabel.Width, 20);
            savedRule.SetBounds(savedLabel.Right + 10, savedTop + 7, Math.Max(10, manageButton.Left - savedLabel.Right - 22), 1);

            int listTop = savedTop + 26;
            int rows = Math.Max(52, savedList.RowsHeight);
            int maxHeight = Math.Max(106, Height - statusBar.Height - 40 - listTop - Math.Max(20, (Height - 700) / 2));
            int listHeight = Math.Min(rows + 2, maxHeight);
            savedCard.SetBounds(0, listTop, width, listHeight);

            int height = listTop + listHeight;
            int areaHeight = Height - statusBar.Height;
            contentPanel.SetBounds((Width - width) / 2, Math.Max(24, (areaHeight - height) / 2), width, height);
        }
    }
}
