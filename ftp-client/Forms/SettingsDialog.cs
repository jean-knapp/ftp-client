using System;
using System.IO;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Git;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    public partial class SettingsDialog : ModernForm
    {
        private static readonly string[] PageTitles = { "Transfers", "Connection", "Editors", "Appearance" };

        private ThemeMode _originalTheme;

        public SettingsDialog()
        {
            InitializeComponent();
            Theme.Apply(skin);
            foreach (var box in new[] { directoryBox, editorBox, gitPathBox })
            {
                box.OverrideSkinFont = true;
                box.Font = Fonts.Code(13f);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _originalTheme = Theme.Mode;
            ButtonRow.ArrangeRight(contentPanel.ClientSize.Width - 20, 8, 80, saveButton, cancelButton);
            navRail.SetSelection(0, false);
            ShowPage(0);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // The library's text boxes and spinners keep values only once their handles exist.
            var settings = AppSettings.Current;
            concurrentBox.Value = Math.Max(1, Math.Min(16, settings.ConcurrentTransfers));
            modeBox.SelectedIndex = (int)settings.TransferMode;
            speedSwitch.SetCheckedQuiet(settings.SpeedLimitEnabled);
            speedBox.Value = Math.Max(16, Math.Min(1048576, settings.SpeedLimitKilobytes));
            speedBox.Visible = settings.SpeedLimitEnabled;
            speedUnitLabel.Visible = settings.SpeedLimitEnabled;
            resumeSwitch.SetCheckedQuiet(settings.ResumeTransfers);
            directoryBox.Text = settings.DefaultRemoteDirectory ?? string.Empty;
            preserveSwitch.SetCheckedQuiet(settings.PreserveTimestamps);

            timeoutBox.Value = Math.Max(5, Math.Min(300, settings.ConnectTimeoutSeconds));
            keepAliveBox.Value = Math.Max(0, Math.Min(3600, settings.KeepAliveSeconds));
            passiveSwitch.SetCheckedQuiet(settings.PassiveMode);

            editorBox.Text = settings.EditorPath ?? string.Empty;
            gitPathBox.Text = settings.GitExecutable ?? string.Empty;

            themeToggle.SelectedIndex = Theme.Mode == ThemeMode.Light ? 1 : 0;
            themeToggle.Width = themeToggle.PreferredWidth;
            themeToggle.Left = themeRow.ClientSize.Width - 16 - themeToggle.Width;
            rowHeightBox.Value = Math.Max(26, Math.Min(38, settings.FileRowHeight));
            hiddenSwitch.SetCheckedQuiet(settings.ShowHiddenFiles);
            groupingBox.SelectedIndex = (int)settings.QueueGrouping;
        }

        private void navRail_SelectionChanged(object sender, EventArgs e) => ShowPage(navRail.SelectedIndex);

        private void ShowPage(int index)
        {
            if (index < 0 || index >= PageTitles.Length) return;
            pageTitleLabel.Text = PageTitles[index];
            transfersPage.Visible = index == 0;
            connectionPage.Visible = index == 1;
            editorsPage.Visible = index == 2;
            appearancePage.Visible = index == 3;
        }

        private void speedSwitch_CheckedChanged(object sender, EventArgs e)
        {
            speedBox.Visible = speedSwitch.Checked;
            speedUnitLabel.Visible = speedSwitch.Checked;
        }

        /// <summary>The theme previews live, so the choice can be judged before saving.</summary>
        private void themeToggle_SelectedIndexChanged(object sender, EventArgs e)
        {
            Theme.Mode = themeToggle.SelectedIndex == 1 ? ThemeMode.Light : ThemeMode.Dark;
            Theme.Apply(skin);
            Invalidate(true);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            var gitPath = gitPathBox.Text.Trim();
            if (gitPath.Length > 0 && !File.Exists(gitPath))
            {
                navRail.SetSelection(2, true);
                Dialogs.Warning(this, "Settings", "git.exe was not found at:\n\n" + gitPath);
                return;
            }
            var editor = editorBox.Text.Trim().Trim('"');
            if (editor.Length > 0 && Path.IsPathRooted(editor) && !File.Exists(editor))
            {
                navRail.SetSelection(2, true);
                Dialogs.Warning(this, "Settings", "The editor was not found at:\n\n" + editor);
                return;
            }

            var settings = AppSettings.Current;
            settings.ConcurrentTransfers = (int)concurrentBox.Value;
            settings.TransferMode = (TransferMode)Math.Max(0, modeBox.SelectedIndex);
            settings.SpeedLimitEnabled = speedSwitch.Checked;
            settings.SpeedLimitKilobytes = (int)speedBox.Value;
            settings.ResumeTransfers = resumeSwitch.Checked;
            var directory = directoryBox.Text.Trim();
            settings.DefaultRemoteDirectory = directory.Length == 0 ? null : RemotePath.Normalize(directory);
            settings.PreserveTimestamps = preserveSwitch.Checked;

            settings.ConnectTimeoutSeconds = (int)timeoutBox.Value;
            settings.KeepAliveSeconds = (int)keepAliveBox.Value;
            settings.PassiveMode = passiveSwitch.Checked;

            settings.EditorPath = editor.Length > 0 ? editor : null;
            settings.GitExecutable = gitPath.Length > 0 ? gitPath : null;

            settings.FileRowHeight = (int)rowHeightBox.Value;
            settings.ShowHiddenFiles = hiddenSwitch.Checked;
            settings.QueueGrouping = (QueueGrouping)Math.Max(0, groupingBox.SelectedIndex);
            settings.Theme = Theme.Mode;
            settings.Save();
            GitRunner.GitExecutable = settings.GitExecutable ?? GitRunner.FindGit();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            // Undo the live theme preview when the dialog is dismissed.
            Theme.Mode = _originalTheme;
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                cancelButton_Click(cancelButton, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
