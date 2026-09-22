using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Deploy;
using FtpClient.Forms;
using FtpClient.Git;
using FtpClient.Remote;
using FtpClient.Services;
using FtpClient.Transfers;
using ModernWinForms;
using RemoteSite = FtpClient.Remote.Site;

namespace FtpClient.Views
{
    /// <summary>
    /// One session tab: the command bar, the browser card (path bar, Files and Commits, the remote
    /// tree), the docked transfer queue and the status bar.
    /// </summary>
    public partial class SiteView : ModernUserControl
    {
        private const int FilesTab = 0;
        private const int CommitsTab = 1;
        private const int CommitHistoryLimit = 500;
        private const int CollapsedQueueHeight = 46;

        private RemoteSite _site;
        private SiteConnection _connection;
        private TransferQueue _queue;
        private string _currentPath = RemotePath.Root;
        private readonly List<string> _back = new List<string>();
        private readonly List<string> _forward = new List<string>();
        private readonly Dictionary<string, List<RemoteEntry>> _cache = new Dictionary<string, List<RemoteEntry>>(StringComparer.Ordinal);
        private readonly HashSet<string> _treeLoading = new HashSet<string>(StringComparer.Ordinal);
        private CancellationTokenSource _listCancellation;
        private bool _queueDirty = true;
        private bool _refreshAfterTransfers;
        private int _queueHeight = 324;
        private bool _queueCollapsed;
        private bool _restoringLayout;
        private DateTime _lastKeepAlive = DateTime.Now;
        private string _busyText;

        // Links: the entry picked as a link target, and the last link pasted from it.
        private RemoteEntry _linkTarget;
        private string _lastLinkPath;

        // Commits
        private DeployPlanner _planner;
        private string _branchName;
        private List<CommitEntry> _commits = new List<CommitEntry>();
        private string _commitsLoadedFor;
        private CancellationTokenSource _commitsCancellation;
        private CancellationTokenSource _filesCancellation;
        private CommitEntry _shownCommit;
        private readonly HashSet<string> _deploying = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Pairing> _deployPairings = new Dictionary<string, Pairing>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> _staging = new Dictionary<string, string>(StringComparer.Ordinal);

        // The paired repository is watched, so a commit made anywhere shows up here on its own.
        private RepositoryWatcher _repositoryWatcher;
        private readonly System.Windows.Forms.Timer _commitWatchTimer = new System.Windows.Forms.Timer { Interval = 400 };
        private string _watchedGitDirectory;
        private string _watchedRoot;
        private string _revisionSha;
        private bool _checkingRevision;

        // A safety net under the file watcher: some drives, antivirus filters and tools never raise
        // its events, so the tip of the branch is also looked at every few seconds.
        private readonly System.Windows.Forms.Timer _commitPollTimer = new System.Windows.Forms.Timer { Interval = 5000 };

        /// <summary>The connection state or the site's name changed.</summary>
        public event EventHandler StateChanged;

        /// <summary>A directory was opened, so the tab's place can be remembered.</summary>
        public event EventHandler PathChanged;

        /// <summary>A quick-connect site's pairings, bookmarks or deploy history changed and need saving with the tabs.</summary>
        public event EventHandler SiteModified;

        /// <summary>Show hidden files was switched from this tab.</summary>
        public event EventHandler HiddenFilesToggled;

        /// <summary>Refresh was pressed without a connection; the main window reconnects, asking for a password if needed.</summary>
        public event EventHandler ReconnectRequested;

        private bool _navigated;

        /// <summary>The directory to open instead of the site's own, e.g. where a restored tab was.</summary>
        public string StartPath { get; set; }

        /// <summary>The tab has begun connecting; restored tabs wait until they are first shown.</summary>
        public bool StartRequested { get; private set; }

        /// <summary>The directory to remember for this tab.</summary>
        public string PersistPath => _navigated ? _currentPath : StartPath ?? _currentPath;
        public event EventHandler SettingsRequested;
        public event EventHandler LogRequested;

        public SiteView()
        {
            InitializeComponent();
            queueTabs.Tabs[3].BadgeKind = TabBadgeKind.Error;
            contentTabs.SetEnabled(CommitsTab, false);
            ApplySplitColors();
            Theme.Changed += Theme_Changed;
            _commitWatchTimer.Tick += commitWatchTimer_Tick;
            _commitPollTimer.Tick += commitPollTimer_Tick;
            _commitPollTimer.Start();
        }

        private ModernSkin _splitSkin;

        private void Theme_Changed(object sender, EventArgs e) => ApplySplitColors();

        /// <summary>
        /// The tree / list splitter sits inside the browser card, so it takes the card's colour
        /// rather than the body's; its grip dots are derived from that.
        /// </summary>
        private void ApplySplitColors()
        {
            var p = Theme.Palette;
            // The split container is a skin container: its own copy of the theme skin keeps the
            // controls and menus inside it skinned while the splitter takes the card's colour.
            if (_splitSkin == null) _splitSkin = new ModernSkin();
            Theme.Apply(_splitSkin);
            _splitSkin.SplitContainer.Colors.SplitterColor = p.Layer;
            _splitSkin.SplitContainer.Colors.PressedColor = p.Fill2On(p.Layer);
            workSplit.Skin = _splitSkin;
            workSplit.Colors.SplitterColor = p.Layer;
            workSplit.Colors.PressedColor = p.Fill2On(p.Layer);
            workSplit.BackColor = p.Layer;
            workSplit.Panel1.BackColor = p.Layer;
            workSplit.Panel2.BackColor = p.Layer;
            workSplit.Invalidate();
        }

        // ------------------------------------------------------------------ public surface

        public RemoteSite CurrentSite => _site;
        public SiteConnection Connection => _connection;
        public string CurrentPath => _currentPath;
        public string TabTitle => _site?.Name ?? "Site";
        public SessionState TabState => _connection == null ? SessionState.Disconnected : MapState(_connection.State);

        public static SessionState MapState(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connected: return SessionState.Connected;
                case ConnectionState.Connecting: return SessionState.Connecting;
                case ConnectionState.Failed: return SessionState.Failed;
                default: return SessionState.Disconnected;
            }
        }

