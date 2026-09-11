namespace FtpClient.Forms
{
    partial class SymlinkDialog
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
            this.content = new FtpClient.Controls.SurfacePanel();
            this.headingLabel = new FtpClient.Controls.TextLabel();
            this.bodyLabel = new FtpClient.Controls.TextLabel();
            this.targetCard = new FtpClient.Controls.SurfacePanel();
            this.targetCaption = new FtpClient.Controls.TextLabel();
            this.targetPathLabel = new FtpClient.Controls.TextLabel();
            this.targetDetailLabel = new FtpClient.Controls.TextLabel();
            this.arrowLabel = new FtpClient.Controls.TextLabel();
            this.linkCard = new FtpClient.Controls.SurfacePanel();
            this.linkCaption = new FtpClient.Controls.TextLabel();
            this.linkPathLabel = new FtpClient.Controls.TextLabel();
            this.linkDetailLabel = new FtpClient.Controls.TextLabel();
            this.placeLabel = new FtpClient.Controls.TextLabel();
            this.treeCard = new FtpClient.Controls.SurfacePanel();
            this.tree = new FtpClient.Controls.RemoteTreeControl();
            this.optionsCard = new FtpClient.Controls.SurfacePanel();
            this.relativeRow = new FtpClient.Controls.SurfacePanel();
            this.relativeTitle = new FtpClient.Controls.TextLabel();
            this.relativeHint = new FtpClient.Controls.TextLabel();
            this.relativeSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.nameRow = new FtpClient.Controls.SurfacePanel();
            this.nameTitle = new FtpClient.Controls.TextLabel();
            this.nameHint = new FtpClient.Controls.TextLabel();
            this.nameBox = new ModernWinForms.ModernTextBox();
            this.createButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.content.SuspendLayout();
            this.targetCard.SuspendLayout();
            this.linkCard.SuspendLayout();
            this.treeCard.SuspendLayout();
            this.optionsCard.SuspendLayout();
            this.relativeRow.SuspendLayout();
            this.nameRow.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Size = new System.Drawing.Size(660, 652);
            this.content.Controls.Add(this.headingLabel);
            this.content.Controls.Add(this.bodyLabel);
            this.content.Controls.Add(this.targetCard);
            this.content.Controls.Add(this.arrowLabel);
            this.content.Controls.Add(this.linkCard);
            this.content.Controls.Add(this.placeLabel);
            this.content.Controls.Add(this.treeCard);
            this.content.Controls.Add(this.optionsCard);
            this.content.Controls.Add(this.createButton);
            this.content.Controls.Add(this.cancelButton);
            this.content.CornerRadius = 0;
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Name = "content";
            this.content.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.content.TabIndex = 0;
            //
            // headingLabel
            //
            this.headingLabel.Location = new System.Drawing.Point(24, 18);
            this.headingLabel.Name = "headingLabel";
            this.headingLabel.Semibold = true;
            this.headingLabel.Size = new System.Drawing.Size(612, 28);
            this.headingLabel.SizePx = 20F;
            this.headingLabel.TabIndex = 0;
            this.headingLabel.Text = "Create a symbolic link";
            //
            // bodyLabel
            //
            this.bodyLabel.Location = new System.Drawing.Point(24, 50);
            this.bodyLabel.MultiLine = true;
            this.bodyLabel.Name = "bodyLabel";
            this.bodyLabel.Role = FtpClient.Controls.TextRole.Secondary;
            this.bodyLabel.Size = new System.Drawing.Size(612, 40);
            this.bodyLabel.SizePx = 13.5F;
            this.bodyLabel.TabIndex = 1;
            this.bodyLabel.Text = "The link opens what it points to. Nothing is copied, and deleting the link later leaves the original alone.";
            this.bodyLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // targetCard
            //
            this.targetCard.Controls.Add(this.targetCaption);
            this.targetCard.Controls.Add(this.targetPathLabel);
            this.targetCard.Controls.Add(this.targetDetailLabel);
            this.targetCard.CornerRadius = 7;
            this.targetCard.Location = new System.Drawing.Point(24, 104);
            this.targetCard.Name = "targetCard";
            this.targetCard.Size = new System.Drawing.Size(280, 92);
            this.targetCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.targetCard.TabIndex = 2;
            //
            // targetCaption
            //
            this.targetCaption.Location = new System.Drawing.Point(14, 12);
            this.targetCaption.Name = "targetCaption";
            this.targetCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.targetCaption.Semibold = true;
            this.targetCaption.Size = new System.Drawing.Size(250, 16);
            this.targetCaption.SizePx = 11F;
            this.targetCaption.TabIndex = 0;
            this.targetCaption.Text = "Points to";
            this.targetCaption.Uppercase = true;
            //
            // targetPathLabel
            //
            this.targetPathLabel.IconGap = 10;
            this.targetPathLabel.IconRole = FtpClient.Controls.TextRole.Folder;
            this.targetPathLabel.IconSize = 14;
            this.targetPathLabel.IconSvg = FtpClient.Controls.Icons.Folder;
            this.targetPathLabel.Location = new System.Drawing.Point(14, 32);
            this.targetPathLabel.Monospace = true;
            this.targetPathLabel.Name = "targetPathLabel";
            this.targetPathLabel.Size = new System.Drawing.Size(252, 22);
            this.targetPathLabel.SizePx = 13.5F;
            this.targetPathLabel.TabIndex = 1;
            this.targetPathLabel.Text = "/";
            //
            // targetDetailLabel
            //
            this.targetDetailLabel.Location = new System.Drawing.Point(14, 60);
            this.targetDetailLabel.Name = "targetDetailLabel";
            this.targetDetailLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.targetDetailLabel.Size = new System.Drawing.Size(252, 18);
            this.targetDetailLabel.SizePx = 12F;
            this.targetDetailLabel.TabIndex = 2;
            //
            // arrowLabel
            //
            this.arrowLabel.IconGap = 0;
            this.arrowLabel.IconRole = FtpClient.Controls.TextRole.Lane2;
            this.arrowLabel.IconSize = 22;
            this.arrowLabel.IconSvg = FtpClient.Controls.Icons.Link;
            this.arrowLabel.Location = new System.Drawing.Point(319, 139);
            this.arrowLabel.Name = "arrowLabel";
            this.arrowLabel.Size = new System.Drawing.Size(22, 22);
            this.arrowLabel.TabIndex = 3;
            //
            // linkCard
            //
            this.linkCard.Controls.Add(this.linkCaption);
            this.linkCard.Controls.Add(this.linkPathLabel);
            this.linkCard.Controls.Add(this.linkDetailLabel);
            this.linkCard.CornerRadius = 7;
            this.linkCard.Location = new System.Drawing.Point(356, 104);
            this.linkCard.Name = "linkCard";
            this.linkCard.Size = new System.Drawing.Size(280, 92);
            this.linkCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.linkCard.TabIndex = 4;
            //
            // linkCaption
            //
            this.linkCaption.Location = new System.Drawing.Point(14, 12);
            this.linkCaption.Name = "linkCaption";
            this.linkCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.linkCaption.Semibold = true;
            this.linkCaption.Size = new System.Drawing.Size(250, 16);
            this.linkCaption.SizePx = 11F;
            this.linkCaption.TabIndex = 0;
            this.linkCaption.Text = "New link";
            this.linkCaption.Uppercase = true;
            //
            // linkPathLabel
            //
            this.linkPathLabel.IconGap = 10;
            this.linkPathLabel.IconRole = FtpClient.Controls.TextRole.Lane2;
            this.linkPathLabel.IconSize = 14;
            this.linkPathLabel.IconSvg = FtpClient.Controls.Icons.Symlink;
            this.linkPathLabel.Location = new System.Drawing.Point(14, 32);
            this.linkPathLabel.Monospace = true;
            this.linkPathLabel.Name = "linkPathLabel";
            this.linkPathLabel.Size = new System.Drawing.Size(252, 22);
            this.linkPathLabel.SizePx = 13.5F;
            this.linkPathLabel.TabIndex = 1;
            this.linkPathLabel.Text = "/";
            //
            // linkDetailLabel
            //
            this.linkDetailLabel.Location = new System.Drawing.Point(14, 60);
            this.linkDetailLabel.Monospace = true;
            this.linkDetailLabel.Name = "linkDetailLabel";
            this.linkDetailLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.linkDetailLabel.Size = new System.Drawing.Size(252, 18);
            this.linkDetailLabel.SizePx = 12F;
            this.linkDetailLabel.TabIndex = 2;
            //
            // placeLabel
            //
            this.placeLabel.Location = new System.Drawing.Point(24, 212);
            this.placeLabel.Name = "placeLabel";
            this.placeLabel.Semibold = true;
            this.placeLabel.Size = new System.Drawing.Size(612, 19);
            this.placeLabel.SizePx = 13.5F;
            this.placeLabel.TabIndex = 5;
            this.placeLabel.Text = "Put the link in";
            //
            // treeCard
            //
            this.treeCard.Controls.Add(this.tree);
            this.treeCard.CornerRadius = 7;
            this.treeCard.Location = new System.Drawing.Point(24, 238);
            this.treeCard.Name = "treeCard";
            this.treeCard.Padding = new System.Windows.Forms.Padding(1, 6, 1, 6);
            this.treeCard.Size = new System.Drawing.Size(612, 222);
            this.treeCard.TabIndex = 6;
            //
            // tree
            //
            this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tree.Location = new System.Drawing.Point(1, 6);
            this.tree.Name = "tree";
            this.tree.Size = new System.Drawing.Size(610, 210);
            this.tree.TabIndex = 0;
            this.tree.NodeSelected += new System.EventHandler<FtpClient.Controls.TreeNodeEventArgs>(this.tree_NodeSelected);
            this.tree.ExpandRequested += new System.EventHandler<FtpClient.Controls.TreeNodeEventArgs>(this.tree_ExpandRequested);
            //
            // optionsCard
            //
            this.optionsCard.Controls.Add(this.relativeRow);
            this.optionsCard.Controls.Add(this.nameRow);
            this.optionsCard.CornerRadius = 7;
            this.optionsCard.Location = new System.Drawing.Point(24, 476);
            this.optionsCard.Name = "optionsCard";
            this.optionsCard.Padding = new System.Windows.Forms.Padding(1);
            this.optionsCard.Size = new System.Drawing.Size(612, 106);
            this.optionsCard.TabIndex = 7;
            //
            // nameRow
            //
            this.nameRow.BottomDivider = true;
            this.nameRow.Controls.Add(this.nameTitle);
            this.nameRow.Controls.Add(this.nameHint);
            this.nameRow.Controls.Add(this.nameBox);
            this.nameRow.CornerRadius = 0;
            this.nameRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.nameRow.Location = new System.Drawing.Point(1, 1);
            this.nameRow.Name = "nameRow";
            this.nameRow.Size = new System.Drawing.Size(610, 52);
            this.nameRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.nameRow.TabIndex = 0;
            //
            // nameTitle
            //
            this.nameTitle.Location = new System.Drawing.Point(14, 8);
            this.nameTitle.Name = "nameTitle";
            this.nameTitle.Size = new System.Drawing.Size(300, 19);
            this.nameTitle.SizePx = 13.5F;
            this.nameTitle.TabIndex = 0;
            this.nameTitle.Text = "Name";
            //
            // nameHint
            //
            this.nameHint.Location = new System.Drawing.Point(14, 27);
            this.nameHint.Name = "nameHint";
            this.nameHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.nameHint.Size = new System.Drawing.Size(300, 17);
            this.nameHint.SizePx = 12F;
            this.nameHint.TabIndex = 1;
            this.nameHint.Text = "What the link is called in that folder";
            //
            // nameBox
            //
            this.nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.nameBox.Location = new System.Drawing.Point(336, 10);
            this.nameBox.Name = "nameBox";
            this.nameBox.PlaceholderText = "link name";
            this.nameBox.Size = new System.Drawing.Size(260, 32);
            this.nameBox.TabIndex = 2;
            this.nameBox.TextChanged += new System.EventHandler(this.nameBox_TextChanged);
            //
            // relativeRow
            //
            this.relativeRow.Controls.Add(this.relativeTitle);
            this.relativeRow.Controls.Add(this.relativeHint);
            this.relativeRow.Controls.Add(this.relativeSwitch);
            this.relativeRow.CornerRadius = 0;
            this.relativeRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.relativeRow.Location = new System.Drawing.Point(1, 53);
            this.relativeRow.Name = "relativeRow";
            this.relativeRow.Size = new System.Drawing.Size(610, 52);
            this.relativeRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.relativeRow.TabIndex = 1;
            //
            // relativeTitle
            //
            this.relativeTitle.Location = new System.Drawing.Point(14, 8);
            this.relativeTitle.Name = "relativeTitle";
            this.relativeTitle.Size = new System.Drawing.Size(520, 19);
            this.relativeTitle.SizePx = 13.5F;
            this.relativeTitle.TabIndex = 0;
            this.relativeTitle.Text = "Store a relative path";
            //
            // relativeHint
            //
            this.relativeHint.Location = new System.Drawing.Point(14, 27);
            this.relativeHint.Name = "relativeHint";
            this.relativeHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.relativeHint.Size = new System.Drawing.Size(520, 17);
            this.relativeHint.SizePx = 12F;
            this.relativeHint.TabIndex = 1;
            //
            // relativeSwitch
            //
            this.relativeSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.relativeSwitch.Location = new System.Drawing.Point(556, 16);
            this.relativeSwitch.Name = "relativeSwitch";
            this.relativeSwitch.Size = new System.Drawing.Size(40, 20);
            this.relativeSwitch.TabIndex = 2;
            this.relativeSwitch.CheckedChanged += new System.EventHandler(this.relativeSwitch_CheckedChanged);
            //
            // createButton
            //
            this.createButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.createButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.createButton.CornerRadius = 5;
            this.createButton.Location = new System.Drawing.Point(438, 598);
            this.createButton.Name = "createButton";
            this.createButton.PaddingX = 22;
            this.createButton.Size = new System.Drawing.Size(96, 34);
            this.createButton.TabIndex = 8;
            this.createButton.Text = "Create link";
            this.createButton.TextSizePx = 13.5F;
            this.createButton.Click += new System.EventHandler(this.createButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(542, 598);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(94, 34);
            this.cancelButton.TabIndex = 9;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // SymlinkDialog
            //
            this.ClientSize = new System.Drawing.Size(660, 684);
            this.Controls.Add(this.content);
            this.MinimumSize = new System.Drawing.Size(660, 684);
            this.Name = "SymlinkDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Create symlink";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.content.ResumeLayout(false);
            this.targetCard.ResumeLayout(false);
            this.linkCard.ResumeLayout(false);
            this.treeCard.ResumeLayout(false);
            this.optionsCard.ResumeLayout(false);
            this.relativeRow.ResumeLayout(false);
            this.nameRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.TextLabel headingLabel;
        private FtpClient.Controls.TextLabel bodyLabel;
        private FtpClient.Controls.SurfacePanel targetCard;
        private FtpClient.Controls.TextLabel targetCaption;
        private FtpClient.Controls.TextLabel targetPathLabel;
        private FtpClient.Controls.TextLabel targetDetailLabel;
        private FtpClient.Controls.TextLabel arrowLabel;
        private FtpClient.Controls.SurfacePanel linkCard;
        private FtpClient.Controls.TextLabel linkCaption;
        private FtpClient.Controls.TextLabel linkPathLabel;
        private FtpClient.Controls.TextLabel linkDetailLabel;
        private FtpClient.Controls.TextLabel placeLabel;
        private FtpClient.Controls.SurfacePanel treeCard;
        private FtpClient.Controls.RemoteTreeControl tree;
        private FtpClient.Controls.SurfacePanel optionsCard;
        private FtpClient.Controls.SurfacePanel nameRow;
        private FtpClient.Controls.TextLabel nameTitle;
        private FtpClient.Controls.TextLabel nameHint;
        private ModernWinForms.ModernTextBox nameBox;
        private FtpClient.Controls.SurfacePanel relativeRow;
        private FtpClient.Controls.TextLabel relativeTitle;
        private FtpClient.Controls.TextLabel relativeHint;
        private FtpClient.Controls.ToggleSwitchControl relativeSwitch;
        private FtpClient.Controls.CommandButton createButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
