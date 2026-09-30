namespace FtpClient.Forms
{
    partial class MainForm
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
            this.addMenu = new ModernWinForms.ModernContextMenu(this.components);
            this.newConnectionItem = new ModernWinForms.ModernContextMenuItem();
            this.openSiteItem = new ModernWinForms.ModernContextMenuItem();
            this.siteManagerItem = new ModernWinForms.ModernContextMenuItem();
            this.settingsItem = new ModernWinForms.ModernContextMenuItem();
            this.tabMenu = new ModernWinForms.ModernContextMenu(this.components);
            this.tabRenameItem = new ModernWinForms.ModernContextMenuItem();
            this.tabSaveSiteItem = new ModernWinForms.ModernContextMenuItem();
            this.tabReconnectItem = new ModernWinForms.ModernContextMenuItem();
            this.tabLogItem = new ModernWinForms.ModernContextMenuItem();
            this.tabCloseItem = new ModernWinForms.ModernContextMenuItem();
            this.tabCloseOthersItem = new ModernWinForms.ModernContextMenuItem();
            this.sessionTabs = new ModernWinForms.ModernTabStrip();
            this.hostPanel = new FtpClient.Controls.SurfacePanel();
            this.connectView = new FtpClient.Views.ConnectView();
            this.hostPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // addMenu
            //
            this.addMenu.Items.Add(this.newConnectionItem);
            this.addMenu.Items.Add(this.openSiteItem);
            this.addMenu.Items.Add(this.siteManagerItem);
            this.addMenu.Items.Add(this.settingsItem);
            //
            // newConnectionItem
            //
            this.newConnectionItem.Shortcut = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T;
            this.newConnectionItem.Text = "New connection…";
            this.newConnectionItem.Click += new System.EventHandler(this.newConnectionItem_Click);
            //
            // openSiteItem
            //
            this.openSiteItem.Text = "Open a saved site";
            //
            // siteManagerItem
            //
            this.siteManagerItem.BeginGroup = true;
            this.siteManagerItem.Text = "Site manager…";
            this.siteManagerItem.Click += new System.EventHandler(this.siteManagerItem_Click);
            //
            // settingsItem
            //
            this.settingsItem.Shortcut = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Oemcomma;
            this.settingsItem.Text = "Settings…";
            this.settingsItem.Click += new System.EventHandler(this.settingsItem_Click);
            //
            // tabMenu
            //
            this.tabMenu.Items.Add(this.tabRenameItem);
            this.tabMenu.Items.Add(this.tabSaveSiteItem);
            this.tabMenu.Items.Add(this.tabReconnectItem);
            this.tabMenu.Items.Add(this.tabLogItem);
            this.tabMenu.Items.Add(this.tabCloseItem);
            this.tabMenu.Items.Add(this.tabCloseOthersItem);
            //
            // tabRenameItem
            //
            this.tabRenameItem.Text = "Rename…";
            this.tabRenameItem.Click += new System.EventHandler(this.tabRenameItem_Click);
            //
            // tabSaveSiteItem
            //
            this.tabSaveSiteItem.Shortcut = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S;
            this.tabSaveSiteItem.Text = "Save as site…";
            this.tabSaveSiteItem.Click += new System.EventHandler(this.tabSaveSiteItem_Click);
            //
            // tabReconnectItem
            //
            this.tabReconnectItem.BeginGroup = true;
            this.tabReconnectItem.Text = "Reconnect";
            this.tabReconnectItem.Click += new System.EventHandler(this.tabReconnectItem_Click);
            //
            // tabLogItem
            //
            this.tabLogItem.Text = "Protocol log";
            this.tabLogItem.Click += new System.EventHandler(this.tabLogItem_Click);
            //
            // tabCloseItem
            //
            this.tabCloseItem.BeginGroup = true;
            this.tabCloseItem.Shortcut = System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W;
            this.tabCloseItem.Text = "Close";
            this.tabCloseItem.Click += new System.EventHandler(this.tabCloseItem_Click);
            //
            // tabCloseOthersItem
            //
            this.tabCloseOthersItem.Text = "Close others";
            this.tabCloseOthersItem.Click += new System.EventHandler(this.tabCloseOthersItem_Click);
            //
            // sessionTabs
            //
            this.sessionTabs.Dock = System.Windows.Forms.DockStyle.Top;
            this.sessionTabs.Location = new System.Drawing.Point(0, 31);
            this.sessionTabs.Name = "sessionTabs";
            this.sessionTabs.Size = new System.Drawing.Size(1440, 38);
            this.sessionTabs.TabIndex = 0;
            this.sessionTabs.Visible = false;
            this.sessionTabs.SelectedIndexChanged += new System.EventHandler(this.sessionTabs_SelectedIndexChanged);
            this.sessionTabs.TabCloseRequested += new System.EventHandler<ModernWinForms.ModernTabStripEventArgs>(this.sessionTabs_TabCloseRequested);
            this.sessionTabs.TabContextMenuRequested += new System.EventHandler<ModernWinForms.ModernTabStripEventArgs>(this.sessionTabs_TabContextMenuRequested);
            this.sessionTabs.AddRequested += new System.EventHandler(this.sessionTabs_AddRequested);
            this.sessionTabs.TabMoved += new System.EventHandler<ModernWinForms.ModernTabMovedEventArgs>(this.sessionTabs_TabMoved);
            this.sessionTabs.TabDoubleClick += new System.EventHandler<ModernWinForms.ModernTabStripEventArgs>(this.sessionTabs_TabDoubleClick);
            //
            // hostPanel
            //
            this.hostPanel.Controls.Add(this.connectView);
            this.hostPanel.CornerRadius = 0;
            this.hostPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hostPanel.Location = new System.Drawing.Point(0, 69);
            this.hostPanel.Name = "hostPanel";
            this.hostPanel.Size = new System.Drawing.Size(1440, 831);
            this.hostPanel.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.hostPanel.TabIndex = 1;
            //
            // connectView
            //
            this.connectView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.connectView.Location = new System.Drawing.Point(0, 0);
            this.connectView.Name = "connectView";
            this.connectView.Size = new System.Drawing.Size(1440, 831);
            this.connectView.TabIndex = 0;
            this.connectView.QuickConnectRequested += new System.EventHandler<FtpClient.Views.QuickConnectEventArgs>(this.connectView_QuickConnectRequested);
            this.connectView.SaveSiteRequested += new System.EventHandler<FtpClient.Views.QuickConnectEventArgs>(this.connectView_SaveSiteRequested);
            this.connectView.SiteEditRequested += new System.EventHandler<FtpClient.Controls.SiteEventArgs>(this.connectView_SiteEditRequested);
            this.connectView.SiteRenameRequested += new System.EventHandler<FtpClient.Controls.SiteEventArgs>(this.connectView_SiteRenameRequested);
            this.connectView.SiteDeleteRequested += new System.EventHandler<FtpClient.Controls.SiteEventArgs>(this.connectView_SiteDeleteRequested);
            this.connectView.SiteActivated += new System.EventHandler<FtpClient.Controls.SiteEventArgs>(this.connectView_SiteActivated);
            this.connectView.ManageRequested += new System.EventHandler(this.connectView_ManageRequested);
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(1440, 900);
            this.Controls.Add(this.hostPanel);
            this.Controls.Add(this.sessionTabs);
            this.MinimumSize = new System.Drawing.Size(980, 640);
            this.Name = "MainForm";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FTP Client";
            this.TitleBar.ShowIcon = true;
            this.hostPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private ModernWinForms.ModernContextMenu addMenu;
        private ModernWinForms.ModernContextMenuItem newConnectionItem;
        private ModernWinForms.ModernContextMenuItem openSiteItem;
        private ModernWinForms.ModernContextMenuItem siteManagerItem;
        private ModernWinForms.ModernContextMenuItem settingsItem;
        private ModernWinForms.ModernContextMenu tabMenu;
        private ModernWinForms.ModernContextMenuItem tabRenameItem;
        private ModernWinForms.ModernContextMenuItem tabSaveSiteItem;
        private ModernWinForms.ModernContextMenuItem tabReconnectItem;
        private ModernWinForms.ModernContextMenuItem tabLogItem;
        private ModernWinForms.ModernContextMenuItem tabCloseItem;
        private ModernWinForms.ModernContextMenuItem tabCloseOthersItem;
        private ModernWinForms.ModernTabStrip sessionTabs;
        private FtpClient.Controls.SurfacePanel hostPanel;
        private FtpClient.Views.ConnectView connectView;
    }
}
