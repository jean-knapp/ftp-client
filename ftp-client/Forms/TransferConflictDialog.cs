using System;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using FtpClient.Transfers;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// "index.php already exists on web-prod": the incoming and existing copies side by side, and
    /// Overwrite, Overwrite if newer, Resume (only for a shorter copy) or Rename.
    /// </summary>
    public partial class TransferConflictDialog : ModernForm
    {
        private readonly ConflictInfo _info;

        public TransferConflictDialog(ConflictInfo info)
        {
            InitializeComponent();
            Theme.Apply(skin);
            _info = info ?? throw new ArgumentNullException(nameof(info));

            var item = info.Item;
            bool upload = item.Direction != TransferDirection.Download;
            var siteName = item.Site?.Name ?? item.Site?.Host ?? "the server";

            titleLabel.Text = item.FileName + (upload ? " already exists on " + siteName : " already exists in " + System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(item.LocalPath) ?? string.Empty));
            pathLabel.Text = upload ? item.RemotePath : Format.HomeRelative(item.LocalPath);

            incomingCaption.Text = upload ? "Local — uploading" : "Remote — downloading";
            existingCaption.Text = upload ? "Remote — on server" : "Local — on disk";
            incomingSize.Text = Format.Bytes(info.IncomingSize);
            existingSize.Text = Format.Bytes(info.ExistingSize);
            incomingModified.Text = Describe(info.IncomingModified);
            existingModified.Text = Describe(info.ExistingModified);
            ShowVerdicts();

            overwriteOption.Description = upload ? "Replace the remote file with the local one" : "Replace the local file with the remote one";
            newerOption.Description = upload ? "Only transfer when the local file is more recent" : "Only transfer when the remote file is more recent";
            resumeOption.Description = "Append from byte " + info.ExistingSize.ToString("N0") + " — " + (upload ? "remote" : "local") + " is a partial copy";
            resumeOption.Visible = info.CanResume;
            var renamed = upload ? RemotePath.Name(info.RenameTo ?? string.Empty) : System.IO.Path.GetFileName(info.RenameTo ?? string.Empty);
            renameOption.Description = (upload ? "Upload as " : "Download as ") + renamed + " and keep both";
            renameOption.Enabled = !string.IsNullOrEmpty(info.RenameTo);

            applyAllBox.Text = info.RemainingConflicts > 0
                ? "Apply to the remaining " + Format.Count(info.RemainingConflicts, "transfer") + " in this queue"
                : "Apply to any other conflicts in this queue";

            // A partial copy most likely wants resuming; otherwise the newer file wins.
            if (info.CanResume) resumeOption.Checked = true;
            else overwriteOption.Checked = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutOptions();
        }

        /// <summary>The user's choice; Cancel when the dialog was dismissed.</summary>
        public ConflictResolution Resolution { get; private set; } = new ConflictResolution(ConflictChoice.Cancel);

        private static string Describe(DateTime? utc)
        {
            if (!utc.HasValue) return "unknown";
            return Format.CommitDate(new DateTimeOffset(utc.Value.ToLocalTime()));
        }

        private void ShowVerdicts()
        {
            incomingVerdict.Visible = false;
            existingVerdict.Visible = false;
            if (!_info.IncomingModified.HasValue || !_info.ExistingModified.HasValue) return;
            var difference = _info.IncomingModified.Value - _info.ExistingModified.Value;
            if (Math.Abs(difference.TotalSeconds) < 2) return;
            bool incomingNewer = difference.TotalSeconds > 0;
            var newer = incomingNewer ? incomingVerdict : existingVerdict;
            var older = incomingNewer ? existingVerdict : incomingVerdict;
            newer.Style = ChipStyle.Up;
            newer.Text = "Newer";
            older.Style = ChipStyle.Neutral;
            older.Text = Age(difference.Duration()) + " older";
            newer.Visible = true;
            older.Visible = true;
        }

        private static string Age(TimeSpan span)
        {
            if (span.TotalDays >= 1) return Format.Count((int)span.TotalDays, "day");
            if (span.TotalHours >= 1) return Format.Count((int)span.TotalHours, "hour");
            if (span.TotalMinutes >= 1) return Format.Count((int)span.TotalMinutes, "minute");
            return Format.Count((int)span.TotalSeconds, "second");
        }

        private void LayoutOptions()
        {
            const int top = 212;
            const int height = 48;
            const int gap = 6;
            int y = top;
            // Decided from the conflict, not from Visible: every control reads as hidden until the
            // dialog is on screen.
            var options = _info.CanResume
                ? new[] { overwriteOption, newerOption, resumeOption, renameOption }
                : new[] { overwriteOption, newerOption, renameOption };
            foreach (var option in options)
            {
                option.SetBounds(24, y, content.Width - 48, height);
                y += height + gap;
            }
            applyAllBox.SetBounds(24, y + 8, content.Width - 48, 22);
            int buttonsTop = y + 46;
            continueButton.Top = skipButton.Top = cancelButton.Top = buttonsTop;
            ButtonRow.ArrangeRight(content.Width - 24, 8, 80, continueButton, skipButton, cancelButton);
            ClientSize = new System.Drawing.Size(ClientSize.Width, 32 + buttonsTop + 34 + 22);
        }

        private ConflictChoice SelectedChoice
        {
            get
            {
                if (newerOption.Checked) return ConflictChoice.OverwriteIfNewer;
                if (resumeOption.Checked && resumeOption.Visible) return ConflictChoice.Resume;
                if (renameOption.Checked) return ConflictChoice.Rename;
                return ConflictChoice.Overwrite;
            }
        }

        private void continueButton_Click(object sender, EventArgs e)
        {
            Resolution = new ConflictResolution(SelectedChoice, applyAllBox.Checked);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void skipButton_Click(object sender, EventArgs e)
        {
            Resolution = new ConflictResolution(ConflictChoice.Skip, applyAllBox.Checked);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Resolution = new ConflictResolution(ConflictChoice.Cancel);
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void option_DoubleClick(object sender, EventArgs e) => continueButton_Click(sender, e);

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                cancelButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == Keys.Enter)
            {
                continueButton_Click(this, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
