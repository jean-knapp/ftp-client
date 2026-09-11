namespace FtpClient.Views
{
    partial class ConnectView
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.statusBar = new FtpClient.Controls.StatusBarControl();
            this.contentPanel = new FtpClient.Controls.SurfacePanel();
            this.headingLabel = new FtpClient.Controls.TextLabel();
            this.subheadLabel = new FtpClient.Controls.TextLabel();
            this.quickCard = new FtpClient.Controls.SurfacePanel();
            this.hostLabel = new FtpClient.Controls.TextLabel();
            this.hostBox = new ModernWinForms.ModernTextBox();
            this.recentHostsButton = new ModernWinForms.ModernTextBoxButton();
            this.protocolLabel = new FtpClient.Controls.TextLabel();
            this.protocolBox = new ModernWinForms.ModernComboBox();
            this.portLabel = new FtpClient.Controls.TextLabel();
            this.portBox = new ModernWinForms.ModernTextBox();
            this.userLabel = new FtpClient.Controls.TextLabel();
            this.userBox = new ModernWinForms.ModernTextBox();
            this.secretLabel = new FtpClient.Controls.TextLabel();
            this.secretBox = new ModernWinForms.ModernTextBox();
            this.browseKeyButton = new ModernWinForms.ModernTextBoxButton();
            this.nameLabel = new FtpClient.Controls.TextLabel();
            this.nameBox = new ModernWinForms.ModernTextBox();
            this.connectButton = new FtpClient.Controls.CommandButton();
            this.saveButton = new FtpClient.Controls.CommandButton();
            this.rememberBox = new FtpClient.Controls.TokenCheckBox();
            this.savedLabel = new FtpClient.Controls.TextLabel();
            this.savedRule = new FtpClient.Controls.SurfacePanel();
            this.manageButton = new FtpClient.Controls.CommandButton();
            this.savedCard = new FtpClient.Controls.SurfacePanel();
            this.savedList = new FtpClient.Controls.SavedSitesListControl();
            this.recentMenu = new ModernWinForms.ModernContextMenu(this.components);
            this.siteMenu = new ModernWinForms.ModernContextMenu(this.components);
            this.siteConnectItem = new ModernWinForms.ModernContextMenuItem();
            this.siteEditItem = new ModernWinForms.ModernContextMenuItem();
            this.siteRenameItem = new ModernWinForms.ModernContextMenuItem();
            this.siteCopyAddressItem = new ModernWinForms.ModernContextMenuItem();
            this.siteDeleteItem = new ModernWinForms.ModernContextMenuItem();
            this.contentPanel.SuspendLayout();
            this.quickCard.SuspendLayout();
            this.savedCard.SuspendLayout();
            this.SuspendLayout();
            //
            // statusBar
            //
            this.statusBar.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.statusBar.Location = new System.Drawing.Point(0, 792);
            this.statusBar.Name = "statusBar";
            this.statusBar.Size = new System.Drawing.Size(1280, 28);
            this.statusBar.State = FtpClient.Controls.SessionState.Disconnected;
            this.statusBar.TabIndex = 1;
            //
            // contentPanel
            //
            this.contentPanel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.contentPanel.Controls.Add(this.headingLabel);
            this.contentPanel.Controls.Add(this.subheadLabel);
            this.contentPanel.Controls.Add(this.quickCard);
            this.contentPanel.Controls.Add(this.savedLabel);
            this.contentPanel.Controls.Add(this.savedRule);
            this.contentPanel.Controls.Add(this.manageButton);
            this.contentPanel.Controls.Add(this.savedCard);
            this.contentPanel.CornerRadius = 0;
            this.contentPanel.Location = new System.Drawing.Point(180, 60);
            this.contentPanel.Name = "contentPanel";
            this.contentPanel.Size = new System.Drawing.Size(920, 600);
            this.contentPanel.Surface = FtpClient.Controls.SurfaceKind.None;
            this.contentPanel.TabIndex = 0;
            //
            // headingLabel
            //
            this.headingLabel.Location = new System.Drawing.Point(0, 0);
            this.headingLabel.Name = "headingLabel";
            this.headingLabel.Semibold = true;
            this.headingLabel.Size = new System.Drawing.Size(920, 42);
            this.headingLabel.SizePx = 32F;
            this.headingLabel.TabIndex = 0;
            this.headingLabel.Text = "Connect to a server";
            //
            // subheadLabel
            //
            this.subheadLabel.Location = new System.Drawing.Point(0, 48);
            this.subheadLabel.Name = "subheadLabel";
            this.subheadLabel.Role = FtpClient.Controls.TextRole.Secondary;
            this.subheadLabel.Size = new System.Drawing.Size(920, 22);
            this.subheadLabel.SizePx = 15F;
            this.subheadLabel.TabIndex = 1;
            this.subheadLabel.Text = "Enter a host to connect straight away, or open a saved site.";
            //
            // quickCard
            //
            this.quickCard.Controls.Add(this.hostLabel);
            this.quickCard.Controls.Add(this.hostBox);
            this.quickCard.Controls.Add(this.protocolLabel);
            this.quickCard.Controls.Add(this.protocolBox);
            this.quickCard.Controls.Add(this.portLabel);
            this.quickCard.Controls.Add(this.portBox);
            this.quickCard.Controls.Add(this.userLabel);
            this.quickCard.Controls.Add(this.userBox);
            this.quickCard.Controls.Add(this.secretLabel);
            this.quickCard.Controls.Add(this.secretBox);
            this.quickCard.Controls.Add(this.nameLabel);
            this.quickCard.Controls.Add(this.nameBox);
            this.quickCard.Controls.Add(this.connectButton);
            this.quickCard.Controls.Add(this.saveButton);
            this.quickCard.Controls.Add(this.rememberBox);
            this.quickCard.Location = new System.Drawing.Point(0, 98);
            this.quickCard.Name = "quickCard";
            this.quickCard.Size = new System.Drawing.Size(920, 276);
            this.quickCard.TabIndex = 2;
            //
            // hostLabel
            //
            this.hostLabel.Location = new System.Drawing.Point(20, 18);
            this.hostLabel.Name = "hostLabel";
            this.hostLabel.Size = new System.Drawing.Size(592, 18);
            this.hostLabel.TabIndex = 0;
            this.hostLabel.Text = "Host";
            //
            // hostBox
            //
            this.hostBox.Buttons.Add(this.recentHostsButton);
            this.hostBox.Location = new System.Drawing.Point(20, 40);
            this.hostBox.Name = "hostBox";
            this.hostBox.PlaceholderText = "host name or IP address";
            this.hostBox.Size = new System.Drawing.Size(592, 34);
            this.hostBox.TabIndex = 1;
            this.hostBox.ButtonClick += new System.EventHandler<ModernWinForms.ModernTextBoxButtonEventArgs>(this.hostBox_ButtonClick);
            //
            // recentHostsButton
            //
            this.recentHostsButton.ToolTipText = "Recent hosts";
            //
            // protocolLabel
            //
            this.protocolLabel.Location = new System.Drawing.Point(624, 18);
            this.protocolLabel.Name = "protocolLabel";
            this.protocolLabel.Size = new System.Drawing.Size(148, 18);
            this.protocolLabel.TabIndex = 2;
            this.protocolLabel.Text = "Protocol";
            //
            // protocolBox
            //
            this.protocolBox.Items.AddRange(new object[] {
            "SFTP",
            "FTP",
            "FTPS",
            "Local folder"});
            this.protocolBox.Location = new System.Drawing.Point(624, 40);
            this.protocolBox.Name = "protocolBox";
            this.protocolBox.Size = new System.Drawing.Size(148, 34);
            this.protocolBox.TabIndex = 3;
            this.protocolBox.SelectedIndexChanged += new System.EventHandler<ModernWinForms.SelectedIndexChangedEventArgs>(this.protocolBox_SelectedIndexChanged);
            //
            // portLabel
            //
            this.portLabel.Location = new System.Drawing.Point(784, 18);
            this.portLabel.Name = "portLabel";
            this.portLabel.Size = new System.Drawing.Size(92, 18);
            this.portLabel.TabIndex = 4;
            this.portLabel.Text = "Port";
            //
            // portBox
            //
            this.portBox.Location = new System.Drawing.Point(784, 40);
            this.portBox.MaxLength = 5;
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(92, 34);
            this.portBox.TabIndex = 5;
            //
            // userLabel
            //
            this.userLabel.Location = new System.Drawing.Point(20, 90);
            this.userLabel.Name = "userLabel";
            this.userLabel.Size = new System.Drawing.Size(310, 18);
            this.userLabel.TabIndex = 6;
            this.userLabel.Text = "User";
            //
            // userBox
            //
            this.userBox.Location = new System.Drawing.Point(20, 112);
            this.userBox.Name = "userBox";
            this.userBox.Size = new System.Drawing.Size(310, 34);
            this.userBox.TabIndex = 7;
            //
            // secretLabel
            //
            this.secretLabel.Location = new System.Drawing.Point(342, 90);
            this.secretLabel.Name = "secretLabel";
            this.secretLabel.Size = new System.Drawing.Size(310, 18);
            this.secretLabel.TabIndex = 8;
            this.secretLabel.Text = "Key or password";
            //
            // secretBox
            //
            this.secretBox.Buttons.Add(this.browseKeyButton);
            this.secretBox.Location = new System.Drawing.Point(342, 112);
            this.secretBox.Name = "secretBox";
            this.secretBox.PlaceholderText = "password, or a key file";
            this.secretBox.Size = new System.Drawing.Size(310, 34);
            this.secretBox.TabIndex = 9;
            this.secretBox.ButtonClick += new System.EventHandler<ModernWinForms.ModernTextBoxButtonEventArgs>(this.secretBox_ButtonClick);
            this.secretBox.TextChanged += new System.EventHandler(this.secretBox_TextChanged);
            //
            // browseKeyButton
            //
            this.browseKeyButton.ToolTipText = "Choose a key file…";
            //
            // nameLabel
            //
            this.nameLabel.Location = new System.Drawing.Point(20, 162);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(656, 18);
            this.nameLabel.TabIndex = 10;
            this.nameLabel.Text = "Name";
            //
            // nameBox
            //
            this.nameBox.Location = new System.Drawing.Point(20, 184);
            this.nameBox.Name = "nameBox";
            this.nameBox.PlaceholderText = "shown in the tab header, e.g. web-prod";
            this.nameBox.Size = new System.Drawing.Size(656, 34);
            this.nameBox.TabIndex = 11;
            //
            // connectButton
            //
            this.connectButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.connectButton.CornerRadius = 5;
            this.connectButton.Location = new System.Drawing.Point(688, 184);
            this.connectButton.Name = "connectButton";
            this.connectButton.PaddingX = 20;
            this.connectButton.Size = new System.Drawing.Size(94, 34);
            this.connectButton.TabIndex = 12;
            this.connectButton.Text = "Connect";
            this.connectButton.TextSizePx = 13.5F;
            this.connectButton.Click += new System.EventHandler(this.connectButton_Click);
            //
            // saveButton
            //
            this.saveButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.saveButton.CornerRadius = 5;
            this.saveButton.Location = new System.Drawing.Point(766, 112);
            this.saveButton.Name = "saveButton";
            this.saveButton.PaddingX = 16;
            this.saveButton.Size = new System.Drawing.Size(110, 34);
            this.saveButton.TabIndex = 11;
            this.saveButton.Text = "Save as site…";
            this.saveButton.TextSizePx = 13.5F;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            //
            // rememberBox
            //
            this.rememberBox.Location = new System.Drawing.Point(20, 162);
            this.rememberBox.Name = "rememberBox";
            this.rememberBox.Size = new System.Drawing.Size(320, 22);
            this.rememberBox.TabIndex = 12;
            this.rememberBox.Text = "Remember this host in Recent";
            this.rememberBox.CheckedChanged += new System.EventHandler(this.rememberBox_CheckedChanged);
            //
            // savedLabel
            //
            this.savedLabel.Location = new System.Drawing.Point(0, 327);
            this.savedLabel.Name = "savedLabel";
            this.savedLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.savedLabel.Semibold = true;
            this.savedLabel.Size = new System.Drawing.Size(110, 20);
            this.savedLabel.SizePx = 13F;
            this.savedLabel.TabIndex = 3;
            this.savedLabel.Text = "Saved sites";
            this.savedLabel.Uppercase = true;
            //
            // savedRule
            //
            this.savedRule.BottomDivider = true;
            this.savedRule.CornerRadius = 0;
            this.savedRule.Location = new System.Drawing.Point(120, 337);
            this.savedRule.Name = "savedRule";
            this.savedRule.Size = new System.Drawing.Size(700, 1);
            this.savedRule.Surface = FtpClient.Controls.SurfaceKind.None;
            this.savedRule.TabIndex = 4;
            //
            // manageButton
            //
            this.manageButton.Location = new System.Drawing.Point(832, 323);
            this.manageButton.Name = "manageButton";
            this.manageButton.PaddingX = 8;
            this.manageButton.Size = new System.Drawing.Size(88, 28);
            this.manageButton.TabIndex = 5;
            this.manageButton.Text = "Manage…";
            this.manageButton.Click += new System.EventHandler(this.manageButton_Click);
            //
            // savedCard
            //
            this.savedCard.Controls.Add(this.savedList);
            this.savedCard.Location = new System.Drawing.Point(0, 356);
            this.savedCard.Name = "savedCard";
            this.savedCard.Padding = new System.Windows.Forms.Padding(1);
            this.savedCard.Size = new System.Drawing.Size(920, 212);
            this.savedCard.TabIndex = 6;
            //
            // savedList
            //
            this.savedList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.savedList.Location = new System.Drawing.Point(1, 1);
            this.savedList.Name = "savedList";
            this.savedList.Size = new System.Drawing.Size(918, 210);
            this.savedList.TabIndex = 0;
            this.savedList.SiteActivated += new System.EventHandler<FtpClient.Controls.SiteEventArgs>(this.savedList_SiteActivated);
            this.savedList.SiteMenuRequested += new System.EventHandler<FtpClient.Controls.SiteMenuEventArgs>(this.savedList_SiteMenuRequested);
            this.savedList.KeyDown += new System.Windows.Forms.KeyEventHandler(this.savedList_KeyDown);
            //
            // siteMenu
            //
            this.siteMenu.Items.Add(this.siteConnectItem);
            this.siteMenu.Items.Add(this.siteEditItem);
            this.siteMenu.Items.Add(this.siteRenameItem);
            this.siteMenu.Items.Add(this.siteCopyAddressItem);
            this.siteMenu.Items.Add(this.siteDeleteItem);
            //
            // siteConnectItem
            //
            this.siteConnectItem.Text = "Connect";
            this.siteConnectItem.Click += new System.EventHandler(this.siteConnectItem_Click);
            //
            // siteEditItem
            //
            this.siteEditItem.BeginGroup = true;
            this.siteEditItem.Text = "Edit in site manager…";
            this.siteEditItem.Click += new System.EventHandler(this.siteEditItem_Click);
            //
            // siteRenameItem
            //
            this.siteRenameItem.Shortcut = System.Windows.Forms.Keys.F2;
            this.siteRenameItem.Text = "Rename…";
            this.siteRenameItem.Click += new System.EventHandler(this.siteRenameItem_Click);
            //
            // siteCopyAddressItem
            //
            this.siteCopyAddressItem.Text = "Copy address";
            this.siteCopyAddressItem.Click += new System.EventHandler(this.siteCopyAddressItem_Click);
            //
            // siteDeleteItem
            //
            this.siteDeleteItem.BeginGroup = true;
            this.siteDeleteItem.Shortcut = System.Windows.Forms.Keys.Delete;
            this.siteDeleteItem.Text = "Delete…";
            this.siteDeleteItem.Click += new System.EventHandler(this.siteDeleteItem_Click);
            //
            // ConnectView
            //
            this.Controls.Add(this.contentPanel);
            this.Controls.Add(this.statusBar);
            this.Name = "ConnectView";
            this.Size = new System.Drawing.Size(1280, 820);
            this.contentPanel.ResumeLayout(false);
            this.quickCard.ResumeLayout(false);
            this.savedCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private FtpClient.Controls.StatusBarControl statusBar;
        private FtpClient.Controls.SurfacePanel contentPanel;
        private FtpClient.Controls.TextLabel headingLabel;
        private FtpClient.Controls.TextLabel subheadLabel;
        private FtpClient.Controls.SurfacePanel quickCard;
        private FtpClient.Controls.TextLabel hostLabel;
        private ModernWinForms.ModernTextBox hostBox;
        private ModernWinForms.ModernTextBoxButton recentHostsButton;
        private FtpClient.Controls.TextLabel protocolLabel;
        private ModernWinForms.ModernComboBox protocolBox;
        private FtpClient.Controls.TextLabel portLabel;
        private ModernWinForms.ModernTextBox portBox;
        private FtpClient.Controls.TextLabel userLabel;
        private ModernWinForms.ModernTextBox userBox;
        private FtpClient.Controls.TextLabel secretLabel;
        private ModernWinForms.ModernTextBox secretBox;
        private ModernWinForms.ModernTextBoxButton browseKeyButton;
        private FtpClient.Controls.TextLabel nameLabel;
        private ModernWinForms.ModernTextBox nameBox;
        private FtpClient.Controls.CommandButton connectButton;
        private FtpClient.Controls.CommandButton saveButton;
        private FtpClient.Controls.TokenCheckBox rememberBox;
        private FtpClient.Controls.TextLabel savedLabel;
        private FtpClient.Controls.SurfacePanel savedRule;
        private FtpClient.Controls.CommandButton manageButton;
        private FtpClient.Controls.SurfacePanel savedCard;
        private FtpClient.Controls.SavedSitesListControl savedList;
        private ModernWinForms.ModernContextMenu recentMenu;
        private ModernWinForms.ModernContextMenu siteMenu;
        private ModernWinForms.ModernContextMenuItem siteConnectItem;
        private ModernWinForms.ModernContextMenuItem siteEditItem;
        private ModernWinForms.ModernContextMenuItem siteRenameItem;
        private ModernWinForms.ModernContextMenuItem siteCopyAddressItem;
        private ModernWinForms.ModernContextMenuItem siteDeleteItem;
    }
}