        public void Attach(RemoteSite site, SiteConnection connection, TransferQueue queue)
        {
            _site = site ?? throw new ArgumentNullException(nameof(site));
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));

            _connection.StateChanged += Connection_StateChanged;
            _queue.ItemAdded += Queue_ItemEvent;
            _queue.ItemChanged += Queue_ItemEvent;
            _queue.ItemsRemoved += Queue_ItemsRemoved;
            _queue.BatchSettled += Queue_BatchSettled;

            tree.IsPaired = path => _site.PairingAt(path) != null;
            breadcrumb.Uri = site.Uri;
            ApplySettings(AppSettings.Current);
            UpdateLocationUi();
            UpdateStatusBar();
            UpdateCommandStates();
            RefreshQueueView();
            statusTimer.Start();
        }

        /// <summary>Connects if needed and opens the starting directory. False when the connection failed.</summary>
        public async Task<bool> StartAsync()
        {
            StartRequested = true;
            SetEmptyText("Connecting to " + _site.Name + "…");
            try
            {
                if (_connection.State != ConnectionState.Connected) await _connection.ConnectAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                SetEmptyText("Could not connect: " + ex.Message + "\nPress Refresh to try again.");
                UpdateStatusBar();
                return false;
            }

            var start = await ChooseStartDirectoryAsync();
            await NavigateAsync(start, false, true);
            if (start != RemotePath.Root) _ = LoadTreeChildrenAsync(RemotePath.Root);
            _ = LoadCommitsAsync(false);
            return true;
        }

        /// <summary>Shown when a restored tab's password was not given.</summary>
        public void ShowSignInNeeded()
        {
            SetEmptyText("Not signed in to " + _site.Name + ".\nPress Refresh to enter the password.");
            UpdateStatusBar();
        }

        private async Task<string> ChooseStartDirectoryAsync()
        {
            if (!string.IsNullOrWhiteSpace(StartPath))
            {
                try
                {
                    var start = RemotePath.Normalize(StartPath);
                    var entry = await _connection.RunAsync(s => s.StatAsync(start, CancellationToken.None), CancellationToken.None);
                    if (entry != null && entry.IsDirectory) return start;
                }
                catch
                {
                    // The remembered directory is gone; the site's own start applies.
                }
            }
            if (!string.IsNullOrWhiteSpace(_site.RemoteDirectory)) return RemotePath.Normalize(_site.RemoteDirectory);
            if (_site.Protocol == RemoteProtocol.Local) return RemotePath.Root;
            var fallback = AppSettings.Current.DefaultRemoteDirectory;
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                try
                {
                    var entry = await _connection.RunAsync(s => s.StatAsync(RemotePath.Normalize(fallback), CancellationToken.None), CancellationToken.None);
                    if (entry != null && entry.IsDirectory) return RemotePath.Normalize(fallback);
                }
                catch
                {
                    // The home directory will do.
                }
            }
            return RemotePath.Normalize(_connection.HomeDirectory);
        }

        /// <summary>Flips Show hidden files; the main window passes it on to the other tabs.</summary>
        private void ToggleHiddenFiles()
        {
            var settings = AppSettings.Current;
            settings.ShowHiddenFiles = !settings.ShowHiddenFiles;
            settings.Save();
            ApplyHiddenFiles();
            HiddenFilesToggled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Shows or hides dotfiles such as .htaccess after the setting changed.</summary>
        public void ApplyHiddenFiles()
        {
            bool show = AppSettings.Current.ShowHiddenFiles;
            fileList.ShowHidden = show;
            tree.ShowHidden = show;
            if (_cache.TryGetValue(_currentPath, out var entries)) ShowEntries(_currentPath, entries);
        }

        public void ApplySettings(AppSettings settings)
        {
            fileList.RowHeight = settings.FileRowHeight;
            fileList.ShowHidden = settings.ShowHiddenFiles;
            tree.ShowHidden = settings.ShowHiddenFiles;
            _queue?.CloseIdleConnections(null);
            if (_cache.TryGetValue(_currentPath, out var entries)) ShowEntries(_currentPath, entries);
            _queueDirty = true;
            RefreshQueueView();
            UpdateStatusBar();
        }

        public void ApplyLayoutSettings(AppSettings settings)
        {
            _restoringLayout = true;
            try
            {
                SetWorkSplitDistance(Math.Max(180, Math.Min(640, settings.TreePaneWidth)));
                commitDetail.Width = Math.Max(360, Math.Min(900, settings.CommitDetailWidth));
                _queueHeight = Math.Max(120, settings.QueueHeight);
                _queueCollapsed = settings.QueueCollapsed;
                ApplyQueueHeight();
                viewToggle.SelectedIndex = settings.FileViewMode == 1 ? 1 : 0;
                SetViewMode(viewToggle.SelectedIndex);
            }
            finally
            {
                _restoringLayout = false;
            }
        }

        public void StoreLayoutSettings(AppSettings settings)
        {
            settings.TreePaneWidth = workSplit.SplitterDistance;
            settings.CommitDetailWidth = commitDetail.Width;
            settings.QueueHeight = _queueHeight;
            settings.QueueCollapsed = _queueCollapsed;
            settings.FileViewMode = viewToggle.SelectedIndex;
        }

        public async Task RefreshAsync()
        {
            if (_connection == null || _connection.State != ConnectionState.Connected)
            {
                ReconnectRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            _cache.Remove(_currentPath);
            await NavigateAsync(_currentPath, false, true);
            if (contentTabs.SelectedIndex == CommitsTab) await LoadCommitsAsync(true);
        }

        /// <summary>Called when the site was edited in the site manager.</summary>
        public void SiteChanged()
        {
            breadcrumb.Uri = _site.Uri;
            tree.Invalidate();
            UpdatePairingUi();
            UpdateStatusBar();
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public async Task DisconnectAsync()
        {
            _listCancellation?.Cancel();
            _commitsCancellation?.Cancel();
            _filesCancellation?.Cancel();
            if (_connection != null) await _connection.DisconnectAsync();
        }

        // ------------------------------------------------------------------ connection

        private void Connection_StateChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                if (IsDisposed) return;
                UpdateStatusBar();
                UpdateCommandStates();
                UpdateDeployFooter();
                StateChanged?.Invoke(this, EventArgs.Empty);
            }));
        }

        private bool Connected => _connection != null && _connection.State == ConnectionState.Connected;

        // ------------------------------------------------------------------ navigation

        private async Task NavigateAsync(string path, bool pushHistory, bool force)
        {
            path = RemotePath.Normalize(path);
            if (pushHistory && path != _currentPath)
            {
                _back.Add(_currentPath);
                if (_back.Count > 100) _back.RemoveAt(0);
                _forward.Clear();
            }
            bool changed = path != _currentPath;
            _currentPath = path;
            _listCancellation?.Cancel();
            var cancellation = _listCancellation = new CancellationTokenSource();

            UpdateLocationUi();
            if (_cache.TryGetValue(path, out var cached))
            {
                ShowEntries(path, cached);
            }
            else if (changed || force)
            {
                SetEmptyText("Loading…");
                fileList.SetEntries(path, null);
                ShowEntrySummary(null);
            }

            try
            {
                var entries = await _connection.RunAsync(s => s.ListAsync(path, cancellation.Token), cancellation.Token);
                if (cancellation.IsCancellationRequested || path != _currentPath) return;
                var list = entries.ToList();
                _cache[path] = list;
                SetEmptyText("This directory is empty.");
                ShowEntries(path, list);
                tree.SetChildren(path, list);
                tree.Expand(path);
                _navigated = true;
                PathChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (path != _currentPath) return;
                SetEmptyText(ex.Message);
                if (!_cache.ContainsKey(path))
                {
                    fileList.SetEntries(path, null);
                    ShowEntrySummary(null);
                }
            }
            finally
            {
                UpdateStatusBar();
                UpdateCommandStates();
            }
        }

        private void SetEmptyText(string text)
        {
            fileList.EmptyText = text;
            tileView.EmptyText = text;
            fileList.Invalidate();
            tileView.Invalidate();
        }

        private async Task LoadTreeChildrenAsync(string path)
        {
            path = RemotePath.Normalize(path);
            if (_cache.TryGetValue(path, out var cached))
            {
                tree.SetChildren(path, cached);
                return;
            }
            if (!_treeLoading.Add(path)) return;
            try
            {
                var entries = await _connection.RunAsync(s => s.ListAsync(path, CancellationToken.None), CancellationToken.None);
                var list = entries.ToList();
                _cache[path] = list;
                tree.SetChildren(path, list);
            }
            catch
            {
                // The directory cannot be listed; the tree keeps its chevron.
            }
            finally
            {
                _treeLoading.Remove(path);
            }
        }

        private void UpdateLocationUi()
        {
            breadcrumb.Path = _currentPath;
            tree.SelectPath(_currentPath);
            backButton.Enabled = _back.Count > 0;
            forwardButton.Enabled = _forward.Count > 0;
            upButton.Enabled = _currentPath != RemotePath.Root;
            dropTarget.TargetPath = _currentPath;
            UpdatePairingUi();
        }

        private void ShowEntries(string path, List<RemoteEntry> entries)
        {
            fileList.SetEntries(path, entries);
            ShowEntrySummary(entries);
            UpdateCommandStates();
            UpdateTransferringRows();
        }

        private void ShowEntrySummary(List<RemoteEntry> entries)
        {
            if (entries == null)
            {
                breadcrumb.InfoText = null;
                contentTabs.SetBadge(FilesTab, null);
                return;
            }
            bool hidden = AppSettings.Current.ShowHiddenFiles;
            var visible = entries.Where(e => hidden || !e.IsHidden).ToList();
            long bytes = visible.Where(e => !e.IsDirectory).Sum(e => e.Size);
            int hiddenCount = entries.Count - visible.Count;
            // Say what the list leaves out, so a missing .htaccess is not a mystery.
            breadcrumb.InfoText = Format.Count(visible.Count, "item") + "  ·  " + Format.Bytes(bytes)
                + (hiddenCount > 0 ? "  ·  " + hiddenCount.ToString("N0") + " hidden (Ctrl+H)" : string.Empty);
            contentTabs.SetBadge(FilesTab, visible.Count.ToString("N0"));
            LayoutTabsBar();
        }

        private void InvalidateCache(string path)
        {
            var normalized = RemotePath.Normalize(path);
            foreach (var key in _cache.Keys.Where(k => RemotePath.IsWithin(k, normalized)).ToList()) _cache.Remove(key);
            _cache.Remove(RemotePath.Parent(normalized));
        }

        private Task ReloadAsync()
        {
            _cache.Remove(_currentPath);
            return NavigateAsync(_currentPath, false, true);
        }

        private async void backButton_Click(object sender, EventArgs e)
        {
            if (_back.Count == 0) return;
            var target = _back[_back.Count - 1];
            _back.RemoveAt(_back.Count - 1);
            _forward.Add(_currentPath);
            await NavigateAsync(target, false, false);
        }

        private async void forwardButton_Click(object sender, EventArgs e)
        {
            if (_forward.Count == 0) return;
            var target = _forward[_forward.Count - 1];
            _forward.RemoveAt(_forward.Count - 1);
            _back.Add(_currentPath);
            await NavigateAsync(target, false, false);
        }

        private async void upButton_Click(object sender, EventArgs e)
        {
            if (_currentPath == RemotePath.Root) return;
            var child = RemotePath.Name(_currentPath);
            await NavigateAsync(RemotePath.Parent(_currentPath), true, false);
            SelectNames(new[] { child });
        }

        private async void breadcrumb_PathRequested(object sender, PathEventArgs e) => await NavigateAsync(e.Path, true, false);

        private async void tree_NodeSelected(object sender, TreeNodeEventArgs e)
        {
            if (e.Node.Path != _currentPath) await NavigateAsync(e.Node.Path, true, false);
        }

        private async void tree_ExpandRequested(object sender, TreeNodeEventArgs e) => await LoadTreeChildrenAsync(e.Node.Path);

        private async void treeRefreshButton_Click(object sender, EventArgs e)
        {
            _cache.Clear();
            tree.Reset();
            await NavigateAsync(_currentPath, false, true);
            if (_currentPath != RemotePath.Root) await LoadTreeChildrenAsync(RemotePath.Root);
        }

        private void treeCollapseButton_Click(object sender, EventArgs e) => tree.CollapseAll();

        private async void refreshButton_Click(object sender, EventArgs e) => await RefreshAsync();

        private async void entry_Activated(object sender, RemoteEntryEventArgs e)
        {
            var entry = e.Entry;
            if (entry.Name == "..")
            {
                upButton_Click(sender, EventArgs.Empty);
                return;
            }
            if (entry.IsDirectory)
            {
                await NavigateAsync(entry.FullPath, true, false);
                return;
            }
            if (entry.Kind == RemoteEntryKind.Symlink)
            {
                // A link to a directory opens it; a link to a file downloads.
                try
                {
                    var listing = await _connection.RunAsync(s => s.ListAsync(entry.FullPath, CancellationToken.None), CancellationToken.None);
                    _cache[entry.FullPath] = listing.ToList();
                    await NavigateAsync(entry.FullPath, true, false);
                    return;
                }
                catch (RemoteException)
                {
                }
            }
            await DownloadAsync(new List<RemoteEntry> { entry });
        }

        private void list_ParentRequested(object sender, EventArgs e) => upButton_Click(sender, e);

        // ------------------------------------------------------------------ selection and view

        private List<RemoteEntry> SelectedEntries => viewToggle.SelectedIndex == 1 ? tileView.SelectedEntries : fileList.SelectedEntries;

        private void SelectNames(IEnumerable<string> names)
        {
            var list = names.ToList();
            fileList.SelectNames(list);
            tileView.SelectNames(list);
        }

        private void list_SelectionChanged(object sender, EventArgs e) => UpdateCommandStates();

        private void fileList_RowsChanged(object sender, EventArgs e) => tileView.SetRows(fileList.Directory, fileList.DisplayedRows);

        private void viewToggle_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetViewMode(viewToggle.SelectedIndex);
            if (!_restoringLayout) AppSettings.Current.FileViewMode = viewToggle.SelectedIndex;
        }

        private void SetViewMode(int mode)
        {
            bool grid = mode == 1;
            tileView.Visible = grid;
            fileList.Visible = !grid;
            if (grid) tileView.SetRows(fileList.Directory, fileList.DisplayedRows);
            UpdateCommandStates();
        }

        private void list_DragHoverChanged(object sender, bool over) => dropTarget.Highlighted = over;

        private void filterBox_TextChanged(object sender, EventArgs e)
        {
            if (contentTabs.SelectedIndex == CommitsTab) ApplyCommitFilter(null);
            else fileList.Filter = filterBox.Text;
        }

        private void UpdateCommandStates()
        {
            if (_site == null) return;
            var selection = SelectedEntries;
            bool connected = Connected;
            uploadButton.Enabled = connected;
            downloadButton.Enabled = connected && selection.Count > 0;
            downloadButton.BadgeText = selection.Count > 0 ? selection.Count.ToString("N0") : null;
            newFolderButton.Enabled = connected;
            renameButton.Enabled = connected && selection.Count == 1;
            deleteButton.Enabled = connected && selection.Count > 0;
            permissionsButton.Enabled = connected && selection.Count > 0;
            dropTarget.Enabled = connected;
            LayoutCommandBar();
        }

        // ------------------------------------------------------------------ uploads

        private void uploadButton_Click(object sender, EventArgs e) => PickAndUploadFiles(_currentPath);

        private void uploadFilesItem_Click(object sender, EventArgs e) => PickAndUploadFiles(_currentPath);

        private void uploadFolderItem_Click(object sender, EventArgs e) => PickAndUploadFolder(_currentPath);

        private void PickAndUploadFiles(string remoteDirectory)
        {
            if (!Connected) return;
            using (var dialog = new OpenFileDialog())
            {
                dialog.Multiselect = true;
                dialog.Title = "Upload to " + remoteDirectory;
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                EnqueueUploads(dialog.FileNames, remoteDirectory);
            }
        }

        private void PickAndUploadFolder(string remoteDirectory)
        {
            if (!Connected) return;
            var folder = FolderPicker.Show(FindForm(), "Choose a folder to upload to " + remoteDirectory, null);
            if (folder != null) EnqueueUploads(new[] { folder }, remoteDirectory);
        }

        private void dropTarget_Click(object sender, EventArgs e) => PickAndUploadFiles(_currentPath);

        private void dropTarget_FilesDropped(object sender, FilesDroppedEventArgs e) => EnqueueUploads(e.Paths, _currentPath);

        private void list_FilesDropped(object sender, RemoteDropEventArgs e) => EnqueueUploads(e.Paths, e.TargetDirectory);

        private void EnqueueUploads(IEnumerable<string> localPaths, string remoteDirectory)
        {
            if (!Connected) return;
            int count = 0;
            var emptyDirectories = new List<string>();
            foreach (var local in localPaths)
            {
                if (File.Exists(local))
                {
                    _queue.EnqueueUpload(_site, local, RemotePath.Combine(remoteDirectory, Path.GetFileName(local)));
                    count++;
                }
                else if (Directory.Exists(local))
                {
                    var root = new DirectoryInfo(local);
                    var remoteRoot = RemotePath.Combine(remoteDirectory, root.Name);
                    bool any = false;
                    foreach (var file in EnumerateFiles(root))
                    {
                        var relative = file.FullName.Substring(root.FullName.TrimEnd('\\').Length).TrimStart('\\').Replace('\\', '/');
                        _queue.EnqueueUpload(_site, file.FullName, RemotePath.Combine(remoteRoot, relative));
                        count++;
                        any = true;
                    }
                    if (!any) emptyDirectories.Add(remoteRoot);
                }
            }
            foreach (var directory in emptyDirectories) _ = CreateDirectoryQuietlyAsync(directory);
            if (count > 0)
            {
                queueTabs.SelectedIndex = 0;
                SetQueueCollapsed(false);
            }
        }

        private static IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo root)
        {
            var pending = new Stack<DirectoryInfo>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                FileSystemInfo[] children;
                try { children = directory.GetFileSystemInfos(); }
                catch (UnauthorizedAccessException) { continue; }
                catch (IOException) { continue; }
                foreach (var child in children)
                {
                    if ((child.Attributes & FileAttributes.ReparsePoint) != 0 && child is DirectoryInfo) continue;
                    if (child is DirectoryInfo sub) pending.Push(sub);
                    else if (child is FileInfo file) yield return file;
                }
            }
        }

        private async Task CreateDirectoryQuietlyAsync(string path)
        {
            try
            {
                await _connection.RunAsync(s => s.CreateDirectoryAsync(path, CancellationToken.None), CancellationToken.None);
                InvalidateCache(path);
                if (RemotePath.Parent(path) == _currentPath) await ReloadAsync();
            }
            catch (Exception ex)
            {
                _connection.Log.Error("Could not create " + path + ": " + ex.Message);
            }
        }

        // ------------------------------------------------------------------ downloads

        private async void downloadButton_Click(object sender, EventArgs e) => await DownloadAsync(SelectedEntries);

        private static string SafeLocalName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            var result = new string(chars).TrimEnd(' ', '.');
            return result.Length == 0 ? "_" : result;
        }

        private async Task DownloadAsync(List<RemoteEntry> entries)
        {
            if (!Connected || entries == null || entries.Count == 0) return;
            var target = KnownFolders.Downloads;
            int count = 0;
            SetBusy("Preparing downloads…");
            try
            {
                foreach (var entry in entries)
                {
                    var local = Path.Combine(target, SafeLocalName(entry.Name));
                    if (entry.IsDirectory) count += await EnqueueDirectoryDownloadAsync(entry.FullPath, local);
                    else
                    {
                        _queue.EnqueueDownload(_site, entry.FullPath, local, entry.Size);
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Dialogs.Error(FindForm(), "Download", ex.Message);
            }
            finally
            {
                SetBusy(null);
            }
            if (count > 0)
            {
                queueTabs.SelectedIndex = 0;
                SetQueueCollapsed(false);
            }
        }

        private async Task<int> EnqueueDirectoryDownloadAsync(string remote, string local)
        {
            Directory.CreateDirectory(local);
            var entries = await _connection.RunAsync(s => s.ListAsync(remote, CancellationToken.None), CancellationToken.None);
            int count = 0;
            foreach (var entry in entries)
            {
                if (entry.Name == "." || entry.Name == "..") continue;
                var childLocal = Path.Combine(local, SafeLocalName(entry.Name));
                if (entry.IsDirectory) count += await EnqueueDirectoryDownloadAsync(entry.FullPath, childLocal);
                else
                {
                    _queue.EnqueueDownload(_site, entry.FullPath, childLocal, entry.Size);
                    count++;
                }
            }
            return count;
        }

        // ------------------------------------------------------------------ file operations

        private static string CheckName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Enter a name.";
            if (name == "." || name == "..") return "That name is reserved.";
            if (name.Contains("/")) return "A name cannot contain a slash.";
            return null;
        }

        private void SetBusy(string text)
        {
            _busyText = text;
            UpdateRightStatus();
        }

        private async void newFolderButton_Click(object sender, EventArgs e) => await NewFolderAsync(_currentPath);

        private async Task NewFolderAsync(string parent)
        {
            if (!Connected) return;
            string name;
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = "New folder";
                dialog.Prompt = "Name of the new folder in " + parent;
                dialog.Value = "New folder";
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                name = dialog.Value;
            }
            var problem = CheckName(name);
            if (problem != null)
            {
                Dialogs.Warning(FindForm(), "New folder", problem);
                return;
            }
            var path = RemotePath.Combine(parent, name);
            try
            {
                await _connection.RunAsync(s => s.CreateDirectoryAsync(path, CancellationToken.None), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Dialogs.Error(FindForm(), "New folder", ex.Message);
                return;
            }
            InvalidateCache(path);
            if (parent == _currentPath)
            {
                await ReloadAsync();
                SelectNames(new[] { name });
            }
        }

        private async void renameButton_Click(object sender, EventArgs e) => await RenameAsync();

        private async Task RenameAsync()
        {
            var selection = SelectedEntries;
            if (!Connected || selection.Count != 1) return;
            var entry = selection[0];
            string name;
            using (var dialog = new TextInputDialog())
            {
                dialog.Caption = "Rename";
                dialog.Prompt = "New name for " + entry.Name;
                dialog.Value = entry.Name;
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                name = dialog.Value;
            }
            if (name == entry.Name) return;
            var problem = CheckName(name);
            if (problem != null)
            {
                Dialogs.Warning(FindForm(), "Rename", problem);
                return;
            }
            var target = RemotePath.Combine(RemotePath.Parent(entry.FullPath), name);
            try
            {
                await _connection.RunAsync(s => s.RenameAsync(entry.FullPath, target, CancellationToken.None), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Dialogs.Error(FindForm(), "Rename", ex.Message);
                return;
            }
            InvalidateCache(entry.FullPath);
            await ReloadAsync();
            SelectNames(new[] { name });
        }

        /// <summary>Remembers what links pasted later will point to, until Esc or another pick.</summary>
        private void PickLinkTarget(RemoteEntry entry)
        {
            if (entry == null || entry.Name == "..") return;
            _linkTarget = entry;
            _lastLinkPath = null;
            UpdateRightStatus();
        }

        private void ClearLinkTarget()
        {
            _linkTarget = null;
            _lastLinkPath = null;
            UpdateRightStatus();
        }

        /// <summary>
        /// Creates a link to the picked target in <paramref name="directory"/>: under the target's own
        /// name (or name-link when that is taken) with a relative path, or as set in the dialog.
        /// </summary>
        private async Task PasteLinkAsync(string directory, bool ask)
        {
            var target = _linkTarget;
            if (!Connected || target == null) return;
            directory = RemotePath.Normalize(directory);
            string linkPath, stored;
            if (ask)
            {
                using (var dialog = new SymlinkDialog(_site, target, directory, ListForDialogAsync))
                {
                    if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                    linkPath = dialog.LinkPath;
                    stored = dialog.TargetText;
                }
            }
            else
            {
                if (target.IsDirectory && RemotePath.IsWithin(directory, target.FullPath))
                {
                    Dialogs.Warning(FindForm(), "Paste link", "A link to " + target.Name + " cannot go inside " + target.Name + " itself; it would make a loop.");
                    return;
                }
                try
                {
                    linkPath = await FreeLinkPathAsync(directory, target);
                }
                catch (Exception ex)
                {
                    Dialogs.Error(FindForm(), "Paste link", ex.Message);
                    return;
                }
                stored = RemotePath.Relative(directory, target.FullPath);
            }
            if (await CreateLinkAsync(stored, linkPath))
            {
                _lastLinkPath = linkPath;
                UpdateRightStatus();
            }
        }

        /// <summary>The target's name in <paramref name="directory"/>; name-link, name-link-2… when it is taken.</summary>
        private async Task<string> FreeLinkPathAsync(string directory, RemoteEntry target)
        {
            var entries = await _connection.RunAsync(s => s.ListAsync(directory, CancellationToken.None), CancellationToken.None);
            _cache[directory] = entries.ToList();
            var comparison = _site.Protocol == RemoteProtocol.Local ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var taken = new HashSet<string>(entries.Select(e => e.Name), comparison);
            var name = target.Name;
            for (int n = 1; taken.Contains(name); n++) name = target.Name + "-link" + (n == 1 ? string.Empty : "-" + n);
            return RemotePath.Combine(directory, name);
        }

        /// <summary>Creates the link and shows it; false when it could not be created.</summary>
        private async Task<bool> CreateLinkAsync(string stored, string linkPath)
        {
            SetBusy("Creating link…");
            try
            {
                var existing = await _connection.RunAsync(s => s.StatAsync(linkPath, CancellationToken.None), CancellationToken.None);
                if (existing != null)
                {
                    Dialogs.Warning(FindForm(), "Paste link", linkPath + " already exists. Choose another name or folder.");
                    return false;
                }
                await _connection.RunAsync(s => s.CreateSymlinkAsync(stored, linkPath, CancellationToken.None), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Dialogs.Error(FindForm(), "Paste link", ex.Message);
                return false;
            }
            finally
            {
                SetBusy(null);
            }

            InvalidateCache(linkPath);
            var parent = RemotePath.Parent(linkPath);
            if (parent == _currentPath)
            {
                await ReloadAsync();
                SelectNames(new[] { RemotePath.Name(linkPath) });
            }
            else if (tree.Find(parent)?.Loaded == true)
            {
                // A link pasted from the tree into another folder shows up under it.
                await LoadTreeChildrenAsync(parent);
            }
            return true;
        }

        /// <summary>Directory listings for the symlink dialog's tree, shared with the browser's cache.</summary>
        private async Task<IReadOnlyList<RemoteEntry>> ListForDialogAsync(string path)
        {
            path = RemotePath.Normalize(path);
            if (_cache.TryGetValue(path, out var cached)) return cached;
            var entries = await _connection.RunAsync(s => s.ListAsync(path, CancellationToken.None), CancellationToken.None);
            var list = entries.ToList();
            _cache[path] = list;
            return list;
        }

        private async void deleteButton_Click(object sender, EventArgs e) => await DeleteAsync();

        private async Task DeleteAsync()
        {
            var selection = SelectedEntries;
            if (!Connected || selection.Count == 0) return;
            var what = selection.Count == 1 ? selection[0].Name : Format.Count(selection.Count, "item");
            var message = "Delete " + what + " from " + _site.Name + "?";
            if (selection.Any(s => s.IsDirectory)) message += "\n\nFolders are deleted with everything inside them.";
            if (selection.Any(s => s.Kind == RemoteEntryKind.Symlink)) message += "\n\nLinks are removed on their own; the folders and files they point to stay.";
            message += "\n\nThis cannot be undone.";
            if (!Dialogs.Confirm(FindForm(), "Delete", message, "Delete")) return;

            var failures = new List<string>();
            SetBusy("Deleting…");
            try
            {
                foreach (var entry in selection)
                {
                    try
                    {
                        await DeleteRecursiveAsync(entry);
                    }
                    catch (Exception ex)
                    {
                        failures.Add(entry.Name + ": " + ex.Message);
                    }
                    InvalidateCache(entry.FullPath);
                }
            }
            finally
            {
                SetBusy(null);
            }
            await ReloadAsync();
            if (failures.Count > 0) Dialogs.Error(FindForm(), "Delete", string.Join("\n", failures.Take(12)));
        }

        private async Task DeleteRecursiveAsync(RemoteEntry entry)
        {
            if (entry.Kind == RemoteEntryKind.Directory)
            {
                var children = await _connection.RunAsync(s => s.ListAsync(entry.FullPath, CancellationToken.None), CancellationToken.None);
                foreach (var child in children)
                {
                    if (child.Name == "." || child.Name == "..") continue;
                    await DeleteRecursiveAsync(child);
                }
                await _connection.RunAsync(s => s.DeleteDirectoryAsync(entry.FullPath, CancellationToken.None), CancellationToken.None);
            }
            else
            {
                // A symbolic link is removed itself, never what it points at.
                await _connection.RunAsync(s => s.DeleteFileAsync(entry.FullPath, CancellationToken.None), CancellationToken.None);
            }
        }

        private async void permissionsButton_Click(object sender, EventArgs e) => await EditPermissionsAsync();

        private async Task EditPermissionsAsync()
        {
            var selection = SelectedEntries;
            if (!Connected || selection.Count == 0) return;
            int mode;
            bool recurse;
            using (var dialog = new PermissionsDialog(selection, _site))
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                mode = dialog.Mode;
                recurse = dialog.Recurse;
            }

            var failures = new List<string>();
            SetBusy("Setting permissions…");
            try
            {
                foreach (var entry in selection)
                {
                    try
                    {
                        await ApplyPermissionsAsync(entry, mode, recurse);
                    }
                    catch (Exception ex)
                    {
                        failures.Add(entry.Name + ": " + ex.Message);
                    }
                }
            }
            finally
            {
                SetBusy(null);
            }
            await ReloadAsync();
            SelectNames(selection.Select(s => s.Name));
            if (failures.Count > 0) Dialogs.Error(FindForm(), "Permissions", string.Join("\n", failures.Take(12)));
        }

        private async Task ApplyPermissionsAsync(RemoteEntry entry, int mode, bool recurse)
        {
            await _connection.RunAsync(s => s.SetPermissionsAsync(entry.FullPath, mode, CancellationToken.None), CancellationToken.None);
            if (!recurse || entry.Kind != RemoteEntryKind.Directory) return;
            var children = await _connection.RunAsync(s => s.ListAsync(entry.FullPath, CancellationToken.None), CancellationToken.None);
            foreach (var child in children)
            {
                if (child.Name == "." || child.Name == ".." || child.Kind == RemoteEntryKind.Symlink) continue;
                // Files inside take the same bits without execute, so 755 on a tree leaves files at 644.
                await ApplyPermissionsAsync(child, child.IsDirectory ? mode : mode & ~0x49, true);
            }
            InvalidateCache(entry.FullPath);
        }

        // ------------------------------------------------------------------ bookmarks

        private void bookmarkButton_Click(object sender, EventArgs e)
        {
            ClearMenu(bookmarkMenu);
            bool marked = _site.Bookmarks.Contains(_currentPath);
            AddItem(bookmarkMenu, marked ? "Remove bookmark for this directory" : "Bookmark this directory", Icons.Bookmark, false, () =>
            {
                if (marked) _site.Bookmarks.Remove(_currentPath);
                else _site.Bookmarks.Add(_currentPath);
                SaveSite();
            });
            bool first = true;
            foreach (var path in _site.Bookmarks.OrderBy(p => p, StringComparer.Ordinal))
            {
                var target = path;
                AddItem(bookmarkMenu, path, Icons.Folder, first, () => _ = NavigateAsync(target, true, false));
                first = false;
            }
        }

        private void SaveSite()
        {
            if (_site == null) return;
            // A quick-connect site lives in the tab list, which the main window saves.
            if (_site.IsTransient) SiteModified?.Invoke(this, EventArgs.Empty);
            else SiteStore.Current.AddOrUpdate(_site);
        }

        // ------------------------------------------------------------------ menus

        private static void ClearMenu(ModernContextMenu menu)
        {
            var old = menu.Items.ToList();
            menu.Items.Clear();
            foreach (var item in old) item.Dispose();
        }

        private static ModernContextMenuItem AddItem(ModernContextMenu menu, string text, string svg, bool beginGroup, Action action, bool enabled = true)
        {
            var item = new ModernContextMenuItem { Text = text, SvgIcon = svg, BeginGroup = beginGroup, Enabled = enabled };
            item.Click += (s, e) => action();
            menu.Items.Add(item);
            return item;
        }

        private void list_RowRightClick(object sender, RowMouseEventArgs e) => ShowFileMenu((Control)sender);

        private void tileView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) ShowFileMenu(tileView);
        }

        private void ShowFileMenu(Control source)
        {
            if (!Connected) return;
            BuildFileMenu();
            fileMenu.Show(source, Cursor.Position);
        }

        private void BuildFileMenu()
        {
            ClearMenu(fileMenu);
            var selection = SelectedEntries;
            bool single = selection.Count == 1;
            var first = single ? selection[0] : null;
            bool folder = single && first.IsDirectory;
            var here = folder ? first.FullPath : _currentPath;

            if (folder) AddItem(fileMenu, "Open", Icons.FolderOpen, false, () => _ = NavigateAsync(first.FullPath, true, false));
            if (selection.Count > 0) AddItem(fileMenu, "Download", Icons.Download, false, () => _ = DownloadAsync(selection));
            AddItem(fileMenu, folder ? "Upload files into " + first.Name + "…" : "Upload files here…", Icons.Upload, false, () => PickAndUploadFiles(here));
            AddItem(fileMenu, "Upload a folder…", Icons.UploadTray, false, () => PickAndUploadFolder(here));

            AddItem(fileMenu, "New folder…", Icons.NewFolder, true, () => _ = NewFolderAsync(here));
            if (single) AddItem(fileMenu, "Rename…", Icons.Rename, false, () => _ = RenameAsync());
            if (selection.Count > 0) AddItem(fileMenu, "Delete…", Icons.Delete, false, () => _ = DeleteAsync());
            if (selection.Count > 0) AddItem(fileMenu, "Permissions…", Icons.Permissions, false, () => _ = EditPermissionsAsync());

            AddLinkItems(fileMenu, single ? first : null, here, folder ? first.Name : null);

            AddItem(fileMenu, "Copy path", Icons.Copy, true, () => CopyText(selection.Count > 0 ? string.Join(Environment.NewLine, selection.Select(s => s.FullPath)) : _currentPath));
            if (_site.Protocol != RemoteProtocol.Local)
            {
                AddItem(fileMenu, "Copy URL", Icons.Link, false, () => CopyText(selection.Count > 0
                    ? string.Join(Environment.NewLine, selection.Select(s => _site.Uri + s.FullPath))
                    : _site.Uri + _currentPath));
            }

            if (selection.Count == 0 || folder)
            {
                var pairTarget = here;
                AddItem(fileMenu, _site.PairingAt(pairTarget) == null ? "Pair a local folder…" : "Change pairing…", Icons.Link, true, () => ShowPairingDialog(pairTarget));
            }
            AddItem(fileMenu, "Refresh", Icons.Refresh, selection.Count > 0 && !folder, () => _ = RefreshAsync());
            var hiddenItem = new ModernContextMenuItem
            {
                Text = "Show hidden files",
                BeginGroup = true,
                Checkable = true,
                Checked = AppSettings.Current.ShowHiddenFiles,
                Shortcut = Keys.Control | Keys.H,
            };
            // A checkable row toggles its own check when clicked; the setting follows it.
            hiddenItem.CheckedChanged += (s, e) => ToggleHiddenFiles();
            fileMenu.Items.Add(hiddenItem);
        }

        /// <summary>
        /// Pick as link target for <paramref name="pickable"/>, and once something is picked, Paste
        /// link into <paramref name="destination"/> - straight away, or through the dialog.
        /// </summary>
        private void AddLinkItems(ModernContextMenu menu, RemoteEntry pickable, string destination, string destinationName)
        {
            bool beginGroup = true;
            if (pickable != null && pickable.Name != "..")
            {
                AddItem(menu, "Pick as link target", Icons.Link, true, () => PickLinkTarget(pickable));
                beginGroup = false;
            }
            var target = _linkTarget;
            if (target == null) return;
            var where = destinationName == null ? " here" : " into " + destinationName;
            var paste = AddItem(menu, "Paste link to “" + target.Name + "”" + where, Icons.Symlink, beginGroup, () => _ = PasteLinkAsync(destination, false));
            if (RemotePath.Normalize(destination) == _currentPath) paste.Shortcut = Keys.Control | Keys.Shift | Keys.V;
            AddItem(menu, "Paste link as…", Icons.Symlink, false, () => _ = PasteLinkAsync(destination, true));
        }

        private void tree_RowRightClick(object sender, RowMouseEventArgs e)
        {
            var node = tree.NodeAt(e.Index);
            if (!Connected || node == null) return;
            BuildTreeMenu(node);
            treeMenu.Show(tree, Cursor.Position);
        }

        private void BuildTreeMenu(RemoteTreeNode node)
        {
            ClearMenu(treeMenu);
            var path = node.Path;
            AddItem(treeMenu, "Open", Icons.FolderOpen, false, () => _ = NavigateAsync(path, true, false));
            var entry = node.IsRoot ? null : new RemoteEntry { Name = node.Name, FullPath = path, Kind = RemoteEntryKind.Directory };
            AddLinkItems(treeMenu, entry, path, node.IsRoot ? "/" : node.Name);
            AddItem(treeMenu, "Copy path", Icons.Copy, true, () => CopyText(path));
        }

        private void CopyText(string text)
        {
            try { Clipboard.SetText(text ?? string.Empty); }
            catch (System.Runtime.InteropServices.ExternalException) { }
        }

        // ------------------------------------------------------------------ pairing

        private Pairing CurrentPairing => _site?.PairingFor(_currentPath);

        private string PairingKey(Pairing pairing) => pairing == null ? null : pairing.RemoteRoot + "|" + pairing.LocalRoot + "|" + pairing.Branch + "|" + pairing.BuildOutputDirectory;

        private void UpdatePairingUi()
        {
            if (_site == null) return;
            var pairing = CurrentPairing;
            bool paired = pairing != null;
            pairingChip.Visible = paired;
            pairButton.Visible = !paired;
            if (paired)
            {
                pairingChip.LocalPath = Format.HomeRelative(pairing.LocalRoot);
                pairingChip.Branch = BranchLabel(pairing);
            }
            contentTabs.SetEnabled(CommitsTab, paired);
            if (!paired)
            {
                StopWatchingRepository();
                contentTabs.SetBadge(CommitsTab, null);
                if (contentTabs.SelectedIndex == CommitsTab) contentTabs.SelectedIndex = FilesTab;
            }
            else if (PairingKey(pairing) != _commitsLoadedFor)
            {
                _ = LoadCommitsAsync(false);
            }
            LayoutTabsBar();
            UpdateStatusBar();
        }

        private string BranchLabel(Pairing pairing)
        {
            if (pairing == null) return null;
            if (!string.IsNullOrWhiteSpace(pairing.Branch)) return pairing.Branch;
            return PairingKey(pairing) == _commitsLoadedFor ? _branchName : null;
        }

        private void pairButton_Click(object sender, EventArgs e) => ShowPairingDialog(_currentPath);

        private void pairingChip_Click(object sender, EventArgs e)
        {
            if (contentTabs.SelectedIndex != CommitsTab && CurrentPairing != null) contentTabs.SelectedIndex = CommitsTab;
            else ShowPairingDialog(CurrentPairing?.RemoteRoot ?? _currentPath);
        }

        private void pairingChip_SettingsClick(object sender, EventArgs e) => ShowPairingDialog(CurrentPairing?.RemoteRoot ?? _currentPath);

        private void changePairingButton_Click(object sender, EventArgs e) => ShowPairingDialog(CurrentPairing?.RemoteRoot ?? _currentPath);

        private void statusBar_PairingClick(object sender, EventArgs e) => pairingChip_SettingsClick(sender, e);

        private void ShowPairingDialog(string remotePath)
        {
            var existing = _site.PairingAt(remotePath);
            int itemCount = _cache.TryGetValue(RemotePath.Normalize(remotePath), out var list) ? list.Count : -1;
            using (var dialog = new PairFolderDialog(_site, remotePath, _currentPath, existing, itemCount))
            {
                if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
                if (existing != null) _site.Pairings.Remove(existing);
                var replaced = dialog.Result == null ? null : _site.PairingAt(dialog.Result.RemoteRoot);
                if (replaced != null) _site.Pairings.Remove(replaced);
                if (!dialog.Unpaired && dialog.Result != null) _site.Pairings.Add(dialog.Result);
            }
            SaveSite();
            StopWatchingRepository();
            _commitsLoadedFor = null;
            _shownCommit = null;
            tree.Invalidate();
            UpdatePairingUi();
        }

        // ------------------------------------------------------------------ commits

        private void contentTabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool commits = contentTabs.SelectedIndex == CommitsTab;
            filesPage.Visible = !commits;
            commitsPage.Visible = commits;
            filterBox.PlaceholderText = commits ? "Filter commits, author, sha…" : "Filter this directory…";
            filterBox.Text = string.Empty;
            fileList.Filter = null;
            UpdateCommandBarMode();
            if (commits) _ = LoadCommitsAsync(false);
        }

        /// <summary>
        /// Reads the paired repository's history. A quiet load is one the repository watcher asked for:
        /// it keeps what is on screen until the new history is there, and says nothing when it fails.
        /// </summary>
        private async Task LoadCommitsAsync(bool force, bool quiet = false)
        {
            var pairing = CurrentPairing;
            if (pairing == null) return;
            var key = PairingKey(pairing);
            if (!force && key == _commitsLoadedFor)
            {
                ApplyCommitStates();
                return;
            }

            _commitsCancellation?.Cancel();
            var cancellation = _commitsCancellation = new CancellationTokenSource();
            _planner = new DeployPlanner(pairing);
            if (!quiet)
            {
                commitList.EmptyText = "Reading the history of " + Format.HomeRelative(pairing.LocalRoot) + "…";
                commitList.Invalidate();
            }
            try
            {
                if (!Directory.Exists(pairing.LocalRoot)) throw new DirectoryNotFoundException(pairing.LocalRoot + " no longer exists.");
                var branch = string.IsNullOrWhiteSpace(pairing.Branch) ? await _planner.Repository.GetCurrentBranchAsync() : pairing.Branch;
                var commits = await _planner.GetCommitsAsync(CommitHistoryLimit, cancellation.Token);
                if (cancellation.IsCancellationRequested || CurrentPairing != pairing) return;
                _branchName = branch;
                _commits = commits;
                _commitsLoadedFor = key;
                _revisionSha = commits.Count > 0 ? commits[0].Commit.Sha : null;
                commitList.EmptyText = "No commits on this branch yet.";
                ApplyCommitStates();
                // A quiet load keeps the commit being read; a fresh one opens the remembered commit.
                ApplyCommitFilter(quiet ? null : pairing.SelectedSha);
                pairingChip.Branch = BranchLabel(pairing);
                LayoutTabsBar();
                _ = WatchRepositoryAsync(pairing);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (cancellation.IsCancellationRequested) return;
                if (quiet)
                {
                    // The repository moved out from under us; leave what is shown and stop watching.
                    StopWatchingRepository();
                    return;
                }
                _commits = new List<CommitEntry>();
                _commitsLoadedFor = null;
                commitList.EmptyText = "Could not read the repository: " + ex.Message;
                commitList.SetEntries(null, null);
                ShowCommitAsync(null);
            }
            UpdateStatusBar();
        }

        // ------------------------------------------------------------------ watching the repository

        /// <summary>
        /// Watches the paired repository, so commits made in an editor, a terminal or another git
        /// client reach the Commits tab without anyone pressing Refresh.
        /// </summary>
        private async Task WatchRepositoryAsync(Pairing pairing)
        {
            if (pairing == null || _planner == null) return;
            string gitDirectory;
            try
            {
                gitDirectory = await _planner.Repository.GetGitDirectoryAsync();
            }
            catch (Exception)
            {
                // Not a repository any more; the next load says so.
                StopWatchingRepository();
                return;
            }
            if (IsDisposed || !ReferenceEquals(CurrentPairing, pairing)) return;
            if (_repositoryWatcher != null && string.Equals(_watchedGitDirectory, gitDirectory, StringComparison.OrdinalIgnoreCase)) return;

            StopWatchingRepository();
            var watcher = RepositoryWatcher.Start(gitDirectory, this);
            if (watcher == null) return;
            watcher.Changed += repositoryWatcher_Changed;
            _repositoryWatcher = watcher;
            _watchedGitDirectory = gitDirectory;
        }

        private void StopWatchingRepository()
        {
            _commitWatchTimer.Stop();
            _watchedGitDirectory = null;
            var watcher = _repositoryWatcher;
            _repositoryWatcher = null;
            if (watcher == null) return;
            watcher.Changed -= repositoryWatcher_Changed;
            watcher.Dispose();
        }

        // One git command writes several files; the timer waits for it to finish before reading.
        private void repositoryWatcher_Changed(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            _commitWatchTimer.Stop();
            _commitWatchTimer.Start();
        }

        private void commitWatchTimer_Tick(object sender, EventArgs e)
        {
            _commitWatchTimer.Stop();
            _ = CheckRevisionAsync();
        }

        private void commitPollTimer_Tick(object sender, EventArgs e)
        {
            // Only the tab in front: the others check when they are shown again.
            if (Visible) _ = CheckRevisionAsync();
        }

        /// <summary>
        /// Reloads the history when the followed branch points somewhere new. One cheap rev-parse
        /// otherwise, so the watcher, the poll and navigation can all ask for it.
        /// </summary>
        private async Task CheckRevisionAsync()
        {
            if (_checkingRevision || IsDisposed) return;
            var pairing = CurrentPairing;
            if (pairing == null || _planner == null || PairingKey(pairing) != _commitsLoadedFor) return;
            _checkingRevision = true;
            try
            {
                string sha;
                try
                {
                    sha = await _planner.GetRevisionShaAsync(CancellationToken.None);
                }
                catch (Exception)
                {
                    return;
                }
                // Staging, fetching and checking out touch the same files; only a moved branch is new history.
                if (IsDisposed || sha == null || sha == _revisionSha || !ReferenceEquals(CurrentPairing, pairing)) return;
                await LoadCommitsAsync(true, quiet: true);
            }
            finally
            {
                _checkingRevision = false;
            }
        }

        private void ApplyCommitStates()
        {
            var pairing = CurrentPairing;
            if (pairing == null) return;
            DeployPlanner.ApplyStates(_commits, pairing, _deploying);
            int ahead = DeployPlanner.CommitsAhead(_commits, pairing);
            contentTabs.SetBadge(CommitsTab, _commitsLoadedFor == null ? null : ahead.ToString("N0"));
            commitList.Invalidate();
            LayoutTabsBar();
        }

        private void ApplyCommitFilter(string selectSha)
        {
            var filter = contentTabs.SelectedIndex == CommitsTab ? filterBox.Text.Trim() : string.Empty;
            IEnumerable<CommitEntry> shown = _commits;
            if (filter.Length > 0)
            {
                shown = _commits.Where(c =>
                    (c.Commit.Subject ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (c.Commit.AuthorName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    c.Commit.Sha.StartsWith(filter, StringComparison.OrdinalIgnoreCase));
            }
            var list = shown.ToList();
            var sha = selectSha ?? commitList.SelectedEntry?.Commit.Sha ?? list.FirstOrDefault()?.Commit.Sha;
            if (sha != null && !list.Any(c => c.Commit.Sha == sha)) sha = list.FirstOrDefault()?.Commit.Sha;
            commitList.SetEntries(list, sha);
            var selected = commitList.SelectedEntry;
            if (selected == null || _shownCommit == null || selected.Commit.Sha != _shownCommit.Commit.Sha) ShowCommitAsync(selected);
        }

        private void commitList_SelectionChanged(object sender, EventArgs e)
        {
            var entry = commitList.SelectedEntry;
            if (entry == null || _shownCommit == null || entry.Commit.Sha != _shownCommit.Commit.Sha) ShowCommitAsync(entry);
        }

        private async void ShowCommitAsync(CommitEntry entry)
        {
            _filesCancellation?.Cancel();
            _shownCommit = entry;
            ShowCommitSummary(entry);
            if (entry == null || _planner == null)
            {
                commitFiles.EmptyText = "Select a commit to see the files it changed.";
                commitFiles.SetFiles(null);
                UpdateDeployFooter();
                return;
            }

            var cancellation = _filesCancellation = new CancellationTokenSource();
            var pairing = CurrentPairing;
            commitFiles.EmptyText = "Reading " + entry.Commit.ShortSha + "…";
            commitFiles.SetFiles(null);
            UpdateDeployFooter();
            try
            {
                var files = await _planner.GetFilesAsync(entry.Commit, cancellation.Token);
                if (cancellation.IsCancellationRequested || _shownCommit != entry) return;
                commitFiles.EmptyText = "This commit changed no files.";
                commitFiles.SetFiles(files);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                if (_shownCommit != entry) return;
                commitFiles.EmptyText = ex.Message;
                commitFiles.SetFiles(null);
            }

            if (pairing != null && pairing.SelectedSha != entry.Commit.Sha)
            {
                pairing.SelectedSha = entry.Commit.Sha;
                pairing.DeselectedPaths.Clear();
                SaveSite();
            }
            UpdateDeployFooter();
        }

        private void ShowCommitSummary(CommitEntry entry)
        {
            summaryPanel.Visible = entry != null;
            if (entry == null) return;
            var commit = entry.Commit;
            authorAvatar.PersonName = commit.AuthorName;
            subjectLabel.Text = commit.Subject;
            shaChip.Text = commit.ShortSha;
            commitMetaLabel.Text = commit.AuthorName + "  ·  " + Format.CommitDate(commit.AuthorDate);
            LayoutSummary();
        }

        private void commitFiles_IncludedChanged(object sender, EventArgs e)
        {
            var pairing = CurrentPairing;
            if (pairing != null && _shownCommit != null)
            {
                pairing.SelectedSha = _shownCommit.Commit.Sha;
                pairing.DeselectedPaths = commitFiles.Files.Where(f => f.CanInclude && !f.Included).Select(f => f.RepoPath).ToList();
                SaveSite();
            }
            UpdateDeployFooter();
        }

        private void commitList_RowRightClick(object sender, RowMouseEventArgs e) => ShowCommitMenu(commitList.EntryAt(e.Index));

        private void commitList_DeployChipClick(object sender, RowMouseEventArgs e) => ShowCommitMenu(commitList.EntryAt(e.Index));

        /// <summary>Marks a commit's deploy state by hand, for code that reached the server some other way.</summary>
        private void ShowCommitMenu(CommitEntry entry)
        {
            var pairing = CurrentPairing;
            if (entry == null || pairing == null) return;
            ClearMenu(commitMenu);
            var sha = entry.Commit.Sha;
            bool deploying = entry.State == DeployState.Deploying;
            bool deployed = entry.State == DeployState.Deployed;
            AddItem(commitMenu, "Mark as deployed", Icons.Check, false,
                () => ChangeDeployState(() => DeployPlanner.MarkDeployed(pairing, _commits, sha)), !deploying && !deployed);
            AddItem(commitMenu, "Mark this and older commits as deployed", Icons.Check, false,
                () => ChangeDeployState(() => DeployPlanner.MarkDeployedThrough(pairing, _commits, sha)), !deploying);
            AddItem(commitMenu, "Mark as pending", Icons.Cross, true,
                () => ChangeDeployState(() => DeployPlanner.MarkPending(pairing, _commits, sha)), deployed);
            AddItem(commitMenu, "Copy commit hash", Icons.Copy, true, () => CopyText(sha));
            commitMenu.Show(commitList, Cursor.Position);
        }

        private void ChangeDeployState(Action change)
        {
            change();
            SaveSite();
            ApplyCommitStates();
            UpdateStatusBar();
        }

        private void selectAllButton_Click(object sender, EventArgs e) => commitFiles.SetAllIncluded(true);

        private void clearAllButton_Click(object sender, EventArgs e) => commitFiles.SetAllIncluded(false);

        private void UpdateDeployFooter()
        {
            if (_site == null) return;
            var files = commitFiles.Files;
            var included = files.Where(f => f.Included).ToList();
            detailCountLabel.Text = files.Count == 0 ? string.Empty : included.Count + " of " + files.Count + " selected";
            long bytes = included.Where(f => f.Action != DeployAction.Delete).Sum(f => f.Size ?? 0);
            footerSummaryLabel.Text = included.Count == 0 ? "No files selected" : Format.Count(included.Count, "file") + "  ·  " + Format.Bytes(bytes) + " will upload to";
            footerTargetLabel.Text = CurrentPairing?.RemoteRoot ?? string.Empty;

            bool deploying = _shownCommit != null && _deploying.Contains(_shownCommit.Commit.Sha);
            bool canDeploy = included.Count > 0 && Connected && !deploying;
            uploadFilesButton.Enabled = canDeploy;
            uploadFilesButton.Text = deploying ? "Uploading…" : "Upload these files";
            dryRunButton.Enabled = included.Count > 0;
            deployCommitButton.Enabled = canDeploy;
            deployCommitButton.Text = included.Count > 0 ? "Upload commit — " + Format.Count(included.Count, "file") : "Upload commit";
            compareButton.Enabled = included.Count > 0 && Connected;
            LayoutCommandBar();
            LayoutDetailHeader();
            LayoutDetailFooter();
        }

        private async void uploadFilesButton_Click(object sender, EventArgs e) => await DeployAsync(false);

        private async void dryRunButton_Click(object sender, EventArgs e) => await DeployAsync(true);

        private async Task DeployAsync(bool dryRun)
        {
            var entry = _shownCommit;
            var pairing = CurrentPairing;
            if (entry == null || pairing == null || _planner == null) return;
            var files = commitFiles.Files.Where(f => f.Included && f.RemotePath != null).ToList();
            if (files.Count == 0) return;
            var commit = entry.Commit;
            var sha = commit.Sha;
            var log = _connection.Log;

            if (dryRun)
            {
                log.Status("Dry run of " + commit.ShortSha + " \"" + commit.Subject + "\" — " + Format.Count(files.Count, "file") + " to " + pairing.RemoteRoot);
                foreach (var file in files)
                {
                    switch (file.Action)
                    {
                        case DeployAction.Delete:
                            log.Status("  would delete " + file.RemotePath);
                            break;
                        default:
                            if (file.Kind == FileChangeKind.Renamed && pairing.ApplyDeletions && file.OldRemotePath != null) log.Status("  would delete " + file.OldRemotePath + " (renamed)");
                            log.Status("  would " + (file.Action == DeployAction.New ? "upload new " : "overwrite ") + file.RemotePath + " (" + Format.Bytes(file.Size ?? 0) + ")");
                            break;
                    }
                }
                log.Success("Dry run finished — nothing was transferred");
                LogRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (!Connected) return;
            if (pairing.ConfirmBeforeDeploy)
            {
                int uploads = files.Count(f => f.Action != DeployAction.Delete);
                int deletes = files.Count - uploads;
                var message = "Upload " + Format.Count(uploads, "file") + " from " + commit.ShortSha + " \"" + commit.Subject + "\" to " + pairing.RemoteRoot + " on " + _site.Name;
                if (deletes > 0) message += " and delete " + Format.Count(deletes, "file") + " there";
                message += "?\n\n" + string.Join("\n", files.Take(12).Select(f => (f.Action == DeployAction.Delete ? "−  " : "+  ") + f.RepoPath));
                if (files.Count > 12) message += "\n…and " + (files.Count - 12) + " more";
                if (!Dialogs.Confirm(FindForm(), "Upload commit", message, "Upload")) return;
            }

            _deploying.Add(sha);
            _deployPairings[sha] = pairing;
            ApplyCommitStates();
            UpdateDeployFooter();
            ShowDeploySource(commit.ShortSha);
            UpdateStatusBar();

            // Everything is read out of the repository before anything is queued, so the batch
            // cannot settle while files are still being added to it.
            var staging = DeployPlanner.NewStagingFolder(sha);
            _staging[sha] = staging;
            var uploadsToQueue = new List<KeyValuePair<string, string>>();
            try
            {
                int index = 0;
                foreach (var file in files.Where(f => f.Action != DeployAction.Delete))
                {
                    index++;
                    SetBusy("Preparing " + index + " of " + files.Count(f => f.Action != DeployAction.Delete) + " from " + commit.ShortSha);
                    var local = Path.Combine(staging, file.RepoPath.Replace('/', '\\'));
                    await _planner.ExtractAsync(sha, file.RepoPath, local, CancellationToken.None);
                    uploadsToQueue.Add(new KeyValuePair<string, string>(local, file.RemotePath));
                }
            }
            catch (Exception ex)
            {
                SetBusy(null);
                _deploying.Remove(sha);
                _deployPairings.Remove(sha);
                DeleteStaging(sha);
                ApplyCommitStates();
                UpdateDeployFooter();
                HideDeploySourceIfIdle();
                Dialogs.Error(FindForm(), "Upload commit", "Could not read the files of " + commit.ShortSha + ":\n\n" + ex.Message);
                return;
            }
            SetBusy(null);

            log.Status("Deploying " + commit.ShortSha + " \"" + commit.Subject + "\" to " + pairing.RemoteRoot);
            foreach (var file in files)
            {
                if (file.Action == DeployAction.Delete)
                {
                    _queue.EnqueueDelete(_site, file.RemotePath, sha);
                }
                else if (file.Kind == FileChangeKind.Renamed && pairing.ApplyDeletions && file.OldRemotePath != null && file.OldRemotePath != file.RemotePath)
                {
                    _queue.EnqueueDelete(_site, file.OldRemotePath, sha);
                }
            }
            foreach (var upload in uploadsToQueue) _queue.EnqueueUpload(_site, upload.Key, upload.Value, sha);

            queueTabs.SelectedIndex = 0;
            SetQueueCollapsed(false);
        }

        private void deployCommitButton_Click(object sender, EventArgs e) => uploadFilesButton_Click(sender, e);

        private void Queue_BatchSettled(object sender, BatchSettledEventArgs e)
        {
            if (_site == null || e.Site == null || e.Site.Id != _site.Id) return;
            var sha = e.Tag;
            if (!_deploying.Remove(sha)) return;
            _deployPairings.TryGetValue(sha, out var pairing);
            _deployPairings.Remove(sha);
            var log = _connection.Log;

            if (pairing != null && e.Succeeded)
            {
                if (!pairing.DeployedShas.Contains(sha)) pairing.DeployedShas.Add(sha);
                if (pairing.DeployedShas.Count > 1000) pairing.DeployedShas.RemoveRange(0, pairing.DeployedShas.Count - 1000);
                int newIndex = _commits.FindIndex(c => c.Commit.Sha == sha);
                int lastIndex = string.IsNullOrEmpty(pairing.LastDeployedSha) ? -1 : _commits.FindIndex(c => c.Commit.Sha == pairing.LastDeployedSha);
                if (lastIndex < 0 || (newIndex >= 0 && newIndex < lastIndex)) pairing.LastDeployedSha = sha;
                SaveSite();
                log.Success("Deployed " + sha.Substring(0, 7) + " — " + Format.Count(e.Total, "file"));
            }
            else
            {
                log.Warning("Deploy of " + sha.Substring(0, 7) + " finished with " + e.Completed + " of " + e.Total + " files; the commit stays pending");
            }

            DeleteStaging(sha);
            if (pairing != null)
            {
                InvalidateCache(pairing.RemoteRoot);
                if (RemotePath.IsWithin(_currentPath, pairing.RemoteRoot)) _ = ReloadAsync();
            }
            ApplyCommitStates();
            UpdateDeployFooter();
            HideDeploySourceIfIdle();
            UpdateStatusBar();
        }

        private void DeleteStaging(string sha)
        {
            if (!_staging.TryGetValue(sha, out var folder)) return;
            _staging.Remove(sha);
            try
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void ShowDeploySource(string shortSha)
        {
            deploySourceLabel.Text = "from commit " + shortSha;
            deploySourceLabel.Visible = true;
            LayoutQueueHeader();
        }

        private void HideDeploySourceIfIdle()
        {
            if (_deploying.Count > 0)
            {
                deploySourceLabel.Text = "from commit " + _deploying.First().Substring(0, 7);
                return;
            }
            deploySourceLabel.Visible = false;
            LayoutQueueHeader();
        }

        private async void compareButton_Click(object sender, EventArgs e)
        {
            var files = commitFiles.Files.Where(f => f.Included && f.RemotePath != null).ToList();
            if (!Connected || files.Count == 0 || _shownCommit == null) return;
            var log = _connection.Log;
            int same = 0, differ = 0, missing = 0, leftover = 0;
            SetBusy("Comparing with " + _site.Name + "…");
            try
            {
                log.Status("Comparing " + _shownCommit.Commit.ShortSha + " with " + _site.Name);
                foreach (var file in files)
                {
                    RemoteEntry remote;
                    try
                    {
                        remote = await _connection.RunAsync(s => s.StatAsync(file.RemotePath, CancellationToken.None), CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        log.Error(file.RemotePath + ": " + ex.Message);
                        continue;
                    }
                    if (file.Action == DeployAction.Delete)
                    {
                        if (remote != null) { leftover++; log.Warning("  still on the server: " + file.RemotePath); }
                        else same++;
                    }
                    else if (remote == null)
                    {
                        missing++;
                        log.Warning("  missing on the server: " + file.RemotePath);
                    }
                    else if (file.Size.HasValue && remote.Size != file.Size.Value)
                    {
                        differ++;
                        log.Warning("  differs: " + file.RemotePath + " is " + Format.Bytes(remote.Size) + " on the server, " + Format.Bytes(file.Size.Value) + " in the commit");
                    }
                    else
                    {
                        same++;
                    }
                }
            }
            finally
            {
                SetBusy(null);
            }

            var summary = Format.Count(same, "file") + " match the commit.";
            if (differ > 0) summary += "\n" + Format.Count(differ, "file") + " differ in size.";
            if (missing > 0) summary += "\n" + Format.Count(missing, "file") + " are missing on the server.";
            if (leftover > 0) summary += "\n" + Format.Count(leftover, "deleted file") + " are still on the server.";
            if (differ + missing + leftover > 0) summary += "\n\nThe details are in the protocol log.";
            log.Success("Comparison finished");
            Dialogs.Information(FindForm(), "Compare with remote", summary);
        }

        private async void fetchButton_Click(object sender, EventArgs e)
        {
            if (_planner == null) return;
            fetchButton.Enabled = false;
            SetBusy("Fetching " + Format.HomeRelative(_planner.Pairing.LocalRoot) + "…");
            try
            {
                var result = await _planner.Repository.FetchAsync(null, CancellationToken.None);
                if (!result.Succeeded) Dialogs.Error(FindForm(), "Fetch repo", result.Message);
            }
            catch (Exception ex)
            {
                Dialogs.Error(FindForm(), "Fetch repo", ex.Message);
            }
            finally
            {
                SetBusy(null);
                fetchButton.Enabled = true;
            }
            await LoadCommitsAsync(true);
        }

        // ------------------------------------------------------------------ transfer queue

        private void Queue_ItemEvent(object sender, TransferItem item)
        {
            _queueDirty = true;
            if (_site != null && item.Site != null && item.Site.Id == _site.Id && item.Status == TransferStatus.Completed && item.Direction != TransferDirection.Download)
            {
                var parent = RemotePath.Parent(item.RemotePath);
                _cache.Remove(parent);
                if (parent == _currentPath && item.BatchTag == null) _refreshAfterTransfers = true;
            }
            if (!queueTimer.Enabled) queueTimer.Start();
        }

        private void Queue_ItemsRemoved(object sender, EventArgs e)
        {
            _queueDirty = true;
            if (!queueTimer.Enabled) queueTimer.Start();
        }

        private void queueTimer_Tick(object sender, EventArgs e)
        {
            queueTimer.Stop();
            if (_queueDirty)
            {
                _queueDirty = false;
                RefreshQueueView();
            }
            if (_refreshAfterTransfers && Connected && !_connection.IsBusy)
            {
                // Waits for the queue to go quiet for this directory before listing it again.
                bool stillUploadingHere = _queue.Snapshot().Any(i => !i.IsFinished && i.Site.Id == _site.Id && RemotePath.Parent(i.RemotePath) == _currentPath);
                if (!stillUploadingHere)
                {
                    _refreshAfterTransfers = false;
                    _ = ReloadAsync();
                }
            }
        }

        private void RefreshQueueView()
        {
            if (_queue == null) return;
            var items = _queue.Snapshot();
            int active = items.Count(i => i.Status == TransferStatus.Active);
            int queued = items.Count(i => i.Status == TransferStatus.Queued || i.Status == TransferStatus.Paused);
            int completed = items.Count(i => i.Status == TransferStatus.Completed || i.Status == TransferStatus.Skipped || i.Status == TransferStatus.Cancelled);
            int failed = items.Count(i => i.Status == TransferStatus.Failed);
            queueTabs.SetBadge(0, active.ToString("N0"));
            queueTabs.SetBadge(1, queued.ToString("N0"));
            queueTabs.SetBadge(2, completed.ToString("N0"));
            queueTabs.SetBadge(3, failed.ToString("N0"));

            IEnumerable<TransferItem> shown;
            switch (queueTabs.SelectedIndex)
            {
                case 1: shown = items.Where(i => i.Status == TransferStatus.Queued || i.Status == TransferStatus.Paused); break;
                case 2: shown = items.Where(i => i.Status == TransferStatus.Completed || i.Status == TransferStatus.Skipped || i.Status == TransferStatus.Cancelled); break;
                case 3: shown = items.Where(i => i.Status == TransferStatus.Failed); break;
                default: shown = items; break;
            }
            transferList.SetItems(shown, AppSettings.Current.QueueGrouping, _queue.QueuePosition);
            queueSummary.SetStats(_queue.Stats());

            bool paused = _queue.IsPaused;
            pauseButton.Text = paused ? "Resume all" : "Pause all";
            pauseButton.IconSvg = paused ? Icons.Resume : Icons.Pause;
            clearCompletedButton.Enabled = completed > 0;
            UpdateTransferringRows(items);
            LayoutQueueHeader();
        }

        private void UpdateTransferringRows(List<TransferItem> items = null)
        {
            if (_queue == null || _site == null) return;
            items = items ?? _queue.Snapshot();
            fileList.SetTransferring(items
                .Where(i => i.Status == TransferStatus.Active && i.Site != null && i.Site.Id == _site.Id && i.Direction != TransferDirection.Delete && RemotePath.Parent(i.RemotePath) == _currentPath)
                .Select(i => i.RemotePath));
        }

        private void queueTabs_SelectedIndexChanged(object sender, EventArgs e) => RefreshQueueView();

        private void pauseButton_Click(object sender, EventArgs e)
        {
            if (_queue.IsPaused) _queue.ResumeAll();
            else _queue.PauseAll();
            RefreshQueueView();
        }

        private void clearCompletedButton_Click(object sender, EventArgs e) => _queue.ClearCompleted();

        private void transferList_RetryRequested(object sender, TransferItemEventArgs e) => _queue.Retry(e.Item);

        private void transferList_RowRightClick(object sender, RowMouseEventArgs e)
        {
            var selection = transferList.SelectedItems;
            if (selection.Count == 0) return;
            ClearMenu(transferMenu);
            var failed = selection.Where(i => i.Status == TransferStatus.Failed || i.Status == TransferStatus.Cancelled).ToList();
            var running = selection.Where(i => !i.IsFinished).ToList();
            var downloaded = selection.FirstOrDefault(i => i.Direction == TransferDirection.Download && i.Status == TransferStatus.Completed);
            AddItem(transferMenu, failed.Count > 1 ? "Retry " + failed.Count + " transfers" : "Retry", Icons.Refresh, false, () => { foreach (var i in failed) _queue.Retry(i); }, failed.Count > 0);
            AddItem(transferMenu, running.Count > 1 ? "Cancel " + running.Count + " transfers" : "Cancel", Icons.Cross, false, () => { foreach (var i in running) _queue.Cancel(i); }, running.Count > 0);
            if (downloaded != null)
            {
                AddItem(transferMenu, "Show in folder", Icons.FolderOpen, true, () =>
                {
                    try { System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + downloaded.LocalPath + "\""); } catch { }
                });
            }
            var first = selection[0];
            AddItem(transferMenu, "Copy path", Icons.Copy, downloaded == null, () => CopyText(first.Direction == TransferDirection.Download ? first.LocalPath : first.RemotePath));
            transferMenu.Show(transferList, Cursor.Position);
        }

        private void collapseQueueButton_Click(object sender, EventArgs e) => SetQueueCollapsed(!_queueCollapsed);

        private void queueHeader_DoubleClick(object sender, EventArgs e) => SetQueueCollapsed(!_queueCollapsed);

        private void SetQueueCollapsed(bool collapsed)
        {
            if (_queueCollapsed == collapsed) return;
            if (collapsed && bodySplit.Panel2.Height > CollapsedQueueHeight + 20) _queueHeight = bodySplit.Panel2.Height;
            _queueCollapsed = collapsed;
            ApplyQueueHeight();
            AppSettings.Current.QueueCollapsed = collapsed;
        }

        private void ApplyQueueHeight()
        {
            transferList.Visible = !_queueCollapsed;
            queueSummary.Visible = !_queueCollapsed;
            collapseQueueButton.IconSvg = _queueCollapsed ? Icons.ChevronUp : Icons.Collapse;
            collapseQueueButton.IconSize = _queueCollapsed ? 12 : 14;
            bodySplit.IsSplitterFixed = _queueCollapsed;
            int height = _queueCollapsed ? CollapsedQueueHeight : _queueHeight;
            SetSplitterDistance(bodySplit.Height - bodySplit.SplitterWidth - height);
        }

        private void SetSplitterDistance(int distance)
        {
            int max = bodySplit.Height - bodySplit.SplitterWidth - bodySplit.Panel2MinSize;
            if (max < bodySplit.Panel1MinSize) return;
            bool restoring = _restoringLayout;
            _restoringLayout = true;
            try
            {
                bodySplit.SplitterDistance = Math.Max(bodySplit.Panel1MinSize, Math.Min(max, distance));
            }
            finally
            {
                _restoringLayout = restoring;
            }
        }

        private void bodySplit_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (_restoringLayout || _queueCollapsed) return;
            _queueHeight = bodySplit.Panel2.Height;
            AppSettings.Current.QueueHeight = _queueHeight;
        }

        private void paneGrip_PaneResized(object sender, EventArgs e)
        {
            AppSettings.Current.CommitDetailWidth = commitDetail.Width;
        }

        private void workSplit_SplitterMoved(object sender, SplitterEventArgs e)
        {
            if (_restoringLayout) return;
            AppSettings.Current.TreePaneWidth = workSplit.SplitterDistance;
        }

        private void SetWorkSplitDistance(int distance)
        {
            int max = workSplit.Width - workSplit.SplitterWidth - workSplit.Panel2MinSize;
            if (max < workSplit.Panel1MinSize) return;
            bool restoring = _restoringLayout;
            _restoringLayout = true;
            try
            {
                workSplit.SplitterDistance = Math.Max(workSplit.Panel1MinSize, Math.Min(max, distance));
            }
            finally
            {
                _restoringLayout = restoring;
            }
        }

        // ------------------------------------------------------------------ status bar

        private void UpdateStatusBar()
        {
            if (_site == null || _connection == null) return;
            var state = _connection.State;
            statusBar.State = MapState(state);
            var protocol = _site.Protocol == RemoteProtocol.Local ? "local folder" : _site.ProtocolName;
            switch (state)
            {
                case ConnectionState.Connected: statusBar.StatusText = "Connected — " + protocol; break;
                case ConnectionState.Connecting: statusBar.StatusText = "Connecting to " + (_site.Protocol == RemoteProtocol.Local ? _site.Name : _site.Host) + "…"; break;
                case ConnectionState.Failed: statusBar.StatusText = "Could not connect — " + _connection.LastError; break;
                default: statusBar.StatusText = "Disconnected"; break;
            }
            statusBar.Endpoint = _site.Endpoint;
            statusBar.Security = state == ConnectionState.Connected ? _connection.SecurityDescription : null;

            var pairing = CurrentPairing;
            if (pairing == null)
            {
                statusBar.PairingText = null;
            }
            else
            {
                var text = Format.HomeRelative(pairing.LocalRoot) + " → " + pairing.RemoteRoot;
                var branch = BranchLabel(pairing);
                if (!string.IsNullOrEmpty(branch)) text += " · " + branch;
                if (PairingKey(pairing) == _commitsLoadedFor)
                {
                    int ahead = DeployPlanner.CommitsAhead(_commits, pairing);
                    text += " · " + (ahead == 0 ? "up to date with the last deploy" : Format.Count(ahead, "commit") + " ahead of last deploy");
                }
                statusBar.PairingText = text;
            }
            UpdateRightStatus();
        }

        private void UpdateRightStatus()
        {
            if (_connection == null) return;
            if (!string.IsNullOrEmpty(_busyText))
            {
                statusBar.RightText = _busyText;
                return;
            }
            if (_deploying.Count > 0)
            {
                statusBar.RightText = "Deploying " + string.Join(", ", _deploying.Select(s => s.Substring(0, 7)));
                return;
            }
            if (_linkTarget != null)
            {
                statusBar.RightText = (_lastLinkPath != null ? "Created " + _lastLinkPath + "  ·  " : string.Empty)
                    + "Linking to " + _linkTarget.Name + ": right-click a folder, Paste link  ·  Esc to stop";
                return;
            }
            if (_connection.State != ConnectionState.Connected)
            {
                statusBar.RightText = null;
                return;
            }
            var idle = DateTime.Now - _connection.LastActivity;
            string idleText;
            if (_connection.IsBusy) idleText = "Working";
            else if (idle.TotalSeconds < 60) idleText = "Idle " + Math.Max(0, (int)idle.TotalSeconds) + "s";
            else if (idle.TotalMinutes < 60) idleText = "Idle " + (int)idle.TotalMinutes + "m";
            else idleText = "Idle " + (int)idle.TotalHours + "h";
            var keepalive = AppSettings.Current.KeepAliveSeconds > 0 && _site.Protocol != RemoteProtocol.Local ? "keepalive on" : "keepalive off";
            statusBar.RightText = idleText + " · " + keepalive;
        }

        private void statusTimer_Tick(object sender, EventArgs e)
        {
            if (_connection == null) return;
            UpdateRightStatus();
            int interval = AppSettings.Current.KeepAliveSeconds;
            if (interval <= 0 || _connection.State != ConnectionState.Connected || _connection.IsBusy) return;
            if ((DateTime.Now - _connection.LastActivity).TotalSeconds < interval) return;
            if ((DateTime.Now - _lastKeepAlive).TotalSeconds < interval) return;
            _lastKeepAlive = DateTime.Now;
            _ = _connection.KeepAliveAsync();
        }

        private void logButton_Click(object sender, EventArgs e) => LogRequested?.Invoke(this, EventArgs.Empty);

        private void settingsButton_Click(object sender, EventArgs e) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        // ------------------------------------------------------------------ layout

        private void UpdateCommandBarMode()
        {
            bool commits = contentTabs.SelectedIndex == CommitsTab;
            foreach (var control in new Control[] { uploadButton, downloadButton, commandSeparator1, newFolderButton, renameButton, deleteButton, permissionsButton, viewToggle })
            {
                control.Visible = !commits;
            }
            foreach (var control in new Control[] { deployCommitButton, compareButton, fetchButton, changePairingButton })
            {
                control.Visible = commits;
            }
            UpdateDeployFooter();
            LayoutCommandBar();
        }

        private void commandBar_Resize(object sender, EventArgs e) => LayoutCommandBar();

        private void LayoutCommandBar()
        {
            if (commandBar == null || commandBar.Width <= 0) return;
            const int top = 10;
            int x = 12;
            // The mode decides what is laid out; Visible reads false whenever this tab is hidden.
            bool commits = contentTabs.SelectedIndex == CommitsTab;
            var filesOnly = new Control[] { uploadButton, downloadButton, commandSeparator1, newFolderButton, renameButton, deleteButton, permissionsButton };
            var commitsOnly = new Control[] { deployCommitButton, compareButton, fetchButton, changePairingButton };
            var order = new Control[]
            {
                uploadButton, downloadButton, deployCommitButton, commandSeparator1,
                newFolderButton, renameButton, deleteButton, permissionsButton,
                compareButton, fetchButton, changePairingButton, commandSeparator2, refreshButton,
            };
            foreach (var control in order)
            {
                if (commits ? filesOnly.Contains(control) : commitsOnly.Contains(control)) continue;
                int width = control is CommandButton button ? button.PreferredWidth : control.Width;
                control.SetBounds(x, top, width, 32);
                x += width + 4;
            }

            int right = commandBar.Width - 12;
            settingsButton.SetBounds(right - 32, top, 32, 32);
            right -= 36;
            logButton.SetBounds(right - 32, top, 32, 32);
            right -= 44;
            if (!commits)
            {
                viewToggle.SetBounds(right - 64, top, 64, 32);
                right -= 72;
            }
            int filterWidth = Math.Max(120, Math.Min(240, right - x - 8));
            filterBox.SetBounds(right - filterWidth, top, filterWidth, 32);
        }

        private void pathBar_Resize(object sender, EventArgs e)
        {
            int width = pathBar.Width;
            backButton.SetBounds(10, 9, 30, 30);
            forwardButton.SetBounds(44, 9, 30, 30);
            upButton.SetBounds(78, 9, 30, 30);
            int bookmarkWidth = bookmarkButton.PreferredWidth;
            bookmarkButton.SetBounds(width - 12 - bookmarkWidth, 8, bookmarkWidth, 32);
            breadcrumb.SetBounds(118, 8, Math.Max(80, bookmarkButton.Left - 8 - 118), 32);
        }

        private void tabsBar_Resize(object sender, EventArgs e) => LayoutTabsBar();

        private void LayoutTabsBar()
        {
            if (tabsBar == null) return;
            contentTabs.SetBounds(12, 0, Math.Max(100, contentTabs.PreferredWidth), 42);
            int right = tabsBar.Width - 12;
            if (CurrentPairing != null)
            {
                int width = Math.Min(pairingChip.PreferredWidth, Math.Max(160, right - contentTabs.Right - 16));
                pairingChip.SetBounds(right - width, 7, width, 28);
            }
            int pairWidth = pairButton.PreferredWidth;
            pairButton.SetBounds(right - pairWidth, 6, pairWidth, 30);
        }

        private void queueHeader_Resize(object sender, EventArgs e) => LayoutQueueHeader();

        private void LayoutQueueHeader()
        {
            if (queueHeader == null) return;
            queueTitle.SetBounds(14, 0, queueTitle.PreferredWidth + 2, 44);
            queueTabs.SetBounds(queueTitle.Right + 12, 8, Math.Max(100, queueTabs.PreferredWidth), 28);
            if (_deploying.Count > 0) deploySourceLabel.SetBounds(queueTabs.Right + 14, 0, deploySourceLabel.PreferredWidth + 4, 44);

            int right = queueHeader.Width - 12;
            collapseQueueButton.SetBounds(right - 28, 8, 28, 28);
            right -= 32;
            int clearWidth = clearCompletedButton.PreferredWidth;
            clearCompletedButton.SetBounds(right - clearWidth, 8, clearWidth, 28);
            right -= clearWidth + 4;
            int pauseWidth = pauseButton.PreferredWidth;
            pauseButton.SetBounds(right - pauseWidth, 8, pauseWidth, 28);
        }

        private void detailHeader_Resize(object sender, EventArgs e) => LayoutDetailHeader();

        private void LayoutDetailHeader()
        {
            if (detailHeader == null) return;
            detailTitle.SetBounds(14, 0, detailTitle.PreferredWidth + 2, 30);
            detailCountLabel.SetBounds(detailTitle.Right + 8, 0, detailCountLabel.PreferredWidth + 4, 30);
            int right = detailHeader.Width - 8;
            int clearWidth = clearAllButton.PreferredWidth;
            clearAllButton.SetBounds(right - clearWidth, 3, clearWidth, 24);
            right -= clearWidth + 2;
            int selectWidth = selectAllButton.PreferredWidth;
            selectAllButton.SetBounds(right - selectWidth, 3, selectWidth, 24);
        }

        private void summaryPanel_Resize(object sender, EventArgs e) => LayoutSummary();

        private void LayoutSummary()
        {
            if (summaryPanel == null) return;
            authorAvatar.SetBounds(14, 21, 32, 32);
            subjectLabel.SetBounds(58, 15, Math.Max(40, summaryPanel.Width - 72), 21);
            shaChip.Location = new Point(58, 40);
            commitMetaLabel.SetBounds(shaChip.Right + 8, 38, Math.Max(40, summaryPanel.Width - shaChip.Right - 22), 22);
        }

        private void detailFooter_Resize(object sender, EventArgs e) => LayoutDetailFooter();

        private void LayoutDetailFooter()
        {
            if (detailFooter == null) return;
            int right = detailFooter.Width - 14;
            int dryWidth = dryRunButton.PreferredWidth;
            dryRunButton.SetBounds(right - dryWidth, 12, dryWidth, 32);
            right -= dryWidth + 8;
            int uploadWidth = uploadFilesButton.PreferredWidth;
            uploadFilesButton.SetBounds(right - uploadWidth, 12, uploadWidth, 32);
            int textWidth = Math.Max(40, uploadFilesButton.Left - 14 - 12);
            footerSummaryLabel.SetBounds(14, 10, textWidth, 19);
            footerTargetLabel.SetBounds(14, 29, textWidth, 17);
        }

        // ------------------------------------------------------------------ keyboard

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Control | Keys.F:
                    filterBox.Focus();
                    return true;
                case Keys.Control | Keys.L:
                    breadcrumb.BeginEdit();
                    return true;
                case Keys.Alt | Keys.Left:
                    backButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Alt | Keys.Right:
                    forwardButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Alt | Keys.Up:
                    upButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.U:
                    PickAndUploadFiles(_currentPath);
                    return true;
                case Keys.Control | Keys.H:
                    ToggleHiddenFiles();
                    return true;
                case Keys.F7:
                case Keys.Control | Keys.Shift | Keys.N:
                    newFolderButton_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Control | Keys.Shift | Keys.V:
                    if (_linkTarget == null || filterBox.ContainsFocus) break;
                    _ = PasteLinkAsync(_currentPath, false);
                    return true;
                case Keys.Escape:
                    if (filterBox.ContainsFocus && filterBox.Text.Length > 0)
                    {
                        filterBox.Text = string.Empty;
                        return true;
                    }
                    if (_linkTarget != null)
                    {
                        ClearLinkTarget();
                        return true;
                    }
                    break;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Theme.Changed -= Theme_Changed;
                _splitSkin?.Dispose();
                StopWatchingRepository();
                _commitWatchTimer.Dispose();
                _commitPollTimer.Stop();
                _commitPollTimer.Dispose();
                statusTimer.Stop();
                queueTimer.Stop();
                _listCancellation?.Cancel();
                _commitsCancellation?.Cancel();
                _filesCancellation?.Cancel();
                if (_connection != null) _connection.StateChanged -= Connection_StateChanged;
                if (_queue != null)
                {
                    _queue.ItemAdded -= Queue_ItemEvent;
                    _queue.ItemChanged -= Queue_ItemEvent;
                    _queue.ItemsRemoved -= Queue_ItemsRemoved;
                    _queue.BatchSettled -= Queue_BatchSettled;
                }
                if (components != null) components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
