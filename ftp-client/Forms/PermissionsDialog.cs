using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// Permissions and properties: the Read / Write / Execute grid for owner, group and everyone
    /// else, kept in step with the numeric and symbolic forms.
    /// </summary>
    public partial class PermissionsDialog : ModernForm
    {
        private readonly List<RemoteEntry> _entries;
        private readonly TokenCheckBox[] _bits;
        private bool _syncing;
        private int _mode;

        public PermissionsDialog(IEnumerable<RemoteEntry> entries, Site site)
        {
            InitializeComponent();
            Theme.Apply(skin);
            octalBox.OverrideSkinFont = true;
            octalBox.Font = Fonts.Code(13f);

            _entries = (entries ?? Enumerable.Empty<RemoteEntry>()).ToList();
            // Bit order matches the grid: owner rwx, group rwx, public rwx.
            _bits = new[] { ownerRead, ownerWrite, ownerExecute, groupRead, groupWrite, groupExecute, publicRead, publicWrite, publicExecute };

            var first = _entries.FirstOrDefault();
            _mode = first?.Mode ?? (first != null && first.IsDirectory ? 0x1ED : 0x1A4);
            bool single = _entries.Count == 1;
            bool anyDirectory = _entries.Any(e => e.IsDirectory);

            Text = (single ? first.Name : Format.Count(_entries.Count, "item")) + " — properties";
            nameLabel.Text = single ? first.Name : Format.Count(_entries.Count, "item");
            if (single)
            {
                var size = first.IsDirectory
                    ? (first.ItemCount.HasValue ? Format.Count(first.ItemCount.Value, "item") : "folder")
                    : Format.Bytes(first.Size);
                pathLabel.Text = first.FullPath + "  ·  " + size;
            }
            else
            {
                pathLabel.Text = first == null ? string.Empty : RemotePath.Parent(first.FullPath);
            }
            iconLabel.IconSvg = single && first.IsDirectory ? Icons.Folder : Icons.FileDetailed;
            iconLabel.IconRole = single && first.IsDirectory ? TextRole.Folder : TextRole.Secondary;

            var owners = _entries.Select(e => e.Owner).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
            var groups = _entries.Select(e => e.Group).Where(o => !string.IsNullOrEmpty(o)).Distinct().ToList();
            ownerDetail.Text = owners.Count == 1 ? owners[0] : owners.Count > 1 ? "several owners" : "the file's owner";
            groupDetail.Text = groups.Count == 1 ? groups[0] : groups.Count > 1 ? "several groups" : "the file's group";
            var summary = new List<string>();
            if (owners.Count == 1) summary.Add("Owner " + owners[0]);
            if (groups.Count == 1) summary.Add("Group " + groups[0]);
            ownershipLabel.Text = string.Join(" · ", summary);

            recurseBox.Visible = anyDirectory;
            if (site != null && site.Protocol == RemoteProtocol.Local)
            {
                localNote.Visible = true;
            }

            if (_entries.Select(e => e.Mode).Distinct().Count() > 1)
            {
                // Mixed selections start from the first item's bits; Apply sets them all alike.
                modeNote.Visible = true;
            }

            foreach (var bit in _bits) bit.CheckedChanged += bit_CheckedChanged;
            ShowMode();
        }

        /// <summary>The mode bits to apply (the 0777 part).</summary>
        public int Mode => _mode & 0x1FF;

        // Read after the dialog closes, when every control reports itself hidden.
        public bool Recurse => _anyDirectory && recurseBox.Checked;

        private bool _anyDirectory => _entries.Any(e => e.IsDirectory);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ButtonRow.ArrangeRight(content.ClientSize.Width - 24, 8, 80, applyButton, cancelButton);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // ModernTextBox keeps text only once its edit box exists.
            ShowMode();
            applyButton.Focus();
        }

        private void ShowMode()
        {
            _syncing = true;
            try
            {
                for (int i = 0; i < 9; i++) _bits[i].Checked = (_mode & (1 << (8 - i))) != 0;
                if (octalBox.IsHandleCreated && octalBox.Text != Permissions.Octal(_mode)) octalBox.Text = Permissions.Octal(_mode);
                var kind = _entries.Count == 1 ? _entries[0].Kind : RemoteEntryKind.File;
                symbolicLabel.Text = Permissions.Symbolic(_mode, kind);
                applyButton.Enabled = true;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void bit_CheckedChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            int mode = 0;
            for (int i = 0; i < 9; i++) if (_bits[i].Checked) mode |= 1 << (8 - i);
            _mode = mode;
            ShowMode();
        }

        private void octalBox_TextChanged(object sender, EventArgs e)
        {
            if (_syncing) return;
            var parsed = Permissions.ParseOctal(octalBox.Text);
            if (!parsed.HasValue)
            {
                applyButton.Enabled = false;
                return;
            }
            _mode = parsed.Value;
            _syncing = true;
            try
            {
                for (int i = 0; i < 9; i++) _bits[i].Checked = (_mode & (1 << (8 - i))) != 0;
                var kind = _entries.Count == 1 ? _entries[0].Kind : RemoteEntryKind.File;
                symbolicLabel.Text = Permissions.Symbolic(_mode, kind);
                applyButton.Enabled = true;
            }
            finally
            {
                _syncing = false;
            }
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (!applyButton.Enabled) return;
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
            if (keyData == Keys.Enter)
            {
                applyButton_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
