namespace FtpClient.Forms
{
    partial class SettingsDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.skin = new ModernWinForms.ModernSkin();
            this.navRail = new FtpClient.Controls.NavRailControl();
            this.contentPanel = new FtpClient.Controls.SurfacePanel();
            this.pageTitleLabel = new FtpClient.Controls.TextLabel();
            this.transfersPage = new FtpClient.Controls.SurfacePanel();
            this.transfersGroup = new FtpClient.Controls.SurfacePanel();
            this.preserveRow = new FtpClient.Controls.SurfacePanel();
            this.preserveLabel = new FtpClient.Controls.TextLabel();
            this.preserveHint = new FtpClient.Controls.TextLabel();
            this.preserveSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.directoryRow = new FtpClient.Controls.SurfacePanel();
            this.directoryLabel = new FtpClient.Controls.TextLabel();
            this.directoryHint = new FtpClient.Controls.TextLabel();
            this.directoryBox = new ModernWinForms.ModernTextBox();
            this.resumeRow = new FtpClient.Controls.SurfacePanel();
            this.resumeLabel = new FtpClient.Controls.TextLabel();
            this.resumeHint = new FtpClient.Controls.TextLabel();
            this.resumeSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.speedRow = new FtpClient.Controls.SurfacePanel();
            this.speedLabel = new FtpClient.Controls.TextLabel();
            this.speedHint = new FtpClient.Controls.TextLabel();
            this.speedBox = new ModernWinForms.ModernNumericUpDown();
            this.speedUnitLabel = new FtpClient.Controls.TextLabel();
            this.speedSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.modeRow = new FtpClient.Controls.SurfacePanel();
            this.modeLabel = new FtpClient.Controls.TextLabel();
            this.modeHint = new FtpClient.Controls.TextLabel();
            this.modeBox = new ModernWinForms.ModernComboBox();
            this.concurrentRow = new FtpClient.Controls.SurfacePanel();
            this.concurrentLabel = new FtpClient.Controls.TextLabel();
            this.concurrentHint = new FtpClient.Controls.TextLabel();
            this.concurrentBox = new ModernWinForms.ModernNumericUpDown();
            this.connectionPage = new FtpClient.Controls.SurfacePanel();
            this.connectionGroup = new FtpClient.Controls.SurfacePanel();
            this.passiveRow = new FtpClient.Controls.SurfacePanel();
            this.passiveLabel = new FtpClient.Controls.TextLabel();
            this.passiveHint = new FtpClient.Controls.TextLabel();
            this.passiveSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.keepAliveRow = new FtpClient.Controls.SurfacePanel();
            this.keepAliveLabel = new FtpClient.Controls.TextLabel();
            this.keepAliveHint = new FtpClient.Controls.TextLabel();
            this.keepAliveBox = new ModernWinForms.ModernNumericUpDown();
            this.timeoutRow = new FtpClient.Controls.SurfacePanel();
            this.timeoutLabel = new FtpClient.Controls.TextLabel();
            this.timeoutHint = new FtpClient.Controls.TextLabel();
            this.timeoutBox = new ModernWinForms.ModernNumericUpDown();
            this.editorsPage = new FtpClient.Controls.SurfacePanel();
            this.editorsGroup = new FtpClient.Controls.SurfacePanel();
            this.gitRow = new FtpClient.Controls.SurfacePanel();
            this.gitLabel = new FtpClient.Controls.TextLabel();
            this.gitHint = new FtpClient.Controls.TextLabel();
            this.gitPathBox = new ModernWinForms.ModernTextBox();
            this.editorRow = new FtpClient.Controls.SurfacePanel();
            this.editorLabel = new FtpClient.Controls.TextLabel();
            this.editorHint = new FtpClient.Controls.TextLabel();
            this.editorBox = new ModernWinForms.ModernTextBox();
            this.appearancePage = new FtpClient.Controls.SurfacePanel();
            this.appearanceGroup = new FtpClient.Controls.SurfacePanel();
            this.groupingRow = new FtpClient.Controls.SurfacePanel();
            this.groupingLabel = new FtpClient.Controls.TextLabel();
            this.groupingHint = new FtpClient.Controls.TextLabel();
            this.groupingBox = new ModernWinForms.ModernComboBox();
            this.hiddenRow = new FtpClient.Controls.SurfacePanel();
            this.hiddenLabel = new FtpClient.Controls.TextLabel();
            this.hiddenHint = new FtpClient.Controls.TextLabel();
            this.hiddenSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.rowHeightRow = new FtpClient.Controls.SurfacePanel();
            this.rowHeightLabel = new FtpClient.Controls.TextLabel();
            this.rowHeightHint = new FtpClient.Controls.TextLabel();
            this.rowHeightBox = new ModernWinForms.ModernNumericUpDown();
            this.themeRow = new FtpClient.Controls.SurfacePanel();
            this.themeLabel = new FtpClient.Controls.TextLabel();
            this.themeHint = new FtpClient.Controls.TextLabel();
            this.themeToggle = new FtpClient.Controls.SegmentedControl();
            this.saveButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.contentPanel.SuspendLayout();
            this.transfersPage.SuspendLayout();
            this.transfersGroup.SuspendLayout();
            this.preserveRow.SuspendLayout();
            this.directoryRow.SuspendLayout();
            this.resumeRow.SuspendLayout();
            this.speedRow.SuspendLayout();
            this.modeRow.SuspendLayout();
            this.concurrentRow.SuspendLayout();
            this.connectionPage.SuspendLayout();
            this.connectionGroup.SuspendLayout();
            this.passiveRow.SuspendLayout();
            this.keepAliveRow.SuspendLayout();
            this.timeoutRow.SuspendLayout();
            this.editorsPage.SuspendLayout();
            this.editorsGroup.SuspendLayout();
            this.gitRow.SuspendLayout();
            this.editorRow.SuspendLayout();
            this.appearancePage.SuspendLayout();
            this.appearanceGroup.SuspendLayout();
            this.groupingRow.SuspendLayout();
            this.hiddenRow.SuspendLayout();
            this.rowHeightRow.SuspendLayout();
            this.themeRow.SuspendLayout();
            this.SuspendLayout();
            //
            // navRail
            //
            this.navRail.Dock = System.Windows.Forms.DockStyle.Left;
            this.navRail.Items.Add("Transfers");
            this.navRail.Items.Add("Connection");
            this.navRail.Items.Add("Editors");
            this.navRail.Items.Add("Appearance");
            this.navRail.Location = new System.Drawing.Point(0, 31);
            this.navRail.Name = "navRail";
            this.navRail.Size = new System.Drawing.Size(188, 589);
            this.navRail.TabIndex = 0;
            this.navRail.SelectionChanged += new System.EventHandler(this.navRail_SelectionChanged);
            //
            // contentPanel
            //
            this.contentPanel.Controls.Add(this.transfersPage);
            this.contentPanel.Controls.Add(this.connectionPage);
            this.contentPanel.Controls.Add(this.editorsPage);
            this.contentPanel.Controls.Add(this.appearancePage);
            this.contentPanel.Controls.Add(this.pageTitleLabel);
            this.contentPanel.Controls.Add(this.saveButton);
            this.contentPanel.Controls.Add(this.cancelButton);
            this.contentPanel.CornerRadius = 0;
            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.contentPanel.Location = new System.Drawing.Point(188, 31);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(532, 589);
            this.contentPanel.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.contentPanel.TabIndex = 1;
            //
            // pageTitleLabel
            //
            this.pageTitleLabel.Location = new System.Drawing.Point(8, 14);
            this.pageTitleLabel.Name = "pageTitleLabel";
            this.pageTitleLabel.Semibold = true;
            this.pageTitleLabel.Size = new System.Drawing.Size(400, 28);
            this.pageTitleLabel.SizePx = 20F;
            this.pageTitleLabel.TabIndex = 0;
            this.pageTitleLabel.Text = "Transfers";
            //
            // transfersPage
            //
            this.transfersPage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.transfersPage.Controls.Add(this.transfersGroup);
            this.transfersPage.CornerRadius = 0;
            this.transfersPage.Location = new System.Drawing.Point(8, 52);
            this.transfersPage.Name = "transfersPage";
            this.transfersPage.Size = new System.Drawing.Size(504, 460);
            this.transfersPage.Surface = FtpClient.Controls.SurfaceKind.None;
            this.transfersPage.TabIndex = 1;
            //
            // transfersGroup
            //
            this.transfersGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.transfersGroup.Controls.Add(this.preserveRow);
            this.transfersGroup.Controls.Add(this.directoryRow);
            this.transfersGroup.Controls.Add(this.resumeRow);
            this.transfersGroup.Controls.Add(this.speedRow);
            this.transfersGroup.Controls.Add(this.modeRow);
            this.transfersGroup.Controls.Add(this.concurrentRow);
            this.transfersGroup.CornerRadius = 7;
            this.transfersGroup.Location = new System.Drawing.Point(0, 0);
            this.transfersGroup.Name = "transfersGroup";
            this.transfersGroup.Padding = new System.Windows.Forms.Padding(1);
            this.transfersGroup.Size = new System.Drawing.Size(504, 350);
            this.transfersGroup.TabIndex = 0;
            //
            // concurrentRow
            //
            this.concurrentRow.BottomDivider = true;
            this.concurrentRow.Controls.Add(this.concurrentLabel);
            this.concurrentRow.Controls.Add(this.concurrentHint);
            this.concurrentRow.Controls.Add(this.concurrentBox);
            this.concurrentRow.CornerRadius = 0;
            this.concurrentRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.concurrentRow.Location = new System.Drawing.Point(1, 1);
            this.concurrentRow.Name = "concurrentRow";
            this.concurrentRow.Size = new System.Drawing.Size(502, 58);
            this.concurrentRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.concurrentRow.TabIndex = 0;
            //
            // concurrentLabel
            //
            this.concurrentLabel.Location = new System.Drawing.Point(16, 12);
            this.concurrentLabel.Name = "concurrentLabel";
            this.concurrentLabel.Size = new System.Drawing.Size(240, 19);
            this.concurrentLabel.SizePx = 13.5F;
            this.concurrentLabel.TabIndex = 0;
            this.concurrentLabel.Text = "Concurrent transfers";
            //
            // concurrentHint
            //
            this.concurrentHint.Location = new System.Drawing.Point(16, 31);
            this.concurrentHint.Name = "concurrentHint";
            this.concurrentHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.concurrentHint.Size = new System.Drawing.Size(240, 17);
            this.concurrentHint.SizePx = 12F;
            this.concurrentHint.TabIndex = 1;
            this.concurrentHint.Text = "Parallel connections per site";
            //
            // concurrentBox
            //
            this.concurrentBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.concurrentBox.Location = new System.Drawing.Point(268, 13);
            this.concurrentBox.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
            this.concurrentBox.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.concurrentBox.Name = "concurrentBox";
            this.concurrentBox.Size = new System.Drawing.Size(218, 32);
            this.concurrentBox.TabIndex = 2;
            this.concurrentBox.ThousandSeparator = "";
            //
            // modeRow
            //
            this.modeRow.BottomDivider = true;
            this.modeRow.Controls.Add(this.modeLabel);
            this.modeRow.Controls.Add(this.modeHint);
            this.modeRow.Controls.Add(this.modeBox);
            this.modeRow.CornerRadius = 0;
            this.modeRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.modeRow.Location = new System.Drawing.Point(1, 59);
            this.modeRow.Name = "modeRow";
            this.modeRow.Size = new System.Drawing.Size(502, 58);
            this.modeRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.modeRow.TabIndex = 1;
            //
            // modeLabel
            //
            this.modeLabel.Location = new System.Drawing.Point(16, 12);
            this.modeLabel.Name = "modeLabel";
            this.modeLabel.Size = new System.Drawing.Size(240, 19);
            this.modeLabel.SizePx = 13.5F;
            this.modeLabel.TabIndex = 0;
            this.modeLabel.Text = "Transfer mode";
            //
            // modeHint
            //
            this.modeHint.Location = new System.Drawing.Point(16, 31);
            this.modeHint.Name = "modeHint";
            this.modeHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.modeHint.Size = new System.Drawing.Size(240, 17);
            this.modeHint.SizePx = 12F;
            this.modeHint.TabIndex = 1;
            this.modeHint.Text = "FTP only — SFTP is always binary";
            //
            // modeBox
            //
            this.modeBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.modeBox.Items.AddRange(new object[] {
            "Binary",
            "ASCII",
            "Automatic"});
            this.modeBox.Location = new System.Drawing.Point(268, 13);
            this.modeBox.Name = "modeBox";
            this.modeBox.Size = new System.Drawing.Size(218, 32);
            this.modeBox.TabIndex = 2;
            //
            // speedRow
            //
            this.speedRow.BottomDivider = true;
            this.speedRow.Controls.Add(this.speedLabel);
            this.speedRow.Controls.Add(this.speedHint);
            this.speedRow.Controls.Add(this.speedBox);
            this.speedRow.Controls.Add(this.speedUnitLabel);
            this.speedRow.Controls.Add(this.speedSwitch);
            this.speedRow.CornerRadius = 0;
            this.speedRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.speedRow.Location = new System.Drawing.Point(1, 117);
            this.speedRow.Name = "speedRow";
            this.speedRow.Size = new System.Drawing.Size(502, 58);
            this.speedRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.speedRow.TabIndex = 2;
            //
            // speedLabel
            //
            this.speedLabel.Location = new System.Drawing.Point(16, 12);
            this.speedLabel.Name = "speedLabel";
            this.speedLabel.Size = new System.Drawing.Size(200, 19);
            this.speedLabel.SizePx = 13.5F;
            this.speedLabel.TabIndex = 0;
            this.speedLabel.Text = "Speed limit";
            //
            // speedHint
            //
            this.speedHint.Location = new System.Drawing.Point(16, 31);
            this.speedHint.Name = "speedHint";
            this.speedHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.speedHint.Size = new System.Drawing.Size(220, 17);
            this.speedHint.SizePx = 12F;
            this.speedHint.TabIndex = 1;
            this.speedHint.Text = "Cap upload and download throughput";
            //
            // speedBox
            //
            this.speedBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.speedBox.Location = new System.Drawing.Point(262, 13);
            this.speedBox.Maximum = new decimal(new int[] { 1048576, 0, 0, 0 });
            this.speedBox.Minimum = new decimal(new int[] { 16, 0, 0, 0 });
            this.speedBox.Name = "speedBox";
            this.speedBox.Size = new System.Drawing.Size(100, 32);
            this.speedBox.TabIndex = 2;
            this.speedBox.ThousandSeparator = "";
            this.speedBox.Value = new decimal(new int[] { 2048, 0, 0, 0 });
            this.speedBox.Visible = false;
            //
            // speedUnitLabel
            //
            this.speedUnitLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.speedUnitLabel.Location = new System.Drawing.Point(368, 13);
            this.speedUnitLabel.Name = "speedUnitLabel";
            this.speedUnitLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.speedUnitLabel.Size = new System.Drawing.Size(60, 32);
            this.speedUnitLabel.SizePx = 12.5F;
            this.speedUnitLabel.TabIndex = 3;
            this.speedUnitLabel.Text = "KB/s";
            this.speedUnitLabel.Visible = false;
            //
            // speedSwitch
            //
            this.speedSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.speedSwitch.Location = new System.Drawing.Point(446, 19);
            this.speedSwitch.Name = "speedSwitch";
            this.speedSwitch.Size = new System.Drawing.Size(40, 20);
            this.speedSwitch.TabIndex = 4;
            this.speedSwitch.CheckedChanged += new System.EventHandler(this.speedSwitch_CheckedChanged);
            //
            // resumeRow
            //
            this.resumeRow.BottomDivider = true;
            this.resumeRow.Controls.Add(this.resumeLabel);
            this.resumeRow.Controls.Add(this.resumeHint);
            this.resumeRow.Controls.Add(this.resumeSwitch);
            this.resumeRow.CornerRadius = 0;
            this.resumeRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.resumeRow.Location = new System.Drawing.Point(1, 175);
            this.resumeRow.Name = "resumeRow";
            this.resumeRow.Size = new System.Drawing.Size(502, 58);
            this.resumeRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.resumeRow.TabIndex = 3;
            //
            // resumeLabel
            //
            this.resumeLabel.Location = new System.Drawing.Point(16, 12);
            this.resumeLabel.Name = "resumeLabel";
            this.resumeLabel.Size = new System.Drawing.Size(300, 19);
            this.resumeLabel.SizePx = 13.5F;
            this.resumeLabel.TabIndex = 0;
            this.resumeLabel.Text = "Resume interrupted transfers";
            //
            // resumeHint
            //
            this.resumeHint.Location = new System.Drawing.Point(16, 31);
            this.resumeHint.Name = "resumeHint";
            this.resumeHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.resumeHint.Size = new System.Drawing.Size(300, 17);
            this.resumeHint.SizePx = 12F;
            this.resumeHint.TabIndex = 1;
            this.resumeHint.Text = "Continue from the last confirmed byte";
            //
            // resumeSwitch
            //
            this.resumeSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.resumeSwitch.Location = new System.Drawing.Point(446, 19);
            this.resumeSwitch.Name = "resumeSwitch";
            this.resumeSwitch.Size = new System.Drawing.Size(40, 20);
            this.resumeSwitch.TabIndex = 2;
            //
            // directoryRow
            //
            this.directoryRow.BottomDivider = true;
            this.directoryRow.Controls.Add(this.directoryLabel);
            this.directoryRow.Controls.Add(this.directoryHint);
            this.directoryRow.Controls.Add(this.directoryBox);
            this.directoryRow.CornerRadius = 0;
            this.directoryRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.directoryRow.Location = new System.Drawing.Point(1, 233);
            this.directoryRow.Name = "directoryRow";
            this.directoryRow.Size = new System.Drawing.Size(502, 58);
            this.directoryRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.directoryRow.TabIndex = 4;
            //
            // directoryLabel
            //
            this.directoryLabel.Location = new System.Drawing.Point(16, 12);
            this.directoryLabel.Name = "directoryLabel";
            this.directoryLabel.Size = new System.Drawing.Size(240, 19);
            this.directoryLabel.SizePx = 13.5F;
            this.directoryLabel.TabIndex = 0;
            this.directoryLabel.Text = "Default remote directory";
            //
            // directoryHint
            //
            this.directoryHint.Location = new System.Drawing.Point(16, 31);
            this.directoryHint.Name = "directoryHint";
            this.directoryHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.directoryHint.Size = new System.Drawing.Size(244, 17);
            this.directoryHint.SizePx = 12F;
            this.directoryHint.TabIndex = 1;
            this.directoryHint.Text = "Opened on connect when a site has none";
            //
            // directoryBox
            //
            this.directoryBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.directoryBox.Location = new System.Drawing.Point(268, 13);
            this.directoryBox.Name = "directoryBox";
            this.directoryBox.PlaceholderText = "home directory";
            this.directoryBox.Size = new System.Drawing.Size(218, 32);
            this.directoryBox.TabIndex = 2;
            //
            // preserveRow
            //
            this.preserveRow.Controls.Add(this.preserveLabel);
            this.preserveRow.Controls.Add(this.preserveHint);
            this.preserveRow.Controls.Add(this.preserveSwitch);
            this.preserveRow.CornerRadius = 0;
            this.preserveRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.preserveRow.Location = new System.Drawing.Point(1, 291);
            this.preserveRow.Name = "preserveRow";
            this.preserveRow.Size = new System.Drawing.Size(502, 58);
            this.preserveRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.preserveRow.TabIndex = 5;
            //
            // preserveLabel
            //
            this.preserveLabel.Location = new System.Drawing.Point(16, 12);
            this.preserveLabel.Name = "preserveLabel";
            this.preserveLabel.Size = new System.Drawing.Size(300, 19);
            this.preserveLabel.SizePx = 13.5F;
            this.preserveLabel.TabIndex = 0;
            this.preserveLabel.Text = "Preserve timestamps";
            //
            // preserveHint
            //
            this.preserveHint.Location = new System.Drawing.Point(16, 31);
            this.preserveHint.Name = "preserveHint";
            this.preserveHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.preserveHint.Size = new System.Drawing.Size(300, 17);
            this.preserveHint.SizePx = 12F;
            this.preserveHint.TabIndex = 1;
            this.preserveHint.Text = "Set the remote mtime to match the local file";
            //
            // preserveSwitch
            //
            this.preserveSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.preserveSwitch.Location = new System.Drawing.Point(446, 19);
            this.preserveSwitch.Name = "preserveSwitch";
            this.preserveSwitch.Size = new System.Drawing.Size(40, 20);
            this.preserveSwitch.TabIndex = 2;
            //
            // connectionPage
            //
            this.connectionPage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionPage.Controls.Add(this.connectionGroup);
            this.connectionPage.CornerRadius = 0;
            this.connectionPage.Location = new System.Drawing.Point(8, 52);
            this.connectionPage.Name = "connectionPage";
            this.connectionPage.Size = new System.Drawing.Size(504, 460);
            this.connectionPage.Surface = FtpClient.Controls.SurfaceKind.None;
            this.connectionPage.TabIndex = 2;
            this.connectionPage.Visible = false;
            //
            // connectionGroup
            //
            this.connectionGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.connectionGroup.Controls.Add(this.passiveRow);
            this.connectionGroup.Controls.Add(this.keepAliveRow);
            this.connectionGroup.Controls.Add(this.timeoutRow);
            this.connectionGroup.CornerRadius = 7;
            this.connectionGroup.Location = new System.Drawing.Point(0, 0);
            this.connectionGroup.Name = "connectionGroup";
            this.connectionGroup.Padding = new System.Windows.Forms.Padding(1);
            this.connectionGroup.Size = new System.Drawing.Size(504, 176);
            this.connectionGroup.TabIndex = 0;
            //
            // timeoutRow
            //
            this.timeoutRow.BottomDivider = true;
            this.timeoutRow.Controls.Add(this.timeoutLabel);
            this.timeoutRow.Controls.Add(this.timeoutHint);
            this.timeoutRow.Controls.Add(this.timeoutBox);
            this.timeoutRow.CornerRadius = 0;
            this.timeoutRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.timeoutRow.Location = new System.Drawing.Point(1, 1);
            this.timeoutRow.Name = "timeoutRow";
            this.timeoutRow.Size = new System.Drawing.Size(502, 58);
            this.timeoutRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.timeoutRow.TabIndex = 0;
            //
            // timeoutLabel
            //
            this.timeoutLabel.Location = new System.Drawing.Point(16, 12);
            this.timeoutLabel.Name = "timeoutLabel";
            this.timeoutLabel.Size = new System.Drawing.Size(240, 19);
            this.timeoutLabel.SizePx = 13.5F;
            this.timeoutLabel.TabIndex = 0;
            this.timeoutLabel.Text = "Connect timeout";
            //
            // timeoutHint
            //
            this.timeoutHint.Location = new System.Drawing.Point(16, 31);
            this.timeoutHint.Name = "timeoutHint";
            this.timeoutHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.timeoutHint.Size = new System.Drawing.Size(244, 17);
            this.timeoutHint.SizePx = 12F;
            this.timeoutHint.TabIndex = 1;
            this.timeoutHint.Text = "Seconds to wait for the server to answer";
            //
            // timeoutBox
            //
            this.timeoutBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.timeoutBox.Location = new System.Drawing.Point(268, 13);
            this.timeoutBox.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            this.timeoutBox.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            this.timeoutBox.Name = "timeoutBox";
            this.timeoutBox.Size = new System.Drawing.Size(218, 32);
            this.timeoutBox.TabIndex = 2;
            this.timeoutBox.ThousandSeparator = "";
            this.timeoutBox.Value = new decimal(new int[] { 30, 0, 0, 0 });
            //
            // keepAliveRow
            //
            this.keepAliveRow.BottomDivider = true;
            this.keepAliveRow.Controls.Add(this.keepAliveLabel);
            this.keepAliveRow.Controls.Add(this.keepAliveHint);
            this.keepAliveRow.Controls.Add(this.keepAliveBox);
            this.keepAliveRow.CornerRadius = 0;
            this.keepAliveRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.keepAliveRow.Location = new System.Drawing.Point(1, 59);
            this.keepAliveRow.Name = "keepAliveRow";
            this.keepAliveRow.Size = new System.Drawing.Size(502, 58);
            this.keepAliveRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.keepAliveRow.TabIndex = 1;
            //
            // keepAliveLabel
            //
            this.keepAliveLabel.Location = new System.Drawing.Point(16, 12);
            this.keepAliveLabel.Name = "keepAliveLabel";
            this.keepAliveLabel.Size = new System.Drawing.Size(240, 19);
            this.keepAliveLabel.SizePx = 13.5F;
            this.keepAliveLabel.TabIndex = 0;
            this.keepAliveLabel.Text = "Keepalive interval";
            //
            // keepAliveHint
            //
            this.keepAliveHint.Location = new System.Drawing.Point(16, 31);
            this.keepAliveHint.Name = "keepAliveHint";
            this.keepAliveHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.keepAliveHint.Size = new System.Drawing.Size(244, 17);
            this.keepAliveHint.SizePx = 12F;
            this.keepAliveHint.TabIndex = 1;
            this.keepAliveHint.Text = "Idle seconds between keepalives; 0 is off";
            //
            // keepAliveBox
            //
            this.keepAliveBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.keepAliveBox.Location = new System.Drawing.Point(268, 13);
            this.keepAliveBox.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            this.keepAliveBox.Name = "keepAliveBox";
            this.keepAliveBox.Size = new System.Drawing.Size(218, 32);
            this.keepAliveBox.TabIndex = 2;
            this.keepAliveBox.ThousandSeparator = "";
            this.keepAliveBox.Value = new decimal(new int[] { 30, 0, 0, 0 });
            //
            // passiveRow
            //
            this.passiveRow.Controls.Add(this.passiveLabel);
            this.passiveRow.Controls.Add(this.passiveHint);
            this.passiveRow.Controls.Add(this.passiveSwitch);
            this.passiveRow.CornerRadius = 0;
            this.passiveRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.passiveRow.Location = new System.Drawing.Point(1, 117);
            this.passiveRow.Name = "passiveRow";
            this.passiveRow.Size = new System.Drawing.Size(502, 58);
            this.passiveRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.passiveRow.TabIndex = 2;
            //
            // passiveLabel
            //
            this.passiveLabel.Location = new System.Drawing.Point(16, 12);
            this.passiveLabel.Name = "passiveLabel";
            this.passiveLabel.Size = new System.Drawing.Size(300, 19);
            this.passiveLabel.SizePx = 13.5F;
            this.passiveLabel.TabIndex = 0;
            this.passiveLabel.Text = "Passive FTP";
            //
            // passiveHint
            //
            this.passiveHint.Location = new System.Drawing.Point(16, 31);
            this.passiveHint.Name = "passiveHint";
            this.passiveHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.passiveHint.Size = new System.Drawing.Size(340, 17);
            this.passiveHint.SizePx = 12F;
            this.passiveHint.TabIndex = 1;
            this.passiveHint.Text = "The client opens FTP data connections, which firewalls allow";
            //
            // passiveSwitch
            //
            this.passiveSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.passiveSwitch.Location = new System.Drawing.Point(446, 19);
            this.passiveSwitch.Name = "passiveSwitch";
            this.passiveSwitch.Size = new System.Drawing.Size(40, 20);
            this.passiveSwitch.TabIndex = 2;
            //
            // editorsPage
            //
            this.editorsPage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editorsPage.Controls.Add(this.editorsGroup);
            this.editorsPage.CornerRadius = 0;
            this.editorsPage.Location = new System.Drawing.Point(8, 52);
            this.editorsPage.Name = "editorsPage";
            this.editorsPage.Size = new System.Drawing.Size(504, 460);
            this.editorsPage.Surface = FtpClient.Controls.SurfaceKind.None;
            this.editorsPage.TabIndex = 3;
            this.editorsPage.Visible = false;
            //
            // editorsGroup
            //
            this.editorsGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.editorsGroup.Controls.Add(this.gitRow);
            this.editorsGroup.Controls.Add(this.editorRow);
            this.editorsGroup.CornerRadius = 7;
            this.editorsGroup.Location = new System.Drawing.Point(0, 0);
            this.editorsGroup.Name = "editorsGroup";
            this.editorsGroup.Padding = new System.Windows.Forms.Padding(1);
            this.editorsGroup.Size = new System.Drawing.Size(504, 118);
            this.editorsGroup.TabIndex = 0;
            //
            // editorRow
            //
            this.editorRow.BottomDivider = true;
            this.editorRow.Controls.Add(this.editorLabel);
            this.editorRow.Controls.Add(this.editorHint);
            this.editorRow.Controls.Add(this.editorBox);
            this.editorRow.CornerRadius = 0;
            this.editorRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.editorRow.Location = new System.Drawing.Point(1, 1);
            this.editorRow.Name = "editorRow";
            this.editorRow.Size = new System.Drawing.Size(502, 58);
            this.editorRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.editorRow.TabIndex = 0;
            //
            // editorLabel
            //
            this.editorLabel.Location = new System.Drawing.Point(16, 12);
            this.editorLabel.Name = "editorLabel";
            this.editorLabel.Size = new System.Drawing.Size(240, 19);
            this.editorLabel.SizePx = 13.5F;
            this.editorLabel.TabIndex = 0;
            this.editorLabel.Text = "Editor";
            //
            // editorHint
            //
            this.editorHint.Location = new System.Drawing.Point(16, 31);
            this.editorHint.Name = "editorHint";
            this.editorHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.editorHint.Size = new System.Drawing.Size(244, 17);
            this.editorHint.SizePx = 12F;
            this.editorHint.TabIndex = 1;
            this.editorHint.Text = "Opens remote files; empty uses Windows";
            //
            // editorBox
            //
            this.editorBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.editorBox.Location = new System.Drawing.Point(268, 13);
            this.editorBox.Name = "editorBox";
            this.editorBox.PlaceholderText = "default app";
            this.editorBox.Size = new System.Drawing.Size(218, 32);
            this.editorBox.TabIndex = 2;
            //
            // gitRow
            //
            this.gitRow.Controls.Add(this.gitLabel);
            this.gitRow.Controls.Add(this.gitHint);
            this.gitRow.Controls.Add(this.gitPathBox);
            this.gitRow.CornerRadius = 0;
            this.gitRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.gitRow.Location = new System.Drawing.Point(1, 59);
            this.gitRow.Name = "gitRow";
            this.gitRow.Size = new System.Drawing.Size(502, 58);
            this.gitRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.gitRow.TabIndex = 1;
            //
            // gitLabel
            //
            this.gitLabel.Location = new System.Drawing.Point(16, 12);
            this.gitLabel.Name = "gitLabel";
            this.gitLabel.Size = new System.Drawing.Size(240, 19);
            this.gitLabel.SizePx = 13.5F;
            this.gitLabel.TabIndex = 0;
            this.gitLabel.Text = "git.exe";
            //
            // gitHint
            //
            this.gitHint.Location = new System.Drawing.Point(16, 31);
            this.gitHint.Name = "gitHint";
            this.gitHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.gitHint.Size = new System.Drawing.Size(244, 17);
            this.gitHint.SizePx = 12F;
            this.gitHint.TabIndex = 1;
            this.gitHint.Text = "For paired folders; empty uses PATH";
            //
            // gitPathBox
            //
            this.gitPathBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gitPathBox.Location = new System.Drawing.Point(268, 13);
            this.gitPathBox.Name = "gitPathBox";
            this.gitPathBox.PlaceholderText = "found on PATH";
            this.gitPathBox.Size = new System.Drawing.Size(218, 32);
            this.gitPathBox.TabIndex = 2;
            //
            // appearancePage
            //
            this.appearancePage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.appearancePage.Controls.Add(this.appearanceGroup);
            this.appearancePage.CornerRadius = 0;
            this.appearancePage.Location = new System.Drawing.Point(8, 52);
            this.appearancePage.Name = "appearancePage";
            this.appearancePage.Size = new System.Drawing.Size(504, 460);
            this.appearancePage.Surface = FtpClient.Controls.SurfaceKind.None;
            this.appearancePage.TabIndex = 4;
            this.appearancePage.Visible = false;
            //
            // appearanceGroup
            //
            this.appearanceGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.appearanceGroup.Controls.Add(this.groupingRow);
            this.appearanceGroup.Controls.Add(this.hiddenRow);
            this.appearanceGroup.Controls.Add(this.rowHeightRow);
            this.appearanceGroup.Controls.Add(this.themeRow);
            this.appearanceGroup.CornerRadius = 7;
            this.appearanceGroup.Location = new System.Drawing.Point(0, 0);
            this.appearanceGroup.Name = "appearanceGroup";
            this.appearanceGroup.Padding = new System.Windows.Forms.Padding(1);
            this.appearanceGroup.Size = new System.Drawing.Size(504, 234);
            this.appearanceGroup.TabIndex = 0;
            //
            // themeRow
            //
            this.themeRow.BottomDivider = true;
            this.themeRow.Controls.Add(this.themeLabel);
            this.themeRow.Controls.Add(this.themeHint);
            this.themeRow.Controls.Add(this.themeToggle);
            this.themeRow.CornerRadius = 0;
            this.themeRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.themeRow.Location = new System.Drawing.Point(1, 1);
            this.themeRow.Name = "themeRow";
            this.themeRow.Size = new System.Drawing.Size(502, 58);
            this.themeRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.themeRow.TabIndex = 0;
            //
            // themeLabel
            //
            this.themeLabel.Location = new System.Drawing.Point(16, 12);
            this.themeLabel.Name = "themeLabel";
            this.themeLabel.Size = new System.Drawing.Size(200, 19);
            this.themeLabel.SizePx = 13.5F;
            this.themeLabel.TabIndex = 0;
            this.themeLabel.Text = "Theme";
            //
            // themeHint
            //
            this.themeHint.Location = new System.Drawing.Point(16, 31);
            this.themeHint.Name = "themeHint";
            this.themeHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.themeHint.Size = new System.Drawing.Size(240, 17);
            this.themeHint.SizePx = 12F;
            this.themeHint.TabIndex = 1;
            this.themeHint.Text = "Applies to every window straight away";
            //
            // themeToggle
            //
            this.themeToggle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.themeToggle.Items.Add("Dark");
            this.themeToggle.Items.Add("Light");
            this.themeToggle.Location = new System.Drawing.Point(376, 17);
            this.themeToggle.Name = "themeToggle";
            this.themeToggle.SegmentPadding = 16;
            this.themeToggle.Size = new System.Drawing.Size(110, 26);
            this.themeToggle.TabIndex = 2;
            this.themeToggle.TextSizePx = 13F;
            this.themeToggle.SelectedIndexChanged += new System.EventHandler(this.themeToggle_SelectedIndexChanged);
            //
            // rowHeightRow
            //
            this.rowHeightRow.BottomDivider = true;
            this.rowHeightRow.Controls.Add(this.rowHeightLabel);
            this.rowHeightRow.Controls.Add(this.rowHeightHint);
            this.rowHeightRow.Controls.Add(this.rowHeightBox);
            this.rowHeightRow.CornerRadius = 0;
            this.rowHeightRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.rowHeightRow.Location = new System.Drawing.Point(1, 59);
            this.rowHeightRow.Name = "rowHeightRow";
            this.rowHeightRow.Size = new System.Drawing.Size(502, 58);
            this.rowHeightRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.rowHeightRow.TabIndex = 1;
            //
            // rowHeightLabel
            //
            this.rowHeightLabel.Location = new System.Drawing.Point(16, 12);
            this.rowHeightLabel.Name = "rowHeightLabel";
            this.rowHeightLabel.Size = new System.Drawing.Size(240, 19);
            this.rowHeightLabel.SizePx = 13.5F;
            this.rowHeightLabel.TabIndex = 0;
            this.rowHeightLabel.Text = "Row height";
            //
            // rowHeightHint
            //
            this.rowHeightHint.Location = new System.Drawing.Point(16, 31);
            this.rowHeightHint.Name = "rowHeightHint";
            this.rowHeightHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.rowHeightHint.Size = new System.Drawing.Size(240, 17);
            this.rowHeightHint.SizePx = 12F;
            this.rowHeightHint.TabIndex = 1;
            this.rowHeightHint.Text = "File list density, 26 to 38 pixels";
            //
            // rowHeightBox
            //
            this.rowHeightBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rowHeightBox.Location = new System.Drawing.Point(268, 13);
            this.rowHeightBox.Maximum = new decimal(new int[] { 38, 0, 0, 0 });
            this.rowHeightBox.Minimum = new decimal(new int[] { 26, 0, 0, 0 });
            this.rowHeightBox.Name = "rowHeightBox";
            this.rowHeightBox.Size = new System.Drawing.Size(218, 32);
            this.rowHeightBox.TabIndex = 2;
            this.rowHeightBox.ThousandSeparator = "";
            this.rowHeightBox.Value = new decimal(new int[] { 32, 0, 0, 0 });
            //
            // hiddenRow
            //
            this.hiddenRow.BottomDivider = true;
            this.hiddenRow.Controls.Add(this.hiddenLabel);
            this.hiddenRow.Controls.Add(this.hiddenHint);
            this.hiddenRow.Controls.Add(this.hiddenSwitch);
            this.hiddenRow.CornerRadius = 0;
            this.hiddenRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.hiddenRow.Location = new System.Drawing.Point(1, 117);
            this.hiddenRow.Name = "hiddenRow";
            this.hiddenRow.Size = new System.Drawing.Size(502, 58);
            this.hiddenRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.hiddenRow.TabIndex = 2;
            //
            // hiddenLabel
            //
            this.hiddenLabel.Location = new System.Drawing.Point(16, 12);
            this.hiddenLabel.Name = "hiddenLabel";
            this.hiddenLabel.Size = new System.Drawing.Size(300, 19);
            this.hiddenLabel.SizePx = 13.5F;
            this.hiddenLabel.TabIndex = 0;
            this.hiddenLabel.Text = "Show hidden files";
            //
            // hiddenHint
            //
            this.hiddenHint.Location = new System.Drawing.Point(16, 31);
            this.hiddenHint.Name = "hiddenHint";
            this.hiddenHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.hiddenHint.Size = new System.Drawing.Size(300, 17);
            this.hiddenHint.SizePx = 12F;
            this.hiddenHint.TabIndex = 1;
            this.hiddenHint.Text = "List dotfiles such as .env and .htaccess";
            //
            // hiddenSwitch
            //
            this.hiddenSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.hiddenSwitch.Location = new System.Drawing.Point(446, 19);
            this.hiddenSwitch.Name = "hiddenSwitch";
            this.hiddenSwitch.Size = new System.Drawing.Size(40, 20);
            this.hiddenSwitch.TabIndex = 2;
            //
            // groupingRow
            //
            this.groupingRow.Controls.Add(this.groupingLabel);
            this.groupingRow.Controls.Add(this.groupingHint);
            this.groupingRow.Controls.Add(this.groupingBox);
            this.groupingRow.CornerRadius = 0;
            this.groupingRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupingRow.Location = new System.Drawing.Point(1, 175);
            this.groupingRow.Name = "groupingRow";
            this.groupingRow.Size = new System.Drawing.Size(502, 58);
            this.groupingRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.groupingRow.TabIndex = 3;
            //
            // groupingLabel
            //
            this.groupingLabel.Location = new System.Drawing.Point(16, 12);
            this.groupingLabel.Name = "groupingLabel";
            this.groupingLabel.Size = new System.Drawing.Size(240, 19);
            this.groupingLabel.SizePx = 13.5F;
            this.groupingLabel.TabIndex = 0;
            this.groupingLabel.Text = "Queue grouping";
            //
            // groupingHint
            //
            this.groupingHint.Location = new System.Drawing.Point(16, 31);
            this.groupingHint.Name = "groupingHint";
            this.groupingHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.groupingHint.Size = new System.Drawing.Size(244, 17);
            this.groupingHint.SizePx = 12F;
            this.groupingHint.TabIndex = 1;
            this.groupingHint.Text = "How the transfer queue groups its rows";
            //
            // groupingBox
            //
            this.groupingBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupingBox.Items.AddRange(new object[] {
            "Flat",
            "By site",
            "By direction"});
            this.groupingBox.Location = new System.Drawing.Point(268, 13);
            this.groupingBox.Name = "groupingBox";
            this.groupingBox.Size = new System.Drawing.Size(218, 32);
            this.groupingBox.TabIndex = 2;
            //
            // saveButton
            //
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.saveButton.CornerRadius = 5;
            this.saveButton.Location = new System.Drawing.Point(338, 535);
            this.saveButton.Name = "saveButton";
            this.saveButton.PaddingX = 20;
            this.saveButton.Size = new System.Drawing.Size(68, 34);
            this.saveButton.TabIndex = 5;
            this.saveButton.Text = "Save";
            this.saveButton.TextSizePx = 13.5F;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(414, 535);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(98, 34);
            this.cancelButton.TabIndex = 6;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // SettingsDialog
            //
            this.ClientSize = new System.Drawing.Size(720, 620);
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.navRail);
            this.MinimumSize = new System.Drawing.Size(640, 560);
            this.Name = "SettingsDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.contentPanel.ResumeLayout(false);
            this.transfersPage.ResumeLayout(false);
            this.transfersGroup.ResumeLayout(false);
            this.preserveRow.ResumeLayout(false);
            this.directoryRow.ResumeLayout(false);
            this.resumeRow.ResumeLayout(false);
            this.speedRow.ResumeLayout(false);
            this.modeRow.ResumeLayout(false);
            this.concurrentRow.ResumeLayout(false);
            this.connectionPage.ResumeLayout(false);
            this.connectionGroup.ResumeLayout(false);
            this.passiveRow.ResumeLayout(false);
            this.keepAliveRow.ResumeLayout(false);
            this.timeoutRow.ResumeLayout(false);
            this.editorsPage.ResumeLayout(false);
            this.editorsGroup.ResumeLayout(false);
            this.gitRow.ResumeLayout(false);
            this.editorRow.ResumeLayout(false);
            this.appearancePage.ResumeLayout(false);
            this.appearanceGroup.ResumeLayout(false);
            this.groupingRow.ResumeLayout(false);
            this.hiddenRow.ResumeLayout(false);
            this.rowHeightRow.ResumeLayout(false);
            this.themeRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.NavRailControl navRail;
        private FtpClient.Controls.SurfacePanel contentPanel;
        private FtpClient.Controls.TextLabel pageTitleLabel;
        private FtpClient.Controls.SurfacePanel transfersPage;
        private FtpClient.Controls.SurfacePanel transfersGroup;
        private FtpClient.Controls.SurfacePanel concurrentRow;
        private FtpClient.Controls.TextLabel concurrentLabel;
        private FtpClient.Controls.TextLabel concurrentHint;
        private ModernWinForms.ModernNumericUpDown concurrentBox;
        private FtpClient.Controls.SurfacePanel modeRow;
        private FtpClient.Controls.TextLabel modeLabel;
        private FtpClient.Controls.TextLabel modeHint;
        private ModernWinForms.ModernComboBox modeBox;
        private FtpClient.Controls.SurfacePanel speedRow;
        private FtpClient.Controls.TextLabel speedLabel;
        private FtpClient.Controls.TextLabel speedHint;
        private ModernWinForms.ModernNumericUpDown speedBox;
        private FtpClient.Controls.TextLabel speedUnitLabel;
        private FtpClient.Controls.ToggleSwitchControl speedSwitch;
        private FtpClient.Controls.SurfacePanel resumeRow;
        private FtpClient.Controls.TextLabel resumeLabel;
        private FtpClient.Controls.TextLabel resumeHint;
        private FtpClient.Controls.ToggleSwitchControl resumeSwitch;
        private FtpClient.Controls.SurfacePanel directoryRow;
        private FtpClient.Controls.TextLabel directoryLabel;
        private FtpClient.Controls.TextLabel directoryHint;
        private ModernWinForms.ModernTextBox directoryBox;
        private FtpClient.Controls.SurfacePanel preserveRow;
        private FtpClient.Controls.TextLabel preserveLabel;
        private FtpClient.Controls.TextLabel preserveHint;
        private FtpClient.Controls.ToggleSwitchControl preserveSwitch;
        private FtpClient.Controls.SurfacePanel connectionPage;
        private FtpClient.Controls.SurfacePanel connectionGroup;
        private FtpClient.Controls.SurfacePanel timeoutRow;
        private FtpClient.Controls.TextLabel timeoutLabel;
        private FtpClient.Controls.TextLabel timeoutHint;
        private ModernWinForms.ModernNumericUpDown timeoutBox;
        private FtpClient.Controls.SurfacePanel keepAliveRow;
        private FtpClient.Controls.TextLabel keepAliveLabel;
        private FtpClient.Controls.TextLabel keepAliveHint;
        private ModernWinForms.ModernNumericUpDown keepAliveBox;
        private FtpClient.Controls.SurfacePanel passiveRow;
        private FtpClient.Controls.TextLabel passiveLabel;
        private FtpClient.Controls.TextLabel passiveHint;
        private FtpClient.Controls.ToggleSwitchControl passiveSwitch;
        private FtpClient.Controls.SurfacePanel editorsPage;
        private FtpClient.Controls.SurfacePanel editorsGroup;
        private FtpClient.Controls.SurfacePanel editorRow;
        private FtpClient.Controls.TextLabel editorLabel;
        private FtpClient.Controls.TextLabel editorHint;
        private ModernWinForms.ModernTextBox editorBox;
        private FtpClient.Controls.SurfacePanel gitRow;
        private FtpClient.Controls.TextLabel gitLabel;
        private FtpClient.Controls.TextLabel gitHint;
        private ModernWinForms.ModernTextBox gitPathBox;
        private FtpClient.Controls.SurfacePanel appearancePage;
        private FtpClient.Controls.SurfacePanel appearanceGroup;
        private FtpClient.Controls.SurfacePanel themeRow;
        private FtpClient.Controls.TextLabel themeLabel;
        private FtpClient.Controls.TextLabel themeHint;
        private FtpClient.Controls.SegmentedControl themeToggle;
        private FtpClient.Controls.SurfacePanel rowHeightRow;
        private FtpClient.Controls.TextLabel rowHeightLabel;
        private FtpClient.Controls.TextLabel rowHeightHint;
        private ModernWinForms.ModernNumericUpDown rowHeightBox;
        private FtpClient.Controls.SurfacePanel hiddenRow;
        private FtpClient.Controls.TextLabel hiddenLabel;
        private FtpClient.Controls.TextLabel hiddenHint;
        private FtpClient.Controls.ToggleSwitchControl hiddenSwitch;
        private FtpClient.Controls.SurfacePanel groupingRow;
        private FtpClient.Controls.TextLabel groupingLabel;
        private FtpClient.Controls.TextLabel groupingHint;
        private ModernWinForms.ModernComboBox groupingBox;
        private FtpClient.Controls.CommandButton saveButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
