using System;
using System.IO;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Deploy;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// Pair local folder: the local repository and the remote directory side by side, the
    /// deploy options, and Pair. Editing an existing pairing keeps its deploy history.
    /// </summary>
    public partial class PairFolderDialog : ModernForm
    {
        private readonly Site _site;
        private readonly string _currentPath;
        private readonly Pairing _existing;
        private readonly int _itemCount;
        private string _remoteRoot;
        private string _localRoot;
        private string _pendingBuildDirectory;
        private RepositorySummary _summary;
        private int _describeVersion;

        public PairFolderDialog(Site site, string remotePath, string currentPath, Pairing existing, int itemCount)
        {
            InitializeComponent();
            Theme.Apply(skin);
            buildBox.OverrideSkinFont = true;
            buildBox.Font = Fonts.Code(13f);

            _site = site ?? throw new ArgumentNullException(nameof(site));
            _currentPath = RemotePath.Normalize(currentPath);
            _existing = existing;
            _itemCount = itemCount;
            _remoteRoot = RemotePath.Normalize(existing?.RemoteRoot ?? remotePath);

            honourSwitch.SetCheckedQuiet(existing?.HonourIgnoreFiles ?? true);
            deletionsSwitch.SetCheckedQuiet(existing?.ApplyDeletions ?? true);
            confirmSwitch.SetCheckedQuiet(existing?.ConfirmBeforeDeploy ?? true);
            _pendingBuildDirectory = existing?.BuildOutputDirectory ?? string.Empty;

            if (existing != null)
            {
                Text = "Change pairing";
                headingLabel.Text = "Change how this directory is paired";
                pairButton.Text = "Save";
                unpairButton.Visible = true;
            }
            ShowRemote();
            SetLocal(existing?.LocalRoot);
        }

        /// <summary>The pairing to store; null when <see cref="Unpaired"/>.</summary>
        public Pairing Result { get; private set; }

        public bool Unpaired { get; private set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ButtonRow.ArrangeRight(content.ClientSize.Width - 24, 8, 80, pairButton, cancelButton);
            unpairButton.Width = unpairButton.PreferredWidth;
            LayoutButtons();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // ModernTextBox keeps text only once its edit box exists.
            buildBox.Text = _pendingBuildDirectory ?? string.Empty;
            if (_localRoot == null) BeginInvoke((Action)ChooseFolder);
        }

        private void ShowRemote()
        {
            remotePathLabel.Text = _remoteRoot;
            var detail = _site.Name + " · " + (_site.Protocol == RemoteProtocol.Local ? "local folder" : _site.ProtocolName);
            if (_itemCount >= 0 && _remoteRoot == RemotePath.Normalize(_currentPath)) detail += " · " + Format.Count(_itemCount, "item");
            remoteDetailLabel.Text = detail;
            useCurrentButton.Enabled = _remoteRoot != _currentPath;
            useCurrentButton.Text = _remoteRoot != _currentPath ? "Use current remote directory (" + _currentPath + ")" : "Use current remote directory";
            LayoutButtons();
        }

        private async void SetLocal(string path)
        {
            _localRoot = string.IsNullOrWhiteSpace(path) ? null : path;
            int version = ++_describeVersion;
            _summary = null;
            pairButton.Enabled = false;

            if (_localRoot == null)
            {
                localPathLabel.Text = "No folder chosen";
                localDetailLabel.Role = TextRole.Tertiary;
                localDetailLabel.IconRole = TextRole.Tertiary;
                localDetailLabel.IconSvg = Icons.Branch;
                localDetailLabel.Text = "Choose the working copy of the repository";
                return;
            }

            localPathLabel.Text = Format.HomeRelative(_localRoot);
            localDetailLabel.Text = "Reading the repository…";
            var summary = await DeployPlanner.DescribeAsync(_localRoot);
            if (version != _describeVersion || IsDisposed) return;
            _summary = summary;

            if (!summary.IsRepository)
            {
                localDetailLabel.Role = TextRole.Warning;
                localDetailLabel.IconRole = TextRole.Warning;
                localDetailLabel.IconSvg = Icons.Warning;
                localDetailLabel.Text = "Not a git repository — the Commits tab needs one";
                return;
            }

            // A folder inside a repository pairs the repository and uploads from that folder.
            var root = summary.Root.TrimEnd('\\', '/');
            var chosen = Path.GetFullPath(_localRoot).TrimEnd('\\', '/');
            if (!string.Equals(root, chosen, StringComparison.OrdinalIgnoreCase) && chosen.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase))
            {
                var relative = chosen.Substring(root.Length + 1).Replace('\\', '/') + "/";
                if (buildBox.IsHandleCreated) buildBox.Text = relative;
                else _pendingBuildDirectory = relative;
            }
            _localRoot = summary.Root;
            localPathLabel.Text = Format.HomeRelative(_localRoot);
            localDetailLabel.Role = TextRole.Tertiary;
            localDetailLabel.IconRole = TextRole.Lane;
            localDetailLabel.IconSvg = Icons.Branch;
            localDetailLabel.Text = "git repository · " + (summary.Branch ?? "detached HEAD") + " · " + Format.Count(summary.CommitCount, "commit");
            pairButton.Enabled = true;
        }

        private void ChooseFolder()
        {
            var start = _localRoot;
            if (start == null)
            {
                var source = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos");
                start = Directory.Exists(source) ? source : null;
            }
            var folder = FolderPicker.Show(this, "Choose the local repository to pair with " + _remoteRoot, start);
            if (folder != null) SetLocal(folder);
        }

        private void chooseFolderButton_Click(object sender, EventArgs e) => ChooseFolder();

        private void useCurrentButton_Click(object sender, EventArgs e)
        {
            _remoteRoot = _currentPath;
            ShowRemote();
        }

        private void LayoutButtons()
        {
            chooseFolderButton.Width = chooseFolderButton.PreferredWidth;
            useCurrentButton.Left = chooseFolderButton.Right + 8;
            useCurrentButton.Width = Math.Min(content.Width - 24 - useCurrentButton.Left, useCurrentButton.PreferredWidth);
        }

        private void pairButton_Click(object sender, EventArgs e)
        {
            if (!pairButton.Enabled || _localRoot == null) return;
            var build = (buildBox.Text ?? string.Empty).Trim().Replace('\\', '/');
            if (build.StartsWith("/", StringComparison.Ordinal) || build.Contains(".."))
            {
                Dialogs.Warning(this, "Pair local folder", "The build output directory must be a folder inside the repository, such as dist/.");
                return;
            }

            var pairing = new Pairing
            {
                RemoteRoot = _remoteRoot,
                LocalRoot = _localRoot,
                Branch = _existing?.Branch,
                HonourIgnoreFiles = honourSwitch.Checked,
                ApplyDeletions = deletionsSwitch.Checked,
                BuildOutputDirectory = build.Length == 0 ? null : build,
                ConfirmBeforeDeploy = confirmSwitch.Checked,
            };
            // The deploy history belongs to the repository; it survives option changes.
            if (_existing != null && string.Equals(_existing.LocalRoot, _localRoot, StringComparison.OrdinalIgnoreCase))
            {
                pairing.LastDeployedSha = _existing.LastDeployedSha;
                pairing.DeployedShas = new System.Collections.Generic.List<string>(_existing.DeployedShas);
                pairing.SelectedSha = _existing.SelectedSha;
                pairing.DeselectedPaths = new System.Collections.Generic.List<string>(_existing.DeselectedPaths);
            }
            Result = pairing;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void unpairButton_Click(object sender, EventArgs e)
        {
            if (!Dialogs.Confirm(this, "Unpair", "Stop pairing " + _remoteRoot + " with " + Format.HomeRelative(_existing?.LocalRoot) + "?\n\nNothing is deleted on either side; the deploy history is forgotten.", "Unpair")) return;
            Unpaired = true;
            Result = null;
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
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
