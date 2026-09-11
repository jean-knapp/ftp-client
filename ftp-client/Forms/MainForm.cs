using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Git;
using FtpClient.Remote;
using FtpClient.Services;
using FtpClient.Transfers;
using FtpClient.Views;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// The shell: title bar, session tabs, and either a site's view or the connect screen. It owns
    /// the application's transfer queue and each site's protocol log.
    /// </summary>
    public partial class MainForm : ModernForm
    {
        private readonly List<SiteView> _views = new List<SiteView>();
        private readonly ConcurrentDictionary<string, SessionLog> _logs = new ConcurrentDictionary<string, SessionLog>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, string> _secrets = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, ProtocolLogDialog> _logWindows = new Dictionary<string, ProtocolLogDialog>(StringComparer.Ordinal);
        private TransferQueue _queue;
        private SiteView _activeView;
        private bool _showingConnect;
        private int _tabMenuIndex = -1;
        private Icon _titleIcon;
        private bool _connecting;
        private bool _restoringTabs;
        private readonly HashSet<SiteView> _starting = new HashSet<SiteView>();

        public MainForm()
        {
            InitializeComponent();

            var settings = AppSettings.Current;
            Theme.Mode = settings.Theme;
            Theme.Apply(skin);
            Theme.Changed += OnThemeChanged;
            if (!string.IsNullOrEmpty(settings.GitExecutable) && File.Exists(settings.GitExecutable)) GitRunner.GitExecutable = settings.GitExecutable;

            newConnectionItem.SvgIcon = Icons.Plus;
            openSiteItem.SvgIcon = Icons.Server;
            siteManagerItem.SvgIcon = Icons.Settings;
            settingsItem.SvgIcon = Icons.Settings;
            tabReconnectItem.SvgIcon = Icons.Refresh;
            tabLogItem.SvgIcon = Icons.Terminal;
            ApplyTitleIcon();
        }

        // ------------------------------------------------------------------ theme and icon

        private void OnThemeChanged(object sender, EventArgs e)
        {
            Theme.Apply(skin);
            Invalidate(true);
        }

        private void ApplyTitleIcon()
        {
            if (_titleIcon != null) return;
            try
            {
                using (var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("FtpClient.app.ico"))
                {
                    if (stream != null)
                    {
                        _titleIcon = new Icon(stream);
                        Icon = _titleIcon;
                        return;
                    }
                }
                var image = IconCache.Get(Icons.Server, 16, Theme.Palette.Lane);
                if (image == null) return;
                using (var bitmap = new Bitmap(image))
                {
                    _titleIcon = Icon.FromHandle(bitmap.GetHicon());
                    Icon = _titleIcon;
                }
            }
            catch
            {
                // The default icon will do.
            }
        }

        // ------------------------------------------------------------------ lifetime

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            RestoreWindowPosition();

            HostTrust.Prompt = ConfirmTrust;
            HostTrust.Trusted += HostTrust_Trusted;
            _queue = new TransferQueue(LogFor, (site, log) => RemoteSessionFactory.Create(site, log, SecretFor(site)));
            _queue.ConflictHandler = ResolveConflictAsync;
            ApplyQueueSettings(AppSettings.Current);
            SiteStore.Current.Changed += (s, args) => RefreshConnectView();

            var settings = AppSettings.Current;
            var tabs = settings.OpenTabs?.ToList() ?? new List<OpenTab>();
            int activeIndex = settings.ActiveTab;
            connectView.Visible = tabs.Count == 0;

            _restoringTabs = true;
            try
            {
                foreach (var tab in tabs)
                {
                    var site = tab.SiteId != null ? SiteStore.Current.Find(tab.SiteId) : tab.Site;
                    if (site == null) continue;
                    if (tab.SiteId == null) site.IsTransient = true;
                    if (_views.Any(v => v.CurrentSite.Id == site.Id)) continue;
                    var view = AddView(site, NewConnection(site));
                    view.StartPath = tab.Path;
                }
            }
            finally
            {
                _restoringTabs = false;
            }

            if (_views.Count == 0)
            {
                Activate(null);
                return;
            }
            // Only the tab in front connects now; the others connect when they are first shown.
            Activate(activeIndex >= 0 && activeIndex < _views.Count ? _views[activeIndex] : _views[0]);
        }

        private async Task StartViewAsync(SiteView view)
        {
            if (!_starting.Add(view)) return;
            try
            {
                var site = view.CurrentSite;
                if (NeedsPassword(site) && string.IsNullOrEmpty(SecretFor(site)))
                {
                    var secret = AskPassword(site);
                    if (secret == null)
                    {
                        view.ShowSignInNeeded();
                        RefreshTabs();
                        return;
                    }
                    _secrets[site.Id] = secret;
                }

                bool connected = false;
                try
                {
                    connected = await view.StartAsync();
                }
                catch (Exception ex)
                {
                    LogFor(site).Error(ex.Message);
                }
                // A refused password is asked for again next time rather than retried.
                if (!connected && view.Connection?.LastErrorKind == RemoteErrorKind.Authentication && NeedsPassword(site)) _secrets[site.Id] = string.Empty;
                RefreshTabs();
                SaveOpenTabs();
            }
            finally
            {
                _starting.Remove(view);
            }
        }

        private static bool NeedsPassword(Site site) => site.Protocol != RemoteProtocol.Local && site.Auth == AuthMethod.Password;

        /// <summary>Asks for a site's password; null when the user cancels.</summary>
        private string AskPassword(Site site)
        {
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = "Sign in to " + site.Name;
                dialog.Prompt = "Password for " + site.Endpoint;
                dialog.Password = true;
                dialog.AllowEmpty = true;
                return dialog.ShowDialog(this) == DialogResult.OK ? dialog.Value : null;
            }
        }

        /// <summary>Writes the tabs into the settings without saving them.</summary>
        private void StoreOpenTabs(AppSettings settings)
        {
            settings.OpenTabs = _views.Select(v => new OpenTab
            {
                SiteId = v.CurrentSite.IsTransient ? null : v.CurrentSite.Id,
                Site = v.CurrentSite.IsTransient ? v.CurrentSite : null,
                Path = v.PersistPath,
            }).ToList();
            settings.ActiveTab = _activeView == null ? -1 : _views.IndexOf(_activeView);
        }

        /// <summary>
        /// Saves the tabs straight away. Stopping the debugger or a crash never reaches the closing
        /// handler, so waiting for a clean exit would lose them.
        /// </summary>
        private void SaveOpenTabs()
        {
            if (_restoringTabs || IsDisposed) return;
            var settings = AppSettings.Current;
            StoreOpenTabs(settings);
            settings.Save();
        }

        private void RestoreWindowPosition()
        {
            var settings = AppSettings.Current;
            if (settings.WindowWidth > 400 && settings.WindowHeight > 300) Size = new Size(settings.WindowWidth, settings.WindowHeight);
            if (settings.WindowX >= 0 && settings.WindowY >= 0)
            {
                var bounds = new Rectangle(settings.WindowX, settings.WindowY, Width, Height);
                if (Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(bounds)))
                {
                    StartPosition = FormStartPosition.Manual;
                    Location = new Point(settings.WindowX, settings.WindowY);
                }
            }
            if (settings.WindowMaximized) WindowState = FormWindowState.Maximized;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_queue != null && e.CloseReason == CloseReason.UserClosing)
            {
                int running = _queue.Snapshot().Count(i => !i.IsFinished && i.Status != TransferStatus.Paused);
                if (running > 0 && !Dialogs.Confirm(this, "Quit", Format.Count(running, "transfer") + " still running. Quit anyway?\n\nInterrupted uploads can be resumed next time.", "Quit"))
                {
                    e.Cancel = true;
                    return;
                }
            }

            var settings = AppSettings.Current;
            settings.WindowMaximized = WindowState == FormWindowState.Maximized;
            if (WindowState == FormWindowState.Normal)
            {
                settings.WindowX = Location.X;
                settings.WindowY = Location.Y;
                settings.WindowWidth = Width;
                settings.WindowHeight = Height;
            }
            StoreOpenTabs(settings);
            _activeView?.StoreLayoutSettings(settings);
            settings.Theme = Theme.Mode;
            settings.Save();

            HostTrust.Prompt = null;
            HostTrust.Trusted -= HostTrust_Trusted;
            foreach (var window in _logWindows.Values.ToList()) window.Close();
            _queue?.Dispose();
            foreach (var view in _views) view.Connection?.Dispose();
            base.OnFormClosing(e);
        }

        // ------------------------------------------------------------------ sessions

        private SessionLog LogFor(Site site) => _logs.GetOrAdd(site?.Id ?? "none", _ => new SessionLog());

        private string SecretFor(Site site)
        {
            if (site == null) return null;
            if (_secrets.TryGetValue(site.Id, out var secret)) return secret;
            return site.IsTransient ? null : CredentialStore.GetSecret(site);
        }

        private SiteConnection NewConnection(Site site)
        {
            return new SiteConnection(site, LogFor(site), (s, log) => RemoteSessionFactory.Create(s, log, SecretFor(s)));
        }

        private void ApplyQueueSettings(AppSettings settings)
        {
            if (_queue == null) return;
            _queue.MaxConnectionsPerSite = Math.Max(1, Math.Min(16, settings.ConcurrentTransfers));
            _queue.ResumeTransfers = settings.ResumeTransfers;
            _queue.PreserveTimestamps = settings.PreserveTimestamps;
            _queue.SpeedLimitBytesPerSecond = settings.SpeedLimitEnabled ? Math.Max(1, settings.SpeedLimitKilobytes) * 1024L : 0;
        }

        /// <summary>
        /// Asks whether to trust a host key or certificate. Called on a connection's thread; the
        /// question is shown on the UI thread while that thread waits.
        /// </summary>
        private bool ConfirmTrust(TrustRequest request)
        {
            if (IsDisposed || !IsHandleCreated) return false;
            Func<bool> ask = () =>
            {
                var site = request.Site;
                var where = site.Name + " (" + site.Host + ":" + site.Port + ")";
                string title, message, trust;
                if (request.Kind == TrustKind.HostKey && !request.IsChange)
                {
                    title = "Verify the host key";
                    message = where + " presented an " + request.Algorithm + " host key this computer has not seen before:\n\n" +
                        request.Fingerprint + "\n\nCompare it with the fingerprint your server's administrator gave you. Trusting it lets future connections through without asking.";
                    trust = "Trust";
                }
                else if (request.Kind == TrustKind.HostKey)
                {
                    title = "The host key changed";
                    message = "The host key of " + where + " is not the one trusted before.\n\nBefore:  " + request.PreviousFingerprint + "\nNow:  " + request.Fingerprint +
                        "\n\nThis happens when a server is reinstalled, but it can also mean someone is intercepting the connection. Trust the new key only if you expected the change.";
                    trust = "Trust new key";
                }
                else
                {
                    title = request.IsChange ? "The certificate changed" : "Unverified certificate";
                    message = "The TLS certificate of " + where + " could not be verified: " + request.Problem + ".\n\nSubject:  " + request.Subject + "\nIssuer:  " + request.Issuer +
                        "\nThumbprint:  " + request.Fingerprint + (request.IsChange ? "\nTrusted before:  " + request.PreviousFingerprint : string.Empty) +
                        "\n\nTrust it only if you know this server uses a self-signed or private certificate.";
                    trust = "Trust certificate";
                }
                return Dialogs.Show(this, title, message, trust, null, "Cancel") == DialogResult.OK;
            };
            try
            {
                return InvokeRequired ? (bool)Invoke(ask) : ask();
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void HostTrust_Trusted(object sender, TrustRequest request)
        {
            if (IsDisposed || !IsHandleCreated) return;
            BeginInvoke((Action)(() =>
            {
                if (!request.Site.IsTransient && SiteStore.Current.Find(request.Site.Id) != null) SiteStore.Current.Save();
            }));
        }

        /// <summary>Shows the conflict dialog on the UI thread for a transfer worker.</summary>
        private Task<ConflictResolution> ResolveConflictAsync(ConflictInfo info)
        {
            var completion = new TaskCompletionSource<ConflictResolution>();
            if (IsDisposed || !IsHandleCreated)
            {
                completion.TrySetResult(new ConflictResolution(ConflictChoice.Skip));
                return completion.Task;
            }
            BeginInvoke((Action)(() =>
            {
                try
                {
                    using (var dialog = new TransferConflictDialog(info))
                    {
                        dialog.ShowDialog(this);
                        completion.TrySetResult(dialog.Resolution);
                    }
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
            }));
            return completion.Task;
        }

        private async Task<bool> OpenSiteAsync(Site site, string secret, bool remember)
        {
            if (site == null || _connecting) return false;

            var existing = site.IsTransient ? null : _views.FirstOrDefault(v => v.CurrentSite.Id == site.Id);
            if (existing != null)
            {
                _showingConnect = false;
                Activate(existing);
                return true;
            }

            if (!RemoteSessionFactory.IsSupported(site.Protocol))
            {
                connectView.SetStatus(SessionState.Failed, site.ProtocolName + " is not available in this build yet");
                Dialogs.Warning(this, "Connect", site.ProtocolName + " connections need a protocol library that has not been added to this build yet.\n\nLocal and network folders already work: choose \"Local folder\" as the protocol.");
                return false;
            }

            if (secret == null && site.Auth == AuthMethod.Password && site.Protocol != RemoteProtocol.Local)
            {
                secret = SecretFor(site);
                if (string.IsNullOrEmpty(secret))
                {
                    secret = AskPassword(site);
                    if (secret == null) return false;
                }
            }
            if (secret != null) _secrets[site.Id] = secret;

            var connection = NewConnection(site);
            _connecting = true;
            connectView.SetBusy(true, "Connecting to " + site.Name + "…");
            try
            {
                await connection.ConnectAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                connection.Dispose();
                if (ex is RemoteException remote && remote.Kind == RemoteErrorKind.Authentication) _secrets.TryRemove(site.Id, out _);
                connectView.SetBusy(false, null);
                connectView.SetStatus(SessionState.Failed, "Could not connect to " + site.Name + " — " + ex.Message);
                return false;
            }
            finally
            {
                _connecting = false;
            }
            connectView.SetBusy(false, null);

            if (remember) AppSettings.Current.AddRecentHost(RecentTarget(site));
            SiteStore.Current.Touch(site);

            var view = AddView(site, connection);
            _showingConnect = false;
            // Activating starts the tab on the connection just opened.
            Activate(view);
            return true;
        }

        private static string RecentTarget(Site site)
        {
            if (site.Protocol == RemoteProtocol.Local) return "file://" + site.Host;
            var scheme = site.Protocol == RemoteProtocol.Sftp ? "sftp" : site.Protocol == RemoteProtocol.Ftps ? "ftps" : "ftp";
            return scheme + "://" + (string.IsNullOrEmpty(site.User) ? string.Empty : site.User + "@") + site.Host + ":" + site.Port;
        }

        private SiteView AddView(Site site, SiteConnection connection)
        {
            var view = new SiteView { Dock = DockStyle.Fill, Visible = false };
            hostPanel.Controls.Add(view);
            view.Attach(site, connection, _queue);
            view.StateChanged += View_StateChanged;
            view.SettingsRequested += settingsItem_Click;
            view.LogRequested += View_LogRequested;
            view.PathChanged += View_PathChanged;
            view.ReconnectRequested += View_ReconnectRequested;
            view.SiteModified += View_PathChanged;
            view.HiddenFilesToggled += View_HiddenFilesToggled;
            _views.Add(view);
            RefreshTabs();
            SaveOpenTabs();
            return view;
        }

        private void View_StateChanged(object sender, EventArgs e)
        {
            RefreshTabs();
            UpdateTitle();
            connectView.RefreshConnectedState();
        }

        private void View_LogRequested(object sender, EventArgs e)
        {
            if (sender is SiteView view) ShowLog(view);
        }

        private void ShowLog(SiteView view)
        {
            var site = view.CurrentSite;
            if (_logWindows.TryGetValue(site.Id, out var open) && !open.IsDisposed)
            {
                if (open.WindowState == FormWindowState.Minimized) open.WindowState = FormWindowState.Normal;
                open.Activate();
                return;
            }
            var window = new ProtocolLogDialog(site, LogFor(site), view.Connection);
            window.FormClosed += (s, e) => _logWindows.Remove(site.Id);
            _logWindows[site.Id] = window;
            window.Show(this);
        }

        // ------------------------------------------------------------------ tabs

        private void RefreshTabs()
        {
            var tabs = _views.Select(v => new SessionTab
            {
                Title = v.TabTitle,
                Protocol = v.CurrentSite.Protocol == RemoteProtocol.Local ? "Local" : v.CurrentSite.ProtocolName,
                State = v.TabState,
                Tag = v,
            }).ToList();
            if (_showingConnect) tabs.Add(new SessionTab { Title = "New connection", State = SessionState.None });
            sessionTabs.Visible = _views.Count > 0;
            int selected = _activeView != null ? _views.IndexOf(_activeView) : tabs.Count - 1;
            sessionTabs.SetTabs(tabs, selected);
        }

        private void Activate(SiteView view)
        {
            if (_activeView != null && _activeView != view) _activeView.StoreLayoutSettings(AppSettings.Current);
            _activeView = view;
            foreach (var v in _views) v.Visible = ReferenceEquals(v, view);
            connectView.Visible = view == null;
            if (view != null)
            {
                view.BringToFront();
                view.ApplyLayoutSettings(AppSettings.Current);
                // Posted, so the tab is on screen before a password question appears over it.
                if (!view.StartRequested) BeginInvoke((Action)(() => { if (!view.IsDisposed) _ = StartViewAsync(view); }));
            }
            else
            {
                connectView.BringToFront();
                RefreshConnectView();
                connectView.FocusHost();
            }
            RefreshTabs();
            UpdateTitle();
            SaveOpenTabs();
        }

        private void UpdateTitle()
        {
            Text = _activeView != null ? _activeView.TabTitle + " — FTP Client" : "FTP Client";
        }

        private void RefreshConnectView()
        {
            if (connectView == null || IsDisposed) return;
            var sites = SiteStore.Current.Sites
                .OrderByDescending(s => _views.Any(v => v.CurrentSite.Id == s.Id))
                .ThenByDescending(s => s.LastUsed ?? DateTime.MinValue)
                .ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            connectView.SetSites(sites, IsConnected);
        }

        private void View_HiddenFilesToggled(object sender, EventArgs e)
        {
            foreach (var view in _views)
            {
                if (!ReferenceEquals(view, sender)) view.ApplyHiddenFiles();
            }
        }

        private bool IsConnected(Site site) => _views.Any(v => v.CurrentSite.Id == site.Id && v.TabState == SessionState.Connected);

        private void sessionTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = sessionTabs.SelectedIndex;
            Activate(index >= 0 && index < _views.Count ? _views[index] : null);
        }

        private void sessionTabs_TabCloseRequested(object sender, TabEventArgs e) => _ = CloseTabAsync(e.Index);

        private async Task CloseTabAsync(int index)
        {
            if (index == _views.Count && _showingConnect)
            {
                _showingConnect = false;
                Activate(_views.LastOrDefault());
                return;
            }
            if (index < 0 || index >= _views.Count) return;

            var view = _views[index];
            var site = view.CurrentSite;
            int running = _queue.Snapshot().Count(i => !i.IsFinished && i.Site.Id == site.Id);
            if (running > 0 && !Dialogs.Confirm(this, "Close " + site.Name, Format.Count(running, "transfer") + " to " + site.Name + " will keep running in the queue after the tab closes.", "Close tab"))
            {
                return;
            }

            view.StoreLayoutSettings(AppSettings.Current);
            _views.RemoveAt(index);
            view.StateChanged -= View_StateChanged;
            view.SettingsRequested -= settingsItem_Click;
            view.LogRequested -= View_LogRequested;
            view.PathChanged -= View_PathChanged;
            view.ReconnectRequested -= View_ReconnectRequested;
            view.SiteModified -= View_PathChanged;
            view.HiddenFilesToggled -= View_HiddenFilesToggled;
            if (_activeView == view) _activeView = null;
            hostPanel.Controls.Remove(view);
            if (_logWindows.TryGetValue(site.Id, out var window)) window.Close();

            var connection = view.Connection;
            view.Dispose();
            if (running == 0) _queue.CloseIdleConnections(site);
            if (site.IsTransient && running == 0) _secrets.TryRemove(site.Id, out _);
            // A quick-connect site's pairings and deploy history outlive its tab, for the next
            // connection to the server or for saving it as a site later.
            if (site.IsTransient) AppSettings.Current.RememberQuickConnect(site);

            if (_views.Count == 0) _showingConnect = false;
            Activate(_views.Count == 0 ? null : _views[Math.Min(index, _views.Count - 1)]);
            if (connection != null)
            {
                await connection.DisconnectAsync();
                connection.Dispose();
            }
        }

        private void sessionTabs_TabContextMenuRequested(object sender, TabEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _views.Count) return;
            _tabMenuIndex = e.Index;
            var view = _views[e.Index];
            tabReconnectItem.Text = view.TabState == SessionState.Connected ? "Reconnect" : "Connect";
            tabSaveSiteItem.Enabled = view.CurrentSite.IsTransient;
            tabSaveSiteItem.Text = view.CurrentSite.IsTransient ? "Save as site…" : "Saved as a site";
            tabCloseOthersItem.Enabled = _views.Count > 1;
            tabMenu.Show(sessionTabs, Cursor.Position);
        }

        private void sessionTabs_AddRequested(object sender, EventArgs e)
        {
            BuildOpenSiteMenu();
            addMenu.Show(sessionTabs, Cursor.Position);
        }

        private void BuildOpenSiteMenu()
        {
            var old = openSiteItem.SubItems.ToList();
            openSiteItem.SubItems.Clear();
            foreach (var item in old) item.Dispose();
            var sites = SiteStore.Current.Sites.OrderByDescending(s => s.LastUsed ?? DateTime.MinValue).Take(15).ToList();
            foreach (var site in sites)
            {
                var target = site;
                var item = new ModernContextMenuItem { Text = site.Name + "   " + site.Endpoint };
                item.Click += async (s, args) => await OpenSiteAsync(target, null, false);
                openSiteItem.SubItems.Add(item);
            }
            openSiteItem.Enabled = sites.Count > 0;
        }

        private void View_PathChanged(object sender, EventArgs e) => SaveOpenTabs();

        private void View_ReconnectRequested(object sender, EventArgs e)
        {
            if (sender is SiteView view) _ = ReconnectAsync(view);
        }

        private void tabRenameItem_Click(object sender, EventArgs e)
        {
            if (_tabMenuIndex >= 0 && _tabMenuIndex < _views.Count) RenameSite(_views[_tabMenuIndex]);
        }

        private void sessionTabs_TabDoubleClick(object sender, TabEventArgs e)
        {
            if (e.Index >= 0 && e.Index < _views.Count) RenameSite(_views[e.Index]);
        }

        /// <summary>Renames a tab's site; a saved site keeps the new name.</summary>
        private void RenameSite(SiteView view) => RenameSite(view.CurrentSite);

        /// <summary>Renames a site from its tab or from the saved sites list; an open tab follows.</summary>
        private void RenameSite(Site site)
        {
            if (site == null) return;
            var view = _views.FirstOrDefault(v => ReferenceEquals(v.CurrentSite, site));
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = view != null ? "Rename tab" : "Rename site";
                dialog.Prompt = site.IsTransient ? "Name for " + site.Endpoint : "Name for the saved site " + site.Name;
                dialog.Value = site.Name;
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Value.Length == 0 || dialog.Value == site.Name) return;
                site.Name = dialog.Value;
            }
            if (!site.IsTransient) SiteStore.Current.AddOrUpdate(site);
            view?.SiteChanged();
            RefreshTabs();
            UpdateTitle();
            SaveOpenTabs();
        }

        private void tabReconnectItem_Click(object sender, EventArgs e)
        {
            if (_tabMenuIndex < 0 || _tabMenuIndex >= _views.Count) return;
            var view = _views[_tabMenuIndex];
            _ = ReconnectAsync(view);
        }

        private async Task ReconnectAsync(SiteView view)
        {
            // Come back to the directory the tab is in, not where it first opened.
            view.StartPath = view.PersistPath;
            await view.DisconnectAsync();
            await StartViewAsync(view);
        }

        private void tabLogItem_Click(object sender, EventArgs e)
        {
            if (_tabMenuIndex >= 0 && _tabMenuIndex < _views.Count) ShowLog(_views[_tabMenuIndex]);
        }

        private void tabCloseItem_Click(object sender, EventArgs e) => _ = CloseTabAsync(_tabMenuIndex);

        private async void tabCloseOthersItem_Click(object sender, EventArgs e)
        {
            if (_tabMenuIndex < 0 || _tabMenuIndex >= _views.Count) return;
            var keep = _views[_tabMenuIndex];
            for (int i = _views.Count - 1; i >= 0; i--)
            {
                if (i < _views.Count && !ReferenceEquals(_views[i], keep)) await CloseTabAsync(i);
            }
        }

        // ------------------------------------------------------------------ commands

        private void newConnectionItem_Click(object sender, EventArgs e)
        {
            if (_views.Count > 0) _showingConnect = true;
            Activate(null);
        }

        private async void connectView_QuickConnectRequested(object sender, QuickConnectEventArgs e)
        {
            // A server used before without saving it gets its pairings, deploy history and bookmarks back.
            e.Site.AdoptHistory(QuickConnectTab(e.Site)?.CurrentSite ?? AppSettings.Current.FindQuickConnect(e.Site));
            await OpenSiteAsync(e.Site, e.Secret, e.Remember);
        }

        private async void connectView_SiteActivated(object sender, SiteEventArgs e) => await OpenSiteAsync(e.Site, null, false);

        private void connectView_SiteRenameRequested(object sender, SiteEventArgs e) => RenameSite(e.Site);

        private async void connectView_SiteDeleteRequested(object sender, SiteEventArgs e)
        {
            var site = e.Site;
            if (site == null || SiteStore.Current.Find(site.Id) == null) return;
            var message = "Delete " + site.Name + "?\n\nIts saved password, bookmarks, pairings and deploy history are removed. Nothing on the server changes.";
            if (_views.Any(v => v.CurrentSite.Id == site.Id)) message += "\n\nIts open tab is closed.";
            if (!Dialogs.Confirm(this, "Delete site", message, "Delete")) return;
            await DeleteSiteAsync(site);
        }

        /// <summary>Removes a saved site, closing its tab first. False when the tab stayed open.</summary>
        private async Task<bool> DeleteSiteAsync(Site site)
        {
            var view = _views.FirstOrDefault(v => v.CurrentSite.Id == site.Id);
            if (view != null)
            {
                bool onConnectScreen = _activeView == null;
                await CloseTabAsync(_views.IndexOf(view));
                // Running transfers can keep the tab open when the user says so.
                if (_views.Contains(view)) return false;
                // Deleting from the connect screen stays there rather than jumping to another tab.
                if (onConnectScreen && _views.Count > 0)
                {
                    _showingConnect = true;
                    Activate(null);
                }
            }
            SiteStore.Current.Remove(site);
            _secrets.TryRemove(site.Id, out _);
            SaveOpenTabs();
            connectView.SetStatus(SessionState.Disconnected, "Deleted " + site.Name);
            return true;
        }

        private void connectView_SaveSiteRequested(object sender, QuickConnectEventArgs e)
        {
            var name = AskSiteName(e.Site);
            if (name == null) return;
            var site = SaveFromConnectScreen(e.Site, name, e.Secret);
            connectView.SetStatus(SessionState.Disconnected, "Saved " + site.Name);
        }

        /// <summary>
        /// Saves the connect card's server. An open quick-connect tab on the same server becomes the
        /// saved site, so it stays connected and keeps its pairings and deploy history.
        /// </summary>
        private Site SaveFromConnectScreen(Site typed, string name, string secret)
        {
            var site = typed;
            var view = QuickConnectTab(typed);
            if (view != null)
            {
                site = view.CurrentSite;
                // The card only decides how to sign in when something was typed in its secret field.
                if (typed.Auth == AuthMethod.KeyFile || secret != null)
                {
                    site.Auth = typed.Auth;
                    site.KeyFile = typed.KeyFile;
                }
                if (!string.IsNullOrEmpty(typed.RemoteDirectory)) site.RemoteDirectory = typed.RemoteDirectory;
                if (secret == null) _secrets.TryGetValue(site.Id, out secret);
            }
            StoreAsSite(site, name, secret);
            return site;
        }

        /// <summary>An open quick-connect tab on the same server as <paramref name="site"/>, the one in front first.</summary>
        private SiteView QuickConnectTab(Site site)
        {
            return _views
                .Where(v => v.CurrentSite.IsTransient && !ReferenceEquals(v.CurrentSite, site) && v.CurrentSite.SameEndpoint(site))
                .OrderBy(v => v == _activeView ? 0 : 1)
                .FirstOrDefault();
        }

        private void tabSaveSiteItem_Click(object sender, EventArgs e)
        {
            if (_tabMenuIndex >= 0 && _tabMenuIndex < _views.Count) SaveTabAsSite(_views[_tabMenuIndex]);
        }

        /// <summary>Saves a quick-connect tab's site under a name the user gives.</summary>
        private void SaveTabAsSite(SiteView view)
        {
            var site = view.CurrentSite;
            if (!site.IsTransient) return;
            var name = AskSiteName(site);
            if (name == null) return;
            _secrets.TryGetValue(site.Id, out var secret);
            StoreAsSite(site, name, secret);
        }

        private string AskSiteName(Site site)
        {
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = "Save as site";
                dialog.Prompt = "Name for " + site.Endpoint + " in your saved sites";
                dialog.Value = site.Name;
                return dialog.ShowDialog(this) == DialogResult.OK && dialog.Value.Length > 0 ? dialog.Value : null;
            }
        }

        /// <summary>
        /// Turns a quick-connect site into a saved one. The same object is stored, so an open tab
        /// stays connected and keeps its pairings, bookmarks and trusted host key; a password given
        /// this session goes to Credential Manager.
        /// </summary>
        private void StoreAsSite(Site site, string name, string secret)
        {
            var view = _views.FirstOrDefault(v => ReferenceEquals(v.CurrentSite, site));
            site.Name = name;
            if (view != null) site.LastUsed = DateTime.Now;
            // Pairings and deploy history remembered from an earlier quick connection now belong to the saved site.
            site.AdoptHistory(AppSettings.Current.FindQuickConnect(site));
            AppSettings.Current.ForgetQuickConnect(site);
            SiteStore.Current.AddOrUpdate(site);
            if (site.Auth == AuthMethod.Password && !string.IsNullOrEmpty(secret)) CredentialStore.SetSecret(site, secret);
            if (view != null)
            {
                view.SiteChanged();
                RefreshTabs();
                UpdateTitle();
            }
            // A tab is now remembered by the site's id instead of carrying the site itself.
            SaveOpenTabs();
            RefreshConnectView();
        }

        private void connectView_ManageRequested(object sender, EventArgs e) => siteManagerItem_Click(sender, e);

        private async void siteManagerItem_Click(object sender, EventArgs e) =>
            await ManageSitesAsync(_activeView?.CurrentSite.IsTransient == false ? _activeView.CurrentSite.Id : null);

        private async void connectView_SiteEditRequested(object sender, SiteEventArgs e) => await ManageSitesAsync(e.Site?.Id);

        private async Task ManageSitesAsync(string selectId)
        {
            Site connect;
            using (var dialog = new SiteManagerDialog(IsConnected, selectId))
            {
                dialog.ShowDialog(this);
                connect = dialog.ConnectRequested;
            }
            foreach (var view in _views) view.SiteChanged();
            RefreshTabs();
            RefreshConnectView();
            if (connect != null) await OpenSiteAsync(connect, null, false);
        }

        private void settingsItem_Click(object sender, EventArgs e)
        {
            using (var dialog = new SettingsDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
            }
            var settings = AppSettings.Current;
            Theme.Mode = settings.Theme;
            GitRunner.GitExecutable = !string.IsNullOrEmpty(settings.GitExecutable) && File.Exists(settings.GitExecutable) ? settings.GitExecutable : GitRunner.FindGit();
            ApplyQueueSettings(settings);
            foreach (var view in _views) view.ApplySettings(settings);
        }

        // ------------------------------------------------------------------ shortcuts

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ModernShortcuts.YieldsToTextEntry(keyData, msg.HWnd)) return base.ProcessCmdKey(ref msg, keyData);

            switch (keyData)
            {
                case Keys.F5:
                    if (_activeView != null) _ = _activeView.RefreshAsync();
                    return true;
                case Keys.Control | Keys.T:
                case Keys.Control | Keys.N:
                    newConnectionItem_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.W:
                    if (_activeView != null) _ = CloseTabAsync(_views.IndexOf(_activeView));
                    else if (_showingConnect) _ = CloseTabAsync(_views.Count);
                    return true;
                case Keys.Control | Keys.Oemcomma:
                    settingsItem_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.S:
                    if (_activeView != null && _activeView.CurrentSite.IsTransient) SaveTabAsSite(_activeView);
                    return true;
                case Keys.Control | Keys.Tab:
                    if (_views.Count > 1 && _activeView != null) Activate(_views[(_views.IndexOf(_activeView) + 1) % _views.Count]);
                    return true;
                case Keys.Control | Keys.Shift | Keys.Tab:
                    if (_views.Count > 1 && _activeView != null) Activate(_views[(_views.IndexOf(_activeView) - 1 + _views.Count) % _views.Count]);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
