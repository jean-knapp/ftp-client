namespace FtpClient.Forms
{
    partial class SiteManagerDialog
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
            this.rail = new FtpClient.Controls.SurfacePanel();
            this.siteTree = new FtpClient.Controls.SiteTreeListControl();
            this.railToolbar = new FtpClient.Controls.SurfacePanel();
            this.newSiteButton = new FtpClient.Controls.CommandButton();
            this.folderButton = new FtpClient.Controls.CommandButton();
            this.detail = new FtpClient.Controls.SurfacePanel();
            this.nameLabel = new FtpClient.Controls.TextLabel();
            this.connectedChip = new FtpClient.Controls.Chip();
            this.detailTabs = new FtpClient.Controls.CardTabStrip();
            this.generalCard = new FtpClient.Controls.SurfacePanel();
            this.directoryRow = new FtpClient.Controls.SurfacePanel();
            this.directoryLabel = new FtpClient.Controls.TextLabel();
            this.directoryBox = new ModernWinForms.ModernTextBox();
            this.keyRow = new FtpClient.Controls.SurfacePanel();
            this.keyLabel = new FtpClient.Controls.TextLabel();
            this.keyBox = new ModernWinForms.ModernTextBox();
            this.browseKeyButton = new ModernWinForms.ModernTextBoxButton();
            this.userRow = new FtpClient.Controls.SurfacePanel();
            this.userLabel = new FtpClient.Controls.TextLabel();
            this.userBox = new ModernWinForms.ModernTextBox();
            this.portRow = new FtpClient.Controls.SurfacePanel();
            this.portLabel = new FtpClient.Controls.TextLabel();
            this.portBox = new ModernWinForms.ModernTextBox();
            this.hostRow = new FtpClient.Controls.SurfacePanel();
            this.hostLabel = new FtpClient.Controls.TextLabel();
            this.hostBox = new ModernWinForms.ModernTextBox();
            this.protocolRow = new FtpClient.Controls.SurfacePanel();
            this.protocolLabel = new FtpClient.Controls.TextLabel();
            this.protocolBox = new ModernWinForms.ModernComboBox();
            this.advancedCard = new FtpClient.Controls.SurfacePanel();
            this.hostKeyRow = new FtpClient.Controls.SurfacePanel();
            this.hostKeyLabel = new FtpClient.Controls.TextLabel();
            this.hostKeyValue = new FtpClient.Controls.TextLabel();
            this.forgetKeyButton = new FtpClient.Controls.CommandButton();
            this.passwordRow = new FtpClient.Controls.SurfacePanel();
            this.passwordLabel = new FtpClient.Controls.TextLabel();
            this.passwordBox = new ModernWinForms.ModernTextBox();
            this.authRow = new FtpClient.Controls.SurfacePanel();
            this.authLabel = new FtpClient.Controls.TextLabel();
            this.authBox = new ModernWinForms.ModernComboBox();
            this.groupRow = new FtpClient.Controls.SurfacePanel();
            this.groupLabel = new FtpClient.Controls.TextLabel();
            this.groupBox = new ModernWinForms.ModernTextBox();
            this.nameRow = new FtpClient.Controls.SurfacePanel();
            this.nameFieldLabel = new FtpClient.Controls.TextLabel();
            this.nameBox = new ModernWinForms.ModernTextBox();
            this.transferCard = new FtpClient.Controls.SurfacePanel();
            this.bookmarksRow = new FtpClient.Controls.SurfacePanel();
            this.bookmarksLabel = new FtpClient.Controls.TextLabel();
            this.bookmarksValue = new FtpClient.Controls.TextLabel();
            this.clearBookmarksButton = new FtpClient.Controls.CommandButton();
            this.pairingsRow = new FtpClient.Controls.SurfacePanel();
            this.pairingsLabel = new FtpClient.Controls.TextLabel();
            this.pairingsValue = new FtpClient.Controls.TextLabel();
            this.forgetPairingsButton = new FtpClient.Controls.CommandButton();
            this.emptyLabel = new FtpClient.Controls.TextLabel();
            this.deleteButton = new FtpClient.Controls.CommandButton();
            this.connectButton = new FtpClient.Controls.CommandButton();
            this.saveButton = new FtpClient.Controls.CommandButton();
            this.closeButton = new FtpClient.Controls.CommandButton();
            this.rail.SuspendLayout();
            this.railToolbar.SuspendLayout();
            this.detail.SuspendLayout();
            this.generalCard.SuspendLayout();
            this.directoryRow.SuspendLayout();
            this.keyRow.SuspendLayout();
            this.userRow.SuspendLayout();
            this.portRow.SuspendLayout();
            this.hostRow.SuspendLayout();
            this.protocolRow.SuspendLayout();
            this.advancedCard.SuspendLayout();
            this.hostKeyRow.SuspendLayout();
            this.passwordRow.SuspendLayout();
            this.authRow.SuspendLayout();
            this.groupRow.SuspendLayout();
            this.nameRow.SuspendLayout();
            this.transferCard.SuspendLayout();
            this.bookmarksRow.SuspendLayout();
            this.pairingsRow.SuspendLayout();
            this.SuspendLayout();
            //
            // rail
            //
            this.rail.Controls.Add(this.siteTree);
            this.rail.Controls.Add(this.railToolbar);
            this.rail.CornerRadius = 0;
            this.rail.Dock = System.Windows.Forms.DockStyle.Left;
            this.rail.Location = new System.Drawing.Point(0, 32);
            this.rail.Name = "rail";
            this.rail.RightDivider = true;
            this.rail.Size = new System.Drawing.Size(260, 620);
            this.rail.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.rail.TabIndex = 0;
            //
            // siteTree
            //
            this.siteTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.siteTree.Location = new System.Drawing.Point(0, 38);
            this.siteTree.Name = "siteTree";
            this.siteTree.Size = new System.Drawing.Size(259, 582);
            this.siteTree.TabIndex = 1;
            this.siteTree.SelectionChanged += new System.EventHandler(this.siteTree_SelectionChanged);
            //
            // railToolbar
            //
            this.railToolbar.Controls.Add(this.newSiteButton);
            this.railToolbar.Controls.Add(this.folderButton);
            this.railToolbar.CornerRadius = 0;
            this.railToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.railToolbar.Location = new System.Drawing.Point(0, 0);
            this.railToolbar.Name = "railToolbar";
            this.railToolbar.Size = new System.Drawing.Size(259, 38);
            this.railToolbar.Surface = FtpClient.Controls.SurfaceKind.None;
            this.railToolbar.TabIndex = 0;
            //
            // newSiteButton
            //
            this.newSiteButton.IconSize = 13;
            this.newSiteButton.IconSvg = FtpClient.Controls.Icons.Plus;
            this.newSiteButton.Location = new System.Drawing.Point(8, 6);
            this.newSiteButton.Name = "newSiteButton";
            this.newSiteButton.PaddingX = 8;
            this.newSiteButton.Size = new System.Drawing.Size(90, 28);
            this.newSiteButton.TabIndex = 0;
            this.newSiteButton.Text = "New site";
            this.newSiteButton.Click += new System.EventHandler(this.newSiteButton_Click);
            //
            // folderButton
            //
            this.folderButton.IconSize = 13;
            this.folderButton.IconSvg = FtpClient.Controls.Icons.Folder;
            this.folderButton.Location = new System.Drawing.Point(102, 6);
            this.folderButton.Name = "folderButton";
            this.folderButton.PaddingX = 8;
            this.folderButton.Size = new System.Drawing.Size(76, 28);
            this.folderButton.TabIndex = 1;
            this.folderButton.Text = "Folder";
            this.folderButton.Click += new System.EventHandler(this.folderButton_Click);
            //
            // detail
            //
            this.detail.Controls.Add(this.nameLabel);
            this.detail.Controls.Add(this.connectedChip);
            this.detail.Controls.Add(this.detailTabs);
            this.detail.Controls.Add(this.generalCard);
            this.detail.Controls.Add(this.advancedCard);
            this.detail.Controls.Add(this.transferCard);
            this.detail.Controls.Add(this.emptyLabel);
            this.detail.Controls.Add(this.deleteButton);
            this.detail.Controls.Add(this.connectButton);
            this.detail.Controls.Add(this.saveButton);
            this.detail.Controls.Add(this.closeButton);
            this.detail.CornerRadius = 0;
            this.detail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.detail.Location = new System.Drawing.Point(260, 32);
            this.detail.Name = "detail";
            this.detail.Size = new System.Drawing.Size(600, 620);
            this.detail.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.detail.TabIndex = 1;
            //
            // nameLabel
            //
            this.nameLabel.Location = new System.Drawing.Point(16, 12);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Semibold = true;
            this.nameLabel.Size = new System.Drawing.Size(200, 32);
            this.nameLabel.SizePx = 20F;
            this.nameLabel.TabIndex = 0;
            //
            // connectedChip
            //
            this.connectedChip.CornerRadius = 4;
            this.connectedChip.Location = new System.Drawing.Point(226, 17);
            this.connectedChip.Name = "connectedChip";
            this.connectedChip.PaddingX = 8;
            this.connectedChip.Semibold = false;
            this.connectedChip.Size = new System.Drawing.Size(72, 22);
            this.connectedChip.Style = FtpClient.Controls.ChipStyle.Up;
            this.connectedChip.TabIndex = 1;
            this.connectedChip.Text = "Connected";
            this.connectedChip.TextSizePx = 12F;
            this.connectedChip.Visible = false;
            //
            // detailTabs
            //
            this.detailTabs.AccentSelectedBadge = false;
            this.detailTabs.Location = new System.Drawing.Point(16, 52);
            this.detailTabs.Name = "detailTabs";
            this.detailTabs.Size = new System.Drawing.Size(320, 36);
            this.detailTabs.TabHeight = 32;
            this.detailTabs.TabIndex = 2;
            this.detailTabs.TabPaddingX = 10;
            this.detailTabs.Tabs.Add(new FtpClient.Controls.CardTab("General", null));
            this.detailTabs.Tabs.Add(new FtpClient.Controls.CardTab("Advanced", null));
            this.detailTabs.Tabs.Add(new FtpClient.Controls.CardTab("Transfer", null));
            this.detailTabs.Underline = true;
            this.detailTabs.SelectedIndexChanged += new System.EventHandler(this.detailTabs_SelectedIndexChanged);
            //
            // generalCard
            //
            this.generalCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.generalCard.Controls.Add(this.directoryRow);
            this.generalCard.Controls.Add(this.keyRow);
            this.generalCard.Controls.Add(this.userRow);
            this.generalCard.Controls.Add(this.portRow);
            this.generalCard.Controls.Add(this.hostRow);
            this.generalCard.Controls.Add(this.protocolRow);
            this.generalCard.Controls.Add(this.nameRow);
            this.generalCard.CornerRadius = 7;
            this.generalCard.Location = new System.Drawing.Point(16, 96);
            this.generalCard.Name = "generalCard";
            this.generalCard.Padding = new System.Windows.Forms.Padding(1);
            this.generalCard.Size = new System.Drawing.Size(568, 366);
            this.generalCard.TabIndex = 3;
            //
            // protocolRow
            //
            this.protocolRow.BottomDivider = true;
            this.protocolRow.Controls.Add(this.protocolLabel);
            this.protocolRow.Controls.Add(this.protocolBox);
            this.protocolRow.CornerRadius = 0;
            this.protocolRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.protocolRow.Location = new System.Drawing.Point(1, 1);
            this.protocolRow.Name = "protocolRow";
            this.protocolRow.Size = new System.Drawing.Size(566, 52);
            this.protocolRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.protocolRow.TabIndex = 0;
            //
            // protocolLabel
            //
            this.protocolLabel.Location = new System.Drawing.Point(16, 0);
            this.protocolLabel.Name = "protocolLabel";
            this.protocolLabel.Size = new System.Drawing.Size(200, 52);
            this.protocolLabel.SizePx = 13.5F;
            this.protocolLabel.TabIndex = 0;
            this.protocolLabel.Text = "Protocol";
            //
            // protocolBox
            //
            this.protocolBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.protocolBox.Items.AddRange(new object[] {
            "SFTP — SSH File Transfer",
            "FTP — File Transfer Protocol",
            "FTPS — FTP over TLS",
            "Local or network folder"});
            this.protocolBox.Location = new System.Drawing.Point(270, 10);
            this.protocolBox.Name = "protocolBox";
            this.protocolBox.Size = new System.Drawing.Size(280, 32);
            this.protocolBox.TabIndex = 1;
            this.protocolBox.SelectedIndexChanged += new System.EventHandler<ModernWinForms.SelectedIndexChangedEventArgs>(this.protocolBox_SelectedIndexChanged);
            //
            // hostRow
            //
            this.hostRow.BottomDivider = true;
            this.hostRow.Controls.Add(this.hostLabel);
            this.hostRow.Controls.Add(this.hostBox);
            this.hostRow.CornerRadius = 0;
            this.hostRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.hostRow.Location = new System.Drawing.Point(1, 53);
            this.hostRow.Name = "hostRow";
            this.hostRow.Size = new System.Drawing.Size(566, 52);
            this.hostRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.hostRow.TabIndex = 1;
            //
            // hostLabel
            //
            this.hostLabel.Location = new System.Drawing.Point(16, 0);
            this.hostLabel.Name = "hostLabel";
            this.hostLabel.Size = new System.Drawing.Size(200, 52);
            this.hostLabel.SizePx = 13.5F;
            this.hostLabel.TabIndex = 0;
            this.hostLabel.Text = "Host";
            //
            // hostBox
            //
            this.hostBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.hostBox.Location = new System.Drawing.Point(270, 10);
            this.hostBox.Name = "hostBox";
            this.hostBox.Size = new System.Drawing.Size(280, 32);
            this.hostBox.TabIndex = 1;
            this.hostBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // portRow
            //
            this.portRow.BottomDivider = true;
            this.portRow.Controls.Add(this.portLabel);
            this.portRow.Controls.Add(this.portBox);
            this.portRow.CornerRadius = 0;
            this.portRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.portRow.Location = new System.Drawing.Point(1, 105);
            this.portRow.Name = "portRow";
            this.portRow.Size = new System.Drawing.Size(566, 52);
            this.portRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.portRow.TabIndex = 2;
            //
            // portLabel
            //
            this.portLabel.Location = new System.Drawing.Point(16, 0);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(200, 52);
            this.portLabel.SizePx = 13.5F;
            this.portLabel.TabIndex = 0;
            this.portLabel.Text = "Port";
            //
            // portBox
            //
            this.portBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.portBox.Location = new System.Drawing.Point(270, 10);
            this.portBox.MaxLength = 5;
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(280, 32);
            this.portBox.TabIndex = 1;
            this.portBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // userRow
            //
            this.userRow.BottomDivider = true;
            this.userRow.Controls.Add(this.userLabel);
            this.userRow.Controls.Add(this.userBox);
            this.userRow.CornerRadius = 0;
            this.userRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.userRow.Location = new System.Drawing.Point(1, 157);
            this.userRow.Name = "userRow";
            this.userRow.Size = new System.Drawing.Size(566, 52);
            this.userRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.userRow.TabIndex = 3;
            //
            // userLabel
            //
            this.userLabel.Location = new System.Drawing.Point(16, 0);
            this.userLabel.Name = "userLabel";
            this.userLabel.Size = new System.Drawing.Size(200, 52);
            this.userLabel.SizePx = 13.5F;
            this.userLabel.TabIndex = 0;
            this.userLabel.Text = "User";
            //
            // userBox
            //
            this.userBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.userBox.Location = new System.Drawing.Point(270, 10);
            this.userBox.Name = "userBox";
            this.userBox.Size = new System.Drawing.Size(280, 32);
            this.userBox.TabIndex = 1;
            this.userBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // keyRow
            //
            this.keyRow.BottomDivider = true;
            this.keyRow.Controls.Add(this.keyLabel);
            this.keyRow.Controls.Add(this.keyBox);
            this.keyRow.CornerRadius = 0;
            this.keyRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.keyRow.Location = new System.Drawing.Point(1, 209);
            this.keyRow.Name = "keyRow";
            this.keyRow.Size = new System.Drawing.Size(566, 52);
            this.keyRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.keyRow.TabIndex = 4;
            //
            // keyLabel
            //
            this.keyLabel.Location = new System.Drawing.Point(16, 0);
            this.keyLabel.Name = "keyLabel";
            this.keyLabel.Size = new System.Drawing.Size(200, 52);
            this.keyLabel.SizePx = 13.5F;
            this.keyLabel.TabIndex = 0;
            this.keyLabel.Text = "Key file";
            //
            // keyBox
            //
            this.keyBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.keyBox.Buttons.Add(this.browseKeyButton);
            this.keyBox.Location = new System.Drawing.Point(270, 10);
            this.keyBox.Name = "keyBox";
            this.keyBox.PlaceholderText = "~/.ssh/id_ed25519";
            this.keyBox.Size = new System.Drawing.Size(280, 32);
            this.keyBox.TabIndex = 1;
            this.keyBox.ButtonClick += new System.EventHandler<ModernWinForms.ModernTextBoxButtonEventArgs>(this.keyBox_ButtonClick);
            this.keyBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // browseKeyButton
            //
            this.browseKeyButton.ToolTipText = "Choose a key file…";
            //
            // directoryRow
            //
            this.directoryRow.Controls.Add(this.directoryLabel);
            this.directoryRow.Controls.Add(this.directoryBox);
            this.directoryRow.CornerRadius = 0;
            this.directoryRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.directoryRow.Location = new System.Drawing.Point(1, 261);
            this.directoryRow.Name = "directoryRow";
            this.directoryRow.Size = new System.Drawing.Size(566, 52);
            this.directoryRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.directoryRow.TabIndex = 5;
            //
            // directoryLabel
            //
            this.directoryLabel.Location = new System.Drawing.Point(16, 0);
            this.directoryLabel.Name = "directoryLabel";
            this.directoryLabel.Size = new System.Drawing.Size(200, 52);
            this.directoryLabel.SizePx = 13.5F;
            this.directoryLabel.TabIndex = 0;
            this.directoryLabel.Text = "Remote directory";
            //
            // directoryBox
            //
            this.directoryBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.directoryBox.Location = new System.Drawing.Point(270, 10);
            this.directoryBox.Name = "directoryBox";
            this.directoryBox.PlaceholderText = "home directory";
            this.directoryBox.Size = new System.Drawing.Size(280, 32);
            this.directoryBox.TabIndex = 1;
            this.directoryBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // advancedCard
            //
            this.advancedCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.advancedCard.Controls.Add(this.hostKeyRow);
            this.advancedCard.Controls.Add(this.passwordRow);
            this.advancedCard.Controls.Add(this.authRow);
            this.advancedCard.Controls.Add(this.groupRow);
            this.advancedCard.CornerRadius = 7;
            this.advancedCard.Location = new System.Drawing.Point(16, 96);
            this.advancedCard.Name = "advancedCard";
            this.advancedCard.Padding = new System.Windows.Forms.Padding(1);
            this.advancedCard.Size = new System.Drawing.Size(568, 210);
            this.advancedCard.TabIndex = 4;
            this.advancedCard.Visible = false;
            //
            // nameRow
            //
            this.nameRow.BottomDivider = true;
            this.nameRow.Controls.Add(this.nameFieldLabel);
            this.nameRow.Controls.Add(this.nameBox);
            this.nameRow.CornerRadius = 0;
            this.nameRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.nameRow.Location = new System.Drawing.Point(1, 1);
            this.nameRow.Name = "nameRow";
            this.nameRow.Size = new System.Drawing.Size(566, 52);
            this.nameRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.nameRow.TabIndex = 0;
            //
            // nameFieldLabel
            //
            this.nameFieldLabel.Location = new System.Drawing.Point(16, 0);
            this.nameFieldLabel.Name = "nameFieldLabel";
            this.nameFieldLabel.Size = new System.Drawing.Size(200, 52);
            this.nameFieldLabel.SizePx = 13.5F;
            this.nameFieldLabel.TabIndex = 0;
            this.nameFieldLabel.Text = "Name";
            //
            // nameBox
            //
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(270, 10);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(280, 32);
            this.nameBox.TabIndex = 1;
            this.nameBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // groupRow
            //
            this.groupRow.BottomDivider = true;
            this.groupRow.Controls.Add(this.groupLabel);
            this.groupRow.Controls.Add(this.groupBox);
            this.groupRow.CornerRadius = 0;
            this.groupRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupRow.Location = new System.Drawing.Point(1, 53);
            this.groupRow.Name = "groupRow";
            this.groupRow.Size = new System.Drawing.Size(566, 52);
            this.groupRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.groupRow.TabIndex = 1;
            //
            // groupLabel
            //
            this.groupLabel.Location = new System.Drawing.Point(16, 0);
            this.groupLabel.Name = "groupLabel";
            this.groupLabel.Size = new System.Drawing.Size(200, 52);
            this.groupLabel.SizePx = 13.5F;
            this.groupLabel.TabIndex = 0;
            this.groupLabel.Text = "Folder";
            //
            // groupBox
            //
            this.groupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox.Location = new System.Drawing.Point(270, 10);
            this.groupBox.Name = "groupBox";
            this.groupBox.PlaceholderText = "none";
            this.groupBox.Size = new System.Drawing.Size(280, 32);
            this.groupBox.TabIndex = 1;
            this.groupBox.TextChanged += new System.EventHandler(this.field_TextChanged);
            //
            // authRow
            //
            this.authRow.BottomDivider = true;
            this.authRow.Controls.Add(this.authLabel);
            this.authRow.Controls.Add(this.authBox);
            this.authRow.CornerRadius = 0;
            this.authRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.authRow.Location = new System.Drawing.Point(1, 105);
            this.authRow.Name = "authRow";
            this.authRow.Size = new System.Drawing.Size(566, 52);
            this.authRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.authRow.TabIndex = 2;
            //
            // authLabel
            //
            this.authLabel.Location = new System.Drawing.Point(16, 0);
            this.authLabel.Name = "authLabel";
            this.authLabel.Size = new System.Drawing.Size(200, 52);
            this.authLabel.SizePx = 13.5F;
            this.authLabel.TabIndex = 0;
            this.authLabel.Text = "Sign in with";
            //
            // authBox
            //
            this.authBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.authBox.Items.AddRange(new object[] {
            "Password",
            "Key file",
            "Anonymous"});
            this.authBox.Location = new System.Drawing.Point(270, 10);
            this.authBox.Name = "authBox";
            this.authBox.Size = new System.Drawing.Size(280, 32);
            this.authBox.TabIndex = 1;
            this.authBox.SelectedIndexChanged += new System.EventHandler<ModernWinForms.SelectedIndexChangedEventArgs>(this.authBox_SelectedIndexChanged);
            //
            // passwordRow
            //
            this.passwordRow.BottomDivider = true;
            this.passwordRow.Controls.Add(this.passwordLabel);
            this.passwordRow.Controls.Add(this.passwordBox);
            this.passwordRow.CornerRadius = 0;
            this.passwordRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.passwordRow.Location = new System.Drawing.Point(1, 157);
            this.passwordRow.Name = "passwordRow";
            this.passwordRow.Size = new System.Drawing.Size(566, 52);
            this.passwordRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.passwordRow.TabIndex = 3;
            //
            // passwordLabel
            //
            this.passwordLabel.Location = new System.Drawing.Point(16, 0);
            this.passwordLabel.Name = "passwordLabel";
            this.passwordLabel.Size = new System.Drawing.Size(240, 52);
            this.passwordLabel.SizePx = 13.5F;
            this.passwordLabel.TabIndex = 0;
            this.passwordLabel.Text = "Password or key passphrase";
            //
            // passwordBox
            //
            this.passwordBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.passwordBox.Location = new System.Drawing.Point(270, 10);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.PasswordChar = '●';
            this.passwordBox.PlaceholderText = "asked when connecting";
            this.passwordBox.Size = new System.Drawing.Size(280, 32);
            this.passwordBox.TabIndex = 1;
            this.passwordBox.TextChanged += new System.EventHandler(this.passwordBox_TextChanged);
            //
            // hostKeyRow
            //
            this.hostKeyRow.Controls.Add(this.hostKeyLabel);
            this.hostKeyRow.Controls.Add(this.hostKeyValue);
            this.hostKeyRow.Controls.Add(this.forgetKeyButton);
            this.hostKeyRow.CornerRadius = 0;
            this.hostKeyRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.hostKeyRow.Location = new System.Drawing.Point(1, 209);
            this.hostKeyRow.Name = "hostKeyRow";
            this.hostKeyRow.Size = new System.Drawing.Size(566, 52);
            this.hostKeyRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.hostKeyRow.TabIndex = 4;
            //
            // hostKeyLabel
            //
            this.hostKeyLabel.Location = new System.Drawing.Point(16, 0);
            this.hostKeyLabel.Name = "hostKeyLabel";
            this.hostKeyLabel.Size = new System.Drawing.Size(140, 52);
            this.hostKeyLabel.SizePx = 13.5F;
            this.hostKeyLabel.TabIndex = 0;
            this.hostKeyLabel.Text = "Host key";
            //
            // hostKeyValue
            //
            this.hostKeyValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.hostKeyValue.Location = new System.Drawing.Point(164, 0);
            this.hostKeyValue.Name = "hostKeyValue";
            this.hostKeyValue.Role = FtpClient.Controls.TextRole.Tertiary;
            this.hostKeyValue.Size = new System.Drawing.Size(310, 52);
            this.hostKeyValue.SizePx = 12F;
            this.hostKeyValue.TabIndex = 1;
            this.hostKeyValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // forgetKeyButton
            //
            this.forgetKeyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.forgetKeyButton.Location = new System.Drawing.Point(482, 12);
            this.forgetKeyButton.Name = "forgetKeyButton";
            this.forgetKeyButton.PaddingX = 8;
            this.forgetKeyButton.Size = new System.Drawing.Size(68, 28);
            this.forgetKeyButton.TabIndex = 2;
            this.forgetKeyButton.Text = "Forget";
            this.forgetKeyButton.Click += new System.EventHandler(this.forgetKeyButton_Click);
            //
            // transferCard
            //
            this.transferCard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.transferCard.Controls.Add(this.bookmarksRow);
            this.transferCard.Controls.Add(this.pairingsRow);
            this.transferCard.CornerRadius = 7;
            this.transferCard.Location = new System.Drawing.Point(16, 96);
            this.transferCard.Name = "transferCard";
            this.transferCard.Padding = new System.Windows.Forms.Padding(1);
            this.transferCard.Size = new System.Drawing.Size(568, 106);
            this.transferCard.TabIndex = 5;
            this.transferCard.Visible = false;
            //
            // pairingsRow
            //
            this.pairingsRow.BottomDivider = true;
            this.pairingsRow.Controls.Add(this.pairingsLabel);
            this.pairingsRow.Controls.Add(this.pairingsValue);
            this.pairingsRow.Controls.Add(this.forgetPairingsButton);
            this.pairingsRow.CornerRadius = 0;
            this.pairingsRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.pairingsRow.Location = new System.Drawing.Point(1, 1);
            this.pairingsRow.Name = "pairingsRow";
            this.pairingsRow.Size = new System.Drawing.Size(566, 52);
            this.pairingsRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.pairingsRow.TabIndex = 0;
            //
            // pairingsLabel
            //
            this.pairingsLabel.Location = new System.Drawing.Point(16, 0);
            this.pairingsLabel.Name = "pairingsLabel";
            this.pairingsLabel.Size = new System.Drawing.Size(120, 52);
            this.pairingsLabel.SizePx = 13.5F;
            this.pairingsLabel.TabIndex = 0;
            this.pairingsLabel.Text = "Pairings";
            //
            // pairingsValue
            //
            this.pairingsValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.pairingsValue.Location = new System.Drawing.Point(140, 0);
            this.pairingsValue.Name = "pairingsValue";
            this.pairingsValue.Role = FtpClient.Controls.TextRole.Tertiary;
            this.pairingsValue.Size = new System.Drawing.Size(334, 52);
            this.pairingsValue.SizePx = 12.5F;
            this.pairingsValue.TabIndex = 1;
            this.pairingsValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // forgetPairingsButton
            //
            this.forgetPairingsButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.forgetPairingsButton.Location = new System.Drawing.Point(482, 12);
            this.forgetPairingsButton.Name = "forgetPairingsButton";
            this.forgetPairingsButton.PaddingX = 8;
            this.forgetPairingsButton.Size = new System.Drawing.Size(68, 28);
            this.forgetPairingsButton.TabIndex = 2;
            this.forgetPairingsButton.Text = "Forget";
            this.forgetPairingsButton.Click += new System.EventHandler(this.forgetPairingsButton_Click);
            //
            // bookmarksRow
            //
            this.bookmarksRow.Controls.Add(this.bookmarksLabel);
            this.bookmarksRow.Controls.Add(this.bookmarksValue);
            this.bookmarksRow.Controls.Add(this.clearBookmarksButton);
            this.bookmarksRow.CornerRadius = 0;
            this.bookmarksRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.bookmarksRow.Location = new System.Drawing.Point(1, 53);
            this.bookmarksRow.Name = "bookmarksRow";
            this.bookmarksRow.Size = new System.Drawing.Size(566, 52);
            this.bookmarksRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.bookmarksRow.TabIndex = 1;
            //
            // bookmarksLabel
            //
            this.bookmarksLabel.Location = new System.Drawing.Point(16, 0);
            this.bookmarksLabel.Name = "bookmarksLabel";
            this.bookmarksLabel.Size = new System.Drawing.Size(120, 52);
            this.bookmarksLabel.SizePx = 13.5F;
            this.bookmarksLabel.TabIndex = 0;
            this.bookmarksLabel.Text = "Bookmarks";
            //
            // bookmarksValue
            //
            this.bookmarksValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.bookmarksValue.Location = new System.Drawing.Point(140, 0);
            this.bookmarksValue.Name = "bookmarksValue";
            this.bookmarksValue.Role = FtpClient.Controls.TextRole.Tertiary;
            this.bookmarksValue.Size = new System.Drawing.Size(334, 52);
            this.bookmarksValue.SizePx = 12.5F;
            this.bookmarksValue.TabIndex = 1;
            this.bookmarksValue.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // clearBookmarksButton
            //
            this.clearBookmarksButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearBookmarksButton.Location = new System.Drawing.Point(482, 12);
            this.clearBookmarksButton.Name = "clearBookmarksButton";
            this.clearBookmarksButton.PaddingX = 8;
            this.clearBookmarksButton.Size = new System.Drawing.Size(68, 28);
            this.clearBookmarksButton.TabIndex = 2;
            this.clearBookmarksButton.Text = "Clear";
            this.clearBookmarksButton.Click += new System.EventHandler(this.clearBookmarksButton_Click);
            //
            // emptyLabel
            //
            this.emptyLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.emptyLabel.Location = new System.Drawing.Point(16, 96);
            this.emptyLabel.Name = "emptyLabel";
            this.emptyLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.emptyLabel.Size = new System.Drawing.Size(568, 440);
            this.emptyLabel.TabIndex = 6;
            this.emptyLabel.Text = "No saved sites yet. Press New site to add one.";
            this.emptyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.emptyLabel.Visible = false;
            //
            // deleteButton
            //
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteButton.CornerRadius = 5;
            this.deleteButton.IconRole = FtpClient.Controls.TextRole.Error;
            this.deleteButton.IconSize = 14;
            this.deleteButton.IconSvg = FtpClient.Controls.Icons.Delete;
            this.deleteButton.Location = new System.Drawing.Point(16, 566);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.PaddingX = 10;
            this.deleteButton.Size = new System.Drawing.Size(104, 34);
            this.deleteButton.TabIndex = 7;
            this.deleteButton.Text = "Delete site";
            this.deleteButton.TextSizePx = 13.5F;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            //
            // connectButton
            //
            this.connectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.connectButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.connectButton.CornerRadius = 5;
            this.connectButton.Location = new System.Drawing.Point(334, 566);
            this.connectButton.Name = "connectButton";
            this.connectButton.PaddingX = 20;
            this.connectButton.Size = new System.Drawing.Size(90, 34);
            this.connectButton.TabIndex = 8;
            this.connectButton.Text = "Connect";
            this.connectButton.TextSizePx = 13.5F;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            //
            // saveButton
            //
            this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.saveButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.saveButton.CornerRadius = 5;
            this.saveButton.Enabled = false;
            this.saveButton.Location = new System.Drawing.Point(432, 566);
            this.saveButton.Name = "saveButton";
            this.saveButton.PaddingX = 20;
            this.saveButton.Size = new System.Drawing.Size(68, 34);
            this.saveButton.TabIndex = 9;
            this.saveButton.Text = "Save";
            this.saveButton.TextSizePx = 13.5F;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // closeButton
            //
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.closeButton.CornerRadius = 5;
            this.closeButton.Location = new System.Drawing.Point(508, 566);
            this.closeButton.Name = "closeButton";
            this.closeButton.PaddingX = 20;
            this.closeButton.Size = new System.Drawing.Size(76, 34);
            this.closeButton.TabIndex = 10;
            this.closeButton.Text = "Close";
            this.closeButton.TextSizePx = 13.5F;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            //
            // SiteManagerDialog
            //
            this.ClientSize = new System.Drawing.Size(860, 652);
            this.Controls.Add(this.detail);
            this.Controls.Add(this.rail);
            this.MinimumSize = new System.Drawing.Size(760, 560);
            this.Name = "SiteManagerDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Site manager";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.rail.ResumeLayout(false);
            this.railToolbar.ResumeLayout(false);
            this.detail.ResumeLayout(false);
            this.generalCard.ResumeLayout(false);
            this.directoryRow.ResumeLayout(false);
            this.keyRow.ResumeLayout(false);
            this.userRow.ResumeLayout(false);
            this.portRow.ResumeLayout(false);
            this.hostRow.ResumeLayout(false);
            this.protocolRow.ResumeLayout(false);
            this.advancedCard.ResumeLayout(false);
            this.hostKeyRow.ResumeLayout(false);
            this.passwordRow.ResumeLayout(false);
            this.authRow.ResumeLayout(false);
            this.groupRow.ResumeLayout(false);
            this.nameRow.ResumeLayout(false);
            this.transferCard.ResumeLayout(false);
            this.bookmarksRow.ResumeLayout(false);
            this.pairingsRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel rail;
        private FtpClient.Controls.SiteTreeListControl siteTree;
        private FtpClient.Controls.SurfacePanel railToolbar;
        private FtpClient.Controls.CommandButton newSiteButton;
        private FtpClient.Controls.CommandButton folderButton;
        private FtpClient.Controls.SurfacePanel detail;
        private FtpClient.Controls.TextLabel nameLabel;
        private FtpClient.Controls.Chip connectedChip;
        private FtpClient.Controls.CardTabStrip detailTabs;
        private FtpClient.Controls.SurfacePanel generalCard;
        private FtpClient.Controls.SurfacePanel protocolRow;
        private FtpClient.Controls.TextLabel protocolLabel;
        private ModernWinForms.ModernComboBox protocolBox;
        private FtpClient.Controls.SurfacePanel hostRow;
        private FtpClient.Controls.TextLabel hostLabel;
        private ModernWinForms.ModernTextBox hostBox;
        private FtpClient.Controls.SurfacePanel portRow;
        private FtpClient.Controls.TextLabel portLabel;
        private ModernWinForms.ModernTextBox portBox;
        private FtpClient.Controls.SurfacePanel userRow;
        private FtpClient.Controls.TextLabel userLabel;
        private ModernWinForms.ModernTextBox userBox;
        private FtpClient.Controls.SurfacePanel keyRow;
        private FtpClient.Controls.TextLabel keyLabel;
        private ModernWinForms.ModernTextBox keyBox;
        private ModernWinForms.ModernTextBoxButton browseKeyButton;
        private FtpClient.Controls.SurfacePanel directoryRow;
        private FtpClient.Controls.TextLabel directoryLabel;
        private ModernWinForms.ModernTextBox directoryBox;
        private FtpClient.Controls.SurfacePanel advancedCard;
        private FtpClient.Controls.SurfacePanel nameRow;
        private FtpClient.Controls.TextLabel nameFieldLabel;
        private ModernWinForms.ModernTextBox nameBox;
        private FtpClient.Controls.SurfacePanel groupRow;
        private FtpClient.Controls.TextLabel groupLabel;
        private ModernWinForms.ModernTextBox groupBox;
        private FtpClient.Controls.SurfacePanel authRow;
        private FtpClient.Controls.TextLabel authLabel;
        private ModernWinForms.ModernComboBox authBox;
        private FtpClient.Controls.SurfacePanel passwordRow;
        private FtpClient.Controls.TextLabel passwordLabel;
        private ModernWinForms.ModernTextBox passwordBox;
        private FtpClient.Controls.SurfacePanel hostKeyRow;
        private FtpClient.Controls.TextLabel hostKeyLabel;
        private FtpClient.Controls.TextLabel hostKeyValue;
        private FtpClient.Controls.CommandButton forgetKeyButton;
        private FtpClient.Controls.SurfacePanel transferCard;
        private FtpClient.Controls.SurfacePanel pairingsRow;
        private FtpClient.Controls.TextLabel pairingsLabel;
        private FtpClient.Controls.TextLabel pairingsValue;
        private FtpClient.Controls.CommandButton forgetPairingsButton;
        private FtpClient.Controls.SurfacePanel bookmarksRow;
        private FtpClient.Controls.TextLabel bookmarksLabel;
        private FtpClient.Controls.TextLabel bookmarksValue;
        private FtpClient.Controls.CommandButton clearBookmarksButton;
        private FtpClient.Controls.TextLabel emptyLabel;
        private FtpClient.Controls.CommandButton deleteButton;
        private FtpClient.Controls.CommandButton connectButton;
        private FtpClient.Controls.CommandButton saveButton;
        private FtpClient.Controls.CommandButton closeButton;
    }
}
