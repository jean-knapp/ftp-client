using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// Create symlink: what the link points to beside where it will be, a tree of the server's
    /// directories to put it in, its name, and whether the target is stored as a relative path.
    /// </summary>
    public partial class SymlinkDialog : ModernForm
    {
        private readonly RemoteEntry _target;
        private readonly Func<string, Task<IReadOnlyList<RemoteEntry>>> _list;
        private readonly HashSet<string> _loading = new HashSet<string>(StringComparer.Ordinal);
        private string _directory;
        private string _pendingName;

        public SymlinkDialog(Site site, RemoteEntry target, string startDirectory, Func<string, Task<IReadOnlyList<RemoteEntry>>> list)
        {
            InitializeComponent();
            Theme.Apply(skin);
            nameBox.OverrideSkinFont = true;
            nameBox.Font = Fonts.Code(13f);

            if (site == null) throw new ArgumentNullException(nameof(site));
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _list = list ?? throw new ArgumentNullException(nameof(list));
            _directory = RemotePath.Normalize(startDirectory);
            // A link beside what it points to needs a name of its own.
            _pendingName = _directory == RemotePath.Parent(target.FullPath) ? target.Name + "-link" : target.Name;

            tree.ShowHidden = AppSettings.Current.ShowHiddenFiles;
            targetPathLabel.Text = target.FullPath;
            if (!target.IsDirectory)
            {
                targetPathLabel.IconSvg = Icons.File;
                targetPathLabel.IconRole = TextRole.Secondary;
            }
            targetDetailLabel.Text = (target.IsDirectory ? "folder" : target.Kind == RemoteEntryKind.Symlink ? "link" : "file") + " · " + site.Name;
            relativeSwitch.SetCheckedQuiet(true);
            UpdatePreview();
        }

        /// <summary>Where the link is created.</summary>
        public string LinkPath { get; private set; }

        /// <summary>What the link stores: a path relative to its directory, or the absolute one.</summary>
        public string TargetText { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ButtonRow.ArrangeRight(content.ClientSize.Width - 24, 8, 80, createButton, cancelButton);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // ModernTextBox keeps text only once its edit box exists.
            nameBox.Text = _pendingName;
            _pendingName = null;
            nameBox.Focus();
            tree.SelectPath(_directory);
            await LoadAsync(_directory);
            tree.Expand(_directory);
        }

        private string NameText => _pendingName ?? (nameBox.Text ?? string.Empty).Trim();

        private async Task LoadAsync(string path)
        {
            path = RemotePath.Normalize(path);
            if (!_loading.Add(path)) return;
            try
            {
                var entries = await _list(path);
                if (IsDisposed) return;
                tree.SetChildren(path, entries);
                // Siblings listed above the chosen directory push it down; keep it in sight.
                if (RemotePath.IsWithin(_directory, path)) tree.RevealSelected();
            }
            catch (Exception)
            {
                // A directory that cannot be listed keeps its chevron.
            }
            finally
            {
                _loading.Remove(path);
            }
        }

        private async void tree_ExpandRequested(object sender, TreeNodeEventArgs e) => await LoadAsync(e.Node.Path);

        private async void tree_NodeSelected(object sender, TreeNodeEventArgs e)
        {
            _directory = e.Node.Path;
            UpdatePreview();
            if (e.Node.Loaded) return;
            await LoadAsync(e.Node.Path);
            if (_directory == e.Node.Path) tree.Expand(e.Node.Path);
        }

        private void nameBox_TextChanged(object sender, EventArgs e) => UpdatePreview();

        private void relativeSwitch_CheckedChanged(object sender, EventArgs e) => UpdatePreview();

        private string StoredTarget => relativeSwitch.Checked ? RemotePath.Relative(_directory, _target.FullPath) : _target.FullPath;

        private void UpdatePreview()
        {
            var name = NameText;
            linkPathLabel.Text = RemotePath.Combine(_directory, name.Length == 0 ? "…" : name);
            linkDetailLabel.Text = "→ " + StoredTarget;
            relativeHint.Text = relativeSwitch.Checked
                ? "Keeps working when the site moves or the account is chrooted"
                : "Stores the full path " + _target.FullPath;
            createButton.Enabled = Problem() == null;
        }

        /// <summary>Why the link cannot be created where and as it is set up; null when it can.</summary>
        private string Problem()
        {
            var name = NameText;
            if (name.Length == 0) return "Enter a name for the link.";
            if (name == "." || name == "..") return "That name is reserved.";
            if (name.Contains("/")) return "A name cannot contain a slash.";
            var link = RemotePath.Combine(_directory, name);
            if (link == _target.FullPath) return "The link cannot take the place of what it points to.";
            if (_target.IsDirectory && RemotePath.IsWithin(link, _target.FullPath)) return "A link inside the folder it points to would make a loop.";
            return null;
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            var problem = Problem();
            if (problem != null)
            {
                Dialogs.Warning(this, "Create symlink", problem);
                return;
            }
            LinkPath = RemotePath.Combine(_directory, NameText);
            TargetText = StoredTarget;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                cancelButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.Enter && nameBox.ContainsFocus)
            {
                createButton_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
