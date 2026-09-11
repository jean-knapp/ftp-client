using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// Site manager: saved sites in their folders on the left, and the selected site's General,
    /// Advanced and Transfer settings on the right.
    /// </summary>
    public partial class SiteManagerDialog : ModernForm
    {
        private const string StoredPasswordMask = "●●●●●●●●";
        private static readonly RemoteProtocol[] ProtocolOrder = { RemoteProtocol.Sftp, RemoteProtocol.Ftp, RemoteProtocol.Ftps, RemoteProtocol.Local };
        private static readonly AuthMethod[] AuthOrder = { AuthMethod.Password, AuthMethod.KeyFile, AuthMethod.Anonymous };

        private readonly Func<Site, bool> _isConnected;
        private readonly string _initialSiteId;
        private Site _site;
        private bool _loading;
        private bool _dirty;
        private bool _passwordTouched;

        public SiteManagerDialog(Func<Site, bool> isConnected, string selectSiteId)
        {
            InitializeComponent();
            Theme.Apply(skin);
            foreach (var box in new[] { keyBox, directoryBox })
            {
                box.OverrideSkinFont = true;
                box.Font = Fonts.Code(13f);
            }
            hostKeyValue.Monospace = true;
            _isConnected = isConnected ?? (s => false);
            _initialSiteId = selectSiteId;
            siteTree.IsConnected = _isConnected;
            browseKeyButton.SvgIcon = Icons.Folder;
        }

        /// <summary>The site to open when the dialog closed through Connect.</summary>
        public Site ConnectRequested { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ButtonRow.ArrangeRight(detail.ClientSize.Width - 16, 8, 76, connectButton, saveButton, closeButton);
            deleteButton.Width = deleteButton.PreferredWidth;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RebuildTree(_initialSiteId ?? SiteStore.Current.Sites.FirstOrDefault()?.Id);
            ShowSite(siteTree.SelectedSite);
        }

        private void RebuildTree(string selectId)
        {
            var groups = SiteStore.Current.Grouped()
                .Select(g => new KeyValuePair<string, List<Site>>(g.Key, g.OrderBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase).ToList()))
                .ToList();
            siteTree.SetSites(groups, selectId);
            if (selectId != null) siteTree.SelectSite(selectId, false);
        }

        // ------------------------------------------------------------------ selection

        private void siteTree_SelectionChanged(object sender, EventArgs e)
        {
            var next = siteTree.SelectedSite;
            if (next == null || next == _site) return;
            if (!ConfirmLeave())
            {
                if (_site != null) siteTree.SelectSite(_site.Id, false);
                return;
            }
            ShowSite(next);
        }

        /// <summary>Asks about unsaved edits; false keeps the current site.</summary>
        private bool ConfirmLeave()
        {
            if (!_dirty || _site == null) return true;
            var answer = Dialogs.Show(this, "Site manager", "Save the changes to " + _site.Name + "?", "Save", "Discard", "Cancel");
            if (answer == DialogResult.Cancel) return false;
            if (answer == DialogResult.OK) return SaveSite();
            _dirty = false;
            return true;
        }

        private void ShowSite(Site site)
        {
            _loading = true;
            try
            {
                _site = site;
                _dirty = false;
                _passwordTouched = false;
                bool has = site != null;
                detailTabs.Visible = has;
                generalCard.Visible = has && detailTabs.SelectedIndex == 0;
                advancedCard.Visible = has && detailTabs.SelectedIndex == 1;
                transferCard.Visible = has && detailTabs.SelectedIndex == 2;
                nameLabel.Visible = has;
                connectButton.Enabled = has;
                deleteButton.Visible = has;
                emptyLabel.Visible = !has;
                if (!has)
                {
                    connectedChip.Visible = false;
                    return;
                }

                nameLabel.Text = site.Name;
                nameLabel.Width = nameLabel.PreferredWidth + 4;
                connectedChip.Visible = _isConnected(site);
                connectedChip.Left = nameLabel.Right + 10;

                protocolBox.SelectedIndex = Math.Max(0, Array.IndexOf(ProtocolOrder, site.Protocol));
                hostBox.Text = site.Host ?? string.Empty;
                portBox.Text = site.Protocol == RemoteProtocol.Local ? string.Empty : site.Port.ToString();
                userBox.Text = site.User ?? string.Empty;
                keyBox.Text = string.IsNullOrEmpty(site.KeyFile) ? string.Empty : Format.HomeRelative(site.KeyFile);
                directoryBox.Text = site.RemoteDirectory ?? string.Empty;

                nameBox.Text = site.Name ?? string.Empty;
                groupBox.Text = site.Group ?? string.Empty;
                authBox.SelectedIndex = Math.Max(0, Array.IndexOf(AuthOrder, site.Auth));
                passwordBox.Text = CredentialStore.GetSecret(site) != null ? StoredPasswordMask : string.Empty;
                hostKeyValue.Text = string.IsNullOrEmpty(site.TrustedHostKey) ? "Not verified yet" : site.TrustedHostKey;
                forgetKeyButton.Enabled = !string.IsNullOrEmpty(site.TrustedHostKey);

                pairingsValue.Text = site.Pairings.Count == 0
                    ? "None"
                    : string.Join(", ", site.Pairings.Select(p => p.RemoteRoot + " ↔ " + Format.HomeRelative(p.LocalRoot)));
                forgetPairingsButton.Enabled = site.Pairings.Count > 0;
                bookmarksValue.Text = site.Bookmarks.Count == 0 ? "None" : string.Join(", ", site.Bookmarks);
                clearBookmarksButton.Enabled = site.Bookmarks.Count > 0;

                UpdateFieldStates();
            }
            finally
            {
                _loading = false;
                UpdateButtons();
            }
        }

        private void UpdateFieldStates()
        {
            var protocol = ProtocolOrder[Math.Max(0, protocolBox.SelectedIndex)];
            var auth = AuthOrder[Math.Max(0, authBox.SelectedIndex)];
            bool local = protocol == RemoteProtocol.Local;
            hostLabel.Text = local ? "Folder" : "Host";
            portBox.Enabled = !local;
            userBox.Enabled = !local && auth != AuthMethod.Anonymous;
            authBox.Enabled = !local;
            keyBox.Enabled = !local && auth == AuthMethod.KeyFile;
            passwordBox.Enabled = !local && auth == AuthMethod.Password;
            directoryBox.Enabled = true;
        }

        private void UpdateButtons()
        {
            saveButton.Enabled = _site != null && _dirty;
        }

        private void MarkDirty()
        {
            if (_loading) return;
            _dirty = true;
            UpdateButtons();
        }

        // ------------------------------------------------------------------ field events

        private void field_TextChanged(object sender, EventArgs e)
        {
            if (sender == nameBox && !_loading && _site != null)
            {
                // The heading follows the name as it is typed.
                nameLabel.Text = nameBox.Text.Trim().Length == 0 ? _site.Name : nameBox.Text.Trim();
                nameLabel.Width = nameLabel.PreferredWidth + 4;
                connectedChip.Left = nameLabel.Right + 10;
            }
            MarkDirty();
        }

        private void passwordBox_TextChanged(object sender, EventArgs e)
        {
            if (_loading) return;
            _passwordTouched = true;
            MarkDirty();
        }

        private void protocolBox_SelectedIndexChanged(object sender, SelectedIndexChangedEventArgs e)
        {
            if (!_loading)
            {
                var protocol = ProtocolOrder[Math.Max(0, protocolBox.SelectedIndex)];
                if (protocol == RemoteProtocol.Local) portBox.Text = string.Empty;
                else if (portBox.Text.Length == 0 || portBox.Text == "21" || portBox.Text == "22") portBox.Text = FtpClient.Remote.Site.DefaultPort(protocol).ToString();
            }
            UpdateFieldStates();
            MarkDirty();
        }

        private void authBox_SelectedIndexChanged(object sender, SelectedIndexChangedEventArgs e)
        {
            UpdateFieldStates();
            MarkDirty();
        }

        private void detailTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            generalCard.Visible = _site != null && detailTabs.SelectedIndex == 0;
            advancedCard.Visible = _site != null && detailTabs.SelectedIndex == 1;
            transferCard.Visible = _site != null && detailTabs.SelectedIndex == 2;
        }

        private void keyBox_ButtonClick(object sender, ModernTextBoxButtonEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a private key";
                dialog.Filter = "Private keys|id_*;*.pem;*.key;*.ppk|All files|*.*";
                var ssh = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh");
                if (Directory.Exists(ssh)) dialog.InitialDirectory = ssh;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                keyBox.Text = Format.HomeRelative(dialog.FileName);
            }
        }

        // ------------------------------------------------------------------ saving

        private static string ExpandHome(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            if (path.StartsWith("~", StringComparison.Ordinal)) path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + path.Substring(1);
            return path.Replace('/', '\\');
        }

        private bool SaveSite()
        {
            if (_site == null) return true;
            var name = nameBox.Text.Trim();
            var protocol = ProtocolOrder[Math.Max(0, protocolBox.SelectedIndex)];
            var host = hostBox.Text.Trim();
            var auth = AuthOrder[Math.Max(0, authBox.SelectedIndex)];
            int port = 0;

            string problem = null;
            if (name.Length == 0) problem = "Give the site a name; it is shown in the tab header.";
            else if (host.Length == 0) problem = protocol == RemoteProtocol.Local ? "Enter the folder to open." : "Enter the host to connect to.";
            else if (protocol == RemoteProtocol.Local && !Directory.Exists(ExpandHome(host))) problem = host + " does not exist.";
            else if (protocol != RemoteProtocol.Local && (!int.TryParse(portBox.Text.Trim(), out port) || port < 1 || port > 65535)) problem = "The port must be between 1 and 65535.";
            else if (protocol != RemoteProtocol.Local && auth == AuthMethod.KeyFile && !File.Exists(ExpandHome(keyBox.Text.Trim()))) problem = "The key file " + keyBox.Text.Trim() + " does not exist.";
            if (problem != null)
            {
                Dialogs.Warning(this, "Site manager", problem);
                return false;
            }

            _site.Name = name;
            _site.Group = groupBox.Text.Trim().Length == 0 ? null : groupBox.Text.Trim();
            _site.Protocol = protocol;
            _site.Host = protocol == RemoteProtocol.Local ? ExpandHome(host) : host;
            _site.Port = protocol == RemoteProtocol.Local ? 0 : port;
            _site.User = protocol == RemoteProtocol.Local ? null : userBox.Text.Trim();
            _site.Auth = protocol == RemoteProtocol.Local ? AuthMethod.Anonymous : auth;
            _site.KeyFile = _site.Auth == AuthMethod.KeyFile ? ExpandHome(keyBox.Text.Trim()) : null;
            _site.RemoteDirectory = directoryBox.Text.Trim().Length == 0 ? null : RemotePath.Normalize(directoryBox.Text.Trim());

            if (_passwordTouched && passwordBox.Text != StoredPasswordMask)
            {
                CredentialStore.SetSecret(_site, _site.Auth == AuthMethod.Password ? passwordBox.Text : null);
            }

            SiteStore.Current.AddOrUpdate(_site);
            _dirty = false;
            var id = _site.Id;
            RebuildTree(id);
            ShowSite(SiteStore.Current.Find(id));
            return true;
        }

        private void saveButton_Click(object sender, EventArgs e) => SaveSite();

        private void connectButton_Click(object sender, EventArgs e)
        {
            if (_site == null) return;
            if (_dirty && !SaveSite()) return;
            ConnectRequested = _site;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            if (!ConfirmLeave()) return;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // ------------------------------------------------------------------ rail commands

        private void newSiteButton_Click(object sender, EventArgs e)
        {
            if (!ConfirmLeave()) return;
            var names = new HashSet<string>(SiteStore.Current.Sites.Select(s => s.Name), StringComparer.CurrentCultureIgnoreCase);
            var name = "New site";
            for (int n = 2; names.Contains(name); n++) name = "New site " + n;
            var site = new Site { Name = name, Protocol = RemoteProtocol.Sftp, Port = 22, Group = _site?.Group };
            SiteStore.Current.AddOrUpdate(site);
            RebuildTree(site.Id);
            detailTabs.SelectedIndex = 0;
            ShowSite(site);
            hostBox.Focus();
        }

        private void folderButton_Click(object sender, EventArgs e)
        {
            if (_site == null) return;
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = "Move to folder";
                dialog.Prompt = "Folder for " + _site.Name + " (for example PRODUCTION); leave empty for none";
                dialog.Value = _site.Group ?? string.Empty;
                dialog.AllowEmpty = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                _site.Group = dialog.Value.Length == 0 ? null : dialog.Value;
            }
            SiteStore.Current.AddOrUpdate(_site);
            RebuildTree(_site.Id);
            _loading = true;
            groupBox.Text = _site.Group ?? string.Empty;
            _loading = false;
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (_site == null) return;
            if (_isConnected(_site))
            {
                Dialogs.Warning(this, "Delete site", "Close the " + _site.Name + " tab before deleting the site.");
                return;
            }
            if (!Dialogs.Confirm(this, "Delete site", "Delete " + _site.Name + "?\n\nIts saved password, bookmarks and pairings are removed. Nothing on the server changes.", "Delete")) return;
            SiteStore.Current.Remove(_site);
            _dirty = false;
            _site = null;
            var next = SiteStore.Current.Sites.FirstOrDefault()?.Id;
            RebuildTree(next);
            ShowSite(siteTree.SelectedSite);
        }

        private void forgetKeyButton_Click(object sender, EventArgs e)
        {
            if (_site == null) return;
            _site.TrustedHostKey = null;
            SiteStore.Current.AddOrUpdate(_site);
            hostKeyValue.Text = "Not verified yet";
            forgetKeyButton.Enabled = false;
        }

        private void forgetPairingsButton_Click(object sender, EventArgs e)
        {
            if (_site == null || _site.Pairings.Count == 0) return;
            if (!Dialogs.Confirm(this, "Forget pairings", "Forget the " + Format.Count(_site.Pairings.Count, "pairing") + " of " + _site.Name + "?\n\nThe deploy history is forgotten too; no files change.", "Forget")) return;
            _site.Pairings.Clear();
            SiteStore.Current.AddOrUpdate(_site);
            pairingsValue.Text = "None";
            forgetPairingsButton.Enabled = false;
        }

        private void clearBookmarksButton_Click(object sender, EventArgs e)
        {
            if (_site == null) return;
            _site.Bookmarks.Clear();
            SiteStore.Current.AddOrUpdate(_site);
            bookmarksValue.Text = "None";
            clearBookmarksButton.Enabled = false;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                closeButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                SaveSite();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
