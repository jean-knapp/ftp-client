namespace FtpClient.Forms
{
    partial class PairFolderDialog
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
            this.localCard = new FtpClient.Controls.SurfacePanel();
            this.localCaption = new FtpClient.Controls.TextLabel();
            this.localPathLabel = new FtpClient.Controls.TextLabel();
            this.localDetailLabel = new FtpClient.Controls.TextLabel();
            this.linkLabel = new FtpClient.Controls.TextLabel();
            this.remoteCard = new FtpClient.Controls.SurfacePanel();
            this.remoteCaption = new FtpClient.Controls.TextLabel();
            this.remotePathLabel = new FtpClient.Controls.TextLabel();
            this.remoteDetailLabel = new FtpClient.Controls.TextLabel();
            this.chooseFolderButton = new FtpClient.Controls.CommandButton();
            this.useCurrentButton = new FtpClient.Controls.CommandButton();
            this.optionsCard = new FtpClient.Controls.SurfacePanel();
            this.confirmRow = new FtpClient.Controls.SurfacePanel();
            this.confirmTitle = new FtpClient.Controls.TextLabel();
            this.confirmHint = new FtpClient.Controls.TextLabel();
            this.confirmSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.buildRow = new FtpClient.Controls.SurfacePanel();
            this.buildTitle = new FtpClient.Controls.TextLabel();
            this.buildHint = new FtpClient.Controls.TextLabel();
            this.buildBox = new ModernWinForms.ModernTextBox();
            this.deletionsRow = new FtpClient.Controls.SurfacePanel();
            this.deletionsTitle = new FtpClient.Controls.TextLabel();
            this.deletionsHint = new FtpClient.Controls.TextLabel();
            this.deletionsSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.honourRow = new FtpClient.Controls.SurfacePanel();
            this.honourTitle = new FtpClient.Controls.TextLabel();
            this.honourHint = new FtpClient.Controls.TextLabel();
            this.honourSwitch = new FtpClient.Controls.ToggleSwitchControl();
            this.unpairButton = new FtpClient.Controls.CommandButton();
            this.pairButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.content.SuspendLayout();
            this.localCard.SuspendLayout();
            this.remoteCard.SuspendLayout();
            this.optionsCard.SuspendLayout();
            this.confirmRow.SuspendLayout();
            this.buildRow.SuspendLayout();
            this.deletionsRow.SuspendLayout();
            this.honourRow.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Size = new System.Drawing.Size(660, 552);
            this.content.Controls.Add(this.headingLabel);
            this.content.Controls.Add(this.bodyLabel);
            this.content.Controls.Add(this.localCard);
            this.content.Controls.Add(this.linkLabel);
            this.content.Controls.Add(this.remoteCard);
            this.content.Controls.Add(this.chooseFolderButton);
            this.content.Controls.Add(this.useCurrentButton);
            this.content.Controls.Add(this.optionsCard);
            this.content.Controls.Add(this.unpairButton);
            this.content.Controls.Add(this.pairButton);
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
            this.headingLabel.Text = "Pair a local folder with this directory";
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
            this.bodyLabel.Text = "Once paired, the Commits tab lists the repository\'s history and can upload just the files a commit touched.";
            this.bodyLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // localCard
            //
            this.localCard.Controls.Add(this.localCaption);
            this.localCard.Controls.Add(this.localPathLabel);
            this.localCard.Controls.Add(this.localDetailLabel);
            this.localCard.CornerRadius = 7;
            this.localCard.Location = new System.Drawing.Point(24, 104);
            this.localCard.Name = "localCard";
            this.localCard.Size = new System.Drawing.Size(280, 92);
            this.localCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.localCard.TabIndex = 2;
            //
            // localCaption
            //
            this.localCaption.Location = new System.Drawing.Point(14, 12);
            this.localCaption.Name = "localCaption";
            this.localCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.localCaption.Semibold = true;
            this.localCaption.Size = new System.Drawing.Size(250, 16);
            this.localCaption.SizePx = 11F;
            this.localCaption.TabIndex = 0;
            this.localCaption.Text = "Local root";
            this.localCaption.Uppercase = true;
            //
            // localPathLabel
            //
            this.localPathLabel.IconGap = 10;
            this.localPathLabel.IconRole = FtpClient.Controls.TextRole.Folder;
            this.localPathLabel.IconSize = 14;
            this.localPathLabel.IconSvg = FtpClient.Controls.Icons.Folder;
            this.localPathLabel.Location = new System.Drawing.Point(14, 32);
            this.localPathLabel.Monospace = true;
            this.localPathLabel.Name = "localPathLabel";
            this.localPathLabel.Size = new System.Drawing.Size(252, 22);
            this.localPathLabel.SizePx = 13.5F;
            this.localPathLabel.TabIndex = 1;
            this.localPathLabel.Text = "No folder chosen";
            //
            // localDetailLabel
            //
            this.localDetailLabel.IconGap = 6;
            this.localDetailLabel.IconRole = FtpClient.Controls.TextRole.Lane;
            this.localDetailLabel.IconSize = 12;
            this.localDetailLabel.IconSvg = FtpClient.Controls.Icons.Branch;
            this.localDetailLabel.Location = new System.Drawing.Point(14, 60);
            this.localDetailLabel.Name = "localDetailLabel";
            this.localDetailLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.localDetailLabel.Size = new System.Drawing.Size(252, 18);
            this.localDetailLabel.SizePx = 12F;
            this.localDetailLabel.TabIndex = 2;
            //
            // linkLabel
            //
            this.linkLabel.IconGap = 0;
            this.linkLabel.IconRole = FtpClient.Controls.TextRole.Lane2;
            this.linkLabel.IconSize = 22;
            this.linkLabel.IconSvg = FtpClient.Controls.Icons.Link;
            this.linkLabel.Location = new System.Drawing.Point(319, 139);
            this.linkLabel.Name = "linkLabel";
            this.linkLabel.Size = new System.Drawing.Size(22, 22);
            this.linkLabel.TabIndex = 3;
            //
            // remoteCard
            //
            this.remoteCard.Controls.Add(this.remoteCaption);
            this.remoteCard.Controls.Add(this.remotePathLabel);
            this.remoteCard.Controls.Add(this.remoteDetailLabel);
            this.remoteCard.CornerRadius = 7;
            this.remoteCard.Location = new System.Drawing.Point(356, 104);
            this.remoteCard.Name = "remoteCard";
            this.remoteCard.Size = new System.Drawing.Size(280, 92);
            this.remoteCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.remoteCard.TabIndex = 4;
            //
            // remoteCaption
            //
            this.remoteCaption.Location = new System.Drawing.Point(14, 12);
            this.remoteCaption.Name = "remoteCaption";
            this.remoteCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.remoteCaption.Semibold = true;
            this.remoteCaption.Size = new System.Drawing.Size(250, 16);
            this.remoteCaption.SizePx = 11F;
            this.remoteCaption.TabIndex = 0;
            this.remoteCaption.Text = "Remote root";
            this.remoteCaption.Uppercase = true;
            //
            // remotePathLabel
            //
            this.remotePathLabel.IconGap = 10;
            this.remotePathLabel.IconRole = FtpClient.Controls.TextRole.Lane;
            this.remotePathLabel.IconSize = 14;
            this.remotePathLabel.IconSvg = FtpClient.Controls.Icons.Server;
            this.remotePathLabel.Location = new System.Drawing.Point(14, 32);
            this.remotePathLabel.Monospace = true;
            this.remotePathLabel.Name = "remotePathLabel";
            this.remotePathLabel.Size = new System.Drawing.Size(252, 22);
            this.remotePathLabel.SizePx = 13.5F;
            this.remotePathLabel.TabIndex = 1;
            this.remotePathLabel.Text = "/";
            //
            // remoteDetailLabel
            //
            this.remoteDetailLabel.Location = new System.Drawing.Point(14, 60);
            this.remoteDetailLabel.Name = "remoteDetailLabel";
            this.remoteDetailLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.remoteDetailLabel.Size = new System.Drawing.Size(252, 18);
            this.remoteDetailLabel.SizePx = 12F;
            this.remoteDetailLabel.TabIndex = 2;
            //
            // chooseFolderButton
            //
            this.chooseFolderButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.chooseFolderButton.CornerRadius = 5;
            this.chooseFolderButton.IconSize = 14;
            this.chooseFolderButton.IconSvg = FtpClient.Controls.Icons.Folder;
            this.chooseFolderButton.Location = new System.Drawing.Point(24, 212);
            this.chooseFolderButton.Name = "chooseFolderButton";
            this.chooseFolderButton.PaddingX = 14;
            this.chooseFolderButton.Size = new System.Drawing.Size(190, 32);
            this.chooseFolderButton.TabIndex = 5;
            this.chooseFolderButton.Text = "Choose another folder…";
            this.chooseFolderButton.TextSizePx = 13.5F;
            this.chooseFolderButton.Click += new System.EventHandler(this.chooseFolderButton_Click);
            //
            // useCurrentButton
            //
            this.useCurrentButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.useCurrentButton.CornerRadius = 5;
            this.useCurrentButton.Location = new System.Drawing.Point(222, 212);
            this.useCurrentButton.Name = "useCurrentButton";
            this.useCurrentButton.PaddingX = 14;
            this.useCurrentButton.Size = new System.Drawing.Size(200, 32);
            this.useCurrentButton.TabIndex = 6;
            this.useCurrentButton.Text = "Use current remote directory";
            this.useCurrentButton.TextSizePx = 13.5F;
            this.useCurrentButton.Click += new System.EventHandler(this.useCurrentButton_Click);
            //
            // optionsCard
            //
            this.optionsCard.Controls.Add(this.confirmRow);
            this.optionsCard.Controls.Add(this.buildRow);
            this.optionsCard.Controls.Add(this.deletionsRow);
            this.optionsCard.Controls.Add(this.honourRow);
            this.optionsCard.CornerRadius = 7;
            this.optionsCard.Location = new System.Drawing.Point(24, 262);
            this.optionsCard.Name = "optionsCard";
            this.optionsCard.Padding = new System.Windows.Forms.Padding(1);
            this.optionsCard.Size = new System.Drawing.Size(612, 210);
            this.optionsCard.TabIndex = 7;
            //
            // honourRow
            //
            this.honourRow.BottomDivider = true;
            this.honourRow.Controls.Add(this.honourTitle);
            this.honourRow.Controls.Add(this.honourHint);
            this.honourRow.Controls.Add(this.honourSwitch);
            this.honourRow.CornerRadius = 0;
            this.honourRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.honourRow.Location = new System.Drawing.Point(1, 1);
            this.honourRow.Name = "honourRow";
            this.honourRow.Size = new System.Drawing.Size(610, 52);
            this.honourRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.honourRow.TabIndex = 0;
            //
            // honourTitle
            //
            this.honourTitle.Location = new System.Drawing.Point(14, 8);
            this.honourTitle.Name = "honourTitle";
            this.honourTitle.Size = new System.Drawing.Size(420, 19);
            this.honourTitle.SizePx = 13.5F;
            this.honourTitle.TabIndex = 0;
            this.honourTitle.Text = "Honour .gitignore and .ftpignore";
            //
            // honourHint
            //
            this.honourHint.Location = new System.Drawing.Point(14, 27);
            this.honourHint.Name = "honourHint";
            this.honourHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.honourHint.Size = new System.Drawing.Size(420, 17);
            this.honourHint.SizePx = 12F;
            this.honourHint.TabIndex = 1;
            this.honourHint.Text = "Skip ignored paths when uploading a commit";
            //
            // honourSwitch
            //
            this.honourSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.honourSwitch.Location = new System.Drawing.Point(556, 16);
            this.honourSwitch.Name = "honourSwitch";
            this.honourSwitch.Size = new System.Drawing.Size(40, 20);
            this.honourSwitch.TabIndex = 2;
            //
            // deletionsRow
            //
            this.deletionsRow.BottomDivider = true;
            this.deletionsRow.Controls.Add(this.deletionsTitle);
            this.deletionsRow.Controls.Add(this.deletionsHint);
            this.deletionsRow.Controls.Add(this.deletionsSwitch);
            this.deletionsRow.CornerRadius = 0;
            this.deletionsRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.deletionsRow.Location = new System.Drawing.Point(1, 53);
            this.deletionsRow.Name = "deletionsRow";
            this.deletionsRow.Size = new System.Drawing.Size(610, 52);
            this.deletionsRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.deletionsRow.TabIndex = 1;
            //
            // deletionsTitle
            //
            this.deletionsTitle.Location = new System.Drawing.Point(14, 8);
            this.deletionsTitle.Name = "deletionsTitle";
            this.deletionsTitle.Size = new System.Drawing.Size(420, 19);
            this.deletionsTitle.SizePx = 13.5F;
            this.deletionsTitle.TabIndex = 0;
            this.deletionsTitle.Text = "Apply deletions";
            //
            // deletionsHint
            //
            this.deletionsHint.Location = new System.Drawing.Point(14, 27);
            this.deletionsHint.Name = "deletionsHint";
            this.deletionsHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.deletionsHint.Size = new System.Drawing.Size(420, 17);
            this.deletionsHint.SizePx = 12F;
            this.deletionsHint.TabIndex = 1;
            this.deletionsHint.Text = "Remove files a commit deleted from the remote";
            //
            // deletionsSwitch
            //
            this.deletionsSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.deletionsSwitch.Location = new System.Drawing.Point(556, 16);
            this.deletionsSwitch.Name = "deletionsSwitch";
            this.deletionsSwitch.Size = new System.Drawing.Size(40, 20);
            this.deletionsSwitch.TabIndex = 2;
            //
            // buildRow
            //
            this.buildRow.BottomDivider = true;
            this.buildRow.Controls.Add(this.buildTitle);
            this.buildRow.Controls.Add(this.buildHint);
            this.buildRow.Controls.Add(this.buildBox);
            this.buildRow.CornerRadius = 0;
            this.buildRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.buildRow.Location = new System.Drawing.Point(1, 105);
            this.buildRow.Name = "buildRow";
            this.buildRow.Size = new System.Drawing.Size(610, 52);
            this.buildRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.buildRow.TabIndex = 2;
            //
            // buildTitle
            //
            this.buildTitle.Location = new System.Drawing.Point(14, 8);
            this.buildTitle.Name = "buildTitle";
            this.buildTitle.Size = new System.Drawing.Size(360, 19);
            this.buildTitle.SizePx = 13.5F;
            this.buildTitle.TabIndex = 0;
            this.buildTitle.Text = "Build output directory";
            //
            // buildHint
            //
            this.buildHint.Location = new System.Drawing.Point(14, 27);
            this.buildHint.Name = "buildHint";
            this.buildHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.buildHint.Size = new System.Drawing.Size(360, 17);
            this.buildHint.SizePx = 12F;
            this.buildHint.TabIndex = 1;
            this.buildHint.Text = "Upload from here instead of the repository root";
            //
            // buildBox
            //
            this.buildBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buildBox.Location = new System.Drawing.Point(396, 10);
            this.buildBox.Name = "buildBox";
            this.buildBox.PlaceholderText = "repository root";
            this.buildBox.Size = new System.Drawing.Size(200, 32);
            this.buildBox.TabIndex = 2;
            //
            // confirmRow
            //
            this.confirmRow.Controls.Add(this.confirmTitle);
            this.confirmRow.Controls.Add(this.confirmHint);
            this.confirmRow.Controls.Add(this.confirmSwitch);
            this.confirmRow.CornerRadius = 0;
            this.confirmRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.confirmRow.Location = new System.Drawing.Point(1, 157);
            this.confirmRow.Name = "confirmRow";
            this.confirmRow.Size = new System.Drawing.Size(610, 52);
            this.confirmRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.confirmRow.TabIndex = 3;
            //
            // confirmTitle
            //
            this.confirmTitle.Location = new System.Drawing.Point(14, 8);
            this.confirmTitle.Name = "confirmTitle";
            this.confirmTitle.Size = new System.Drawing.Size(420, 19);
            this.confirmTitle.SizePx = 13.5F;
            this.confirmTitle.TabIndex = 0;
            this.confirmTitle.Text = "Confirm before each deploy";
            //
            // confirmHint
            //
            this.confirmHint.Location = new System.Drawing.Point(14, 27);
            this.confirmHint.Name = "confirmHint";
            this.confirmHint.Role = FtpClient.Controls.TextRole.Tertiary;
            this.confirmHint.Size = new System.Drawing.Size(420, 17);
            this.confirmHint.SizePx = 12F;
            this.confirmHint.TabIndex = 1;
            this.confirmHint.Text = "Show the file list before the transfer starts";
            //
            // confirmSwitch
            //
            this.confirmSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.confirmSwitch.Location = new System.Drawing.Point(556, 16);
            this.confirmSwitch.Name = "confirmSwitch";
            this.confirmSwitch.Size = new System.Drawing.Size(40, 20);
            this.confirmSwitch.TabIndex = 2;
            //
            // unpairButton
            //
            this.unpairButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.unpairButton.CornerRadius = 5;
            this.unpairButton.IconRole = FtpClient.Controls.TextRole.Error;
            this.unpairButton.IconSize = 14;
            this.unpairButton.IconSvg = FtpClient.Controls.Icons.Cross;
            this.unpairButton.Location = new System.Drawing.Point(24, 494);
            this.unpairButton.Name = "unpairButton";
            this.unpairButton.PaddingX = 12;
            this.unpairButton.Size = new System.Drawing.Size(96, 34);
            this.unpairButton.TabIndex = 8;
            this.unpairButton.Text = "Unpair";
            this.unpairButton.TextSizePx = 13.5F;
            this.unpairButton.Visible = false;
            this.unpairButton.Click += new System.EventHandler(this.unpairButton_Click);
            //
            // pairButton
            //
            this.pairButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.pairButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.pairButton.CornerRadius = 5;
            this.pairButton.Enabled = false;
            this.pairButton.Location = new System.Drawing.Point(464, 494);
            this.pairButton.Name = "pairButton";
            this.pairButton.PaddingX = 22;
            this.pairButton.Size = new System.Drawing.Size(70, 34);
            this.pairButton.TabIndex = 9;
            this.pairButton.Text = "Pair";
            this.pairButton.TextSizePx = 13.5F;
            this.pairButton.Click += new System.EventHandler(this.pairButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(542, 494);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(94, 34);
            this.cancelButton.TabIndex = 10;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // PairFolderDialog
            //
            this.ClientSize = new System.Drawing.Size(660, 584);
            this.Controls.Add(this.content);
            this.MinimumSize = new System.Drawing.Size(660, 584);
            this.Name = "PairFolderDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pair local folder";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.content.ResumeLayout(false);
            this.localCard.ResumeLayout(false);
            this.remoteCard.ResumeLayout(false);
            this.optionsCard.ResumeLayout(false);
            this.confirmRow.ResumeLayout(false);
            this.buildRow.ResumeLayout(false);
            this.deletionsRow.ResumeLayout(false);
            this.honourRow.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.TextLabel headingLabel;
        private FtpClient.Controls.TextLabel bodyLabel;
        private FtpClient.Controls.SurfacePanel localCard;
        private FtpClient.Controls.TextLabel localCaption;
        private FtpClient.Controls.TextLabel localPathLabel;
        private FtpClient.Controls.TextLabel localDetailLabel;
        private FtpClient.Controls.TextLabel linkLabel;
        private FtpClient.Controls.SurfacePanel remoteCard;
        private FtpClient.Controls.TextLabel remoteCaption;
        private FtpClient.Controls.TextLabel remotePathLabel;
        private FtpClient.Controls.TextLabel remoteDetailLabel;
        private FtpClient.Controls.CommandButton chooseFolderButton;
        private FtpClient.Controls.CommandButton useCurrentButton;
        private FtpClient.Controls.SurfacePanel optionsCard;
        private FtpClient.Controls.SurfacePanel honourRow;
        private FtpClient.Controls.TextLabel honourTitle;
        private FtpClient.Controls.TextLabel honourHint;
        private FtpClient.Controls.ToggleSwitchControl honourSwitch;
        private FtpClient.Controls.SurfacePanel deletionsRow;
        private FtpClient.Controls.TextLabel deletionsTitle;
        private FtpClient.Controls.TextLabel deletionsHint;
        private FtpClient.Controls.ToggleSwitchControl deletionsSwitch;
        private FtpClient.Controls.SurfacePanel buildRow;
        private FtpClient.Controls.TextLabel buildTitle;
        private FtpClient.Controls.TextLabel buildHint;
        private ModernWinForms.ModernTextBox buildBox;
        private FtpClient.Controls.SurfacePanel confirmRow;
        private FtpClient.Controls.TextLabel confirmTitle;
        private FtpClient.Controls.TextLabel confirmHint;
        private FtpClient.Controls.ToggleSwitchControl confirmSwitch;
        private FtpClient.Controls.CommandButton unpairButton;
        private FtpClient.Controls.CommandButton pairButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
