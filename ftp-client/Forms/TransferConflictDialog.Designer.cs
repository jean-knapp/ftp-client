namespace FtpClient.Forms
{
    partial class TransferConflictDialog
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
            this.warningIcon = new FtpClient.Controls.TextLabel();
            this.titleLabel = new FtpClient.Controls.TextLabel();
            this.pathLabel = new FtpClient.Controls.TextLabel();
            this.incomingCard = new FtpClient.Controls.SurfacePanel();
            this.incomingCaption = new FtpClient.Controls.TextLabel();
            this.incomingSizeCaption = new FtpClient.Controls.TextLabel();
            this.incomingSize = new FtpClient.Controls.TextLabel();
            this.incomingModifiedCaption = new FtpClient.Controls.TextLabel();
            this.incomingModified = new FtpClient.Controls.TextLabel();
            this.incomingVerdict = new FtpClient.Controls.Chip();
            this.existingCard = new FtpClient.Controls.SurfacePanel();
            this.existingCaption = new FtpClient.Controls.TextLabel();
            this.existingSizeCaption = new FtpClient.Controls.TextLabel();
            this.existingSize = new FtpClient.Controls.TextLabel();
            this.existingModifiedCaption = new FtpClient.Controls.TextLabel();
            this.existingModified = new FtpClient.Controls.TextLabel();
            this.existingVerdict = new FtpClient.Controls.Chip();
            this.overwriteOption = new FtpClient.Controls.OptionRow();
            this.newerOption = new FtpClient.Controls.OptionRow();
            this.resumeOption = new FtpClient.Controls.OptionRow();
            this.renameOption = new FtpClient.Controls.OptionRow();
            this.applyAllBox = new FtpClient.Controls.TokenCheckBox();
            this.continueButton = new FtpClient.Controls.CommandButton();
            this.skipButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.content.SuspendLayout();
            this.incomingCard.SuspendLayout();
            this.existingCard.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Size = new System.Drawing.Size(640, 530);
            this.content.Controls.Add(this.warningIcon);
            this.content.Controls.Add(this.titleLabel);
            this.content.Controls.Add(this.pathLabel);
            this.content.Controls.Add(this.incomingCard);
            this.content.Controls.Add(this.existingCard);
            this.content.Controls.Add(this.overwriteOption);
            this.content.Controls.Add(this.newerOption);
            this.content.Controls.Add(this.resumeOption);
            this.content.Controls.Add(this.renameOption);
            this.content.Controls.Add(this.applyAllBox);
            this.content.Controls.Add(this.continueButton);
            this.content.Controls.Add(this.skipButton);
            this.content.Controls.Add(this.cancelButton);
            this.content.CornerRadius = 0;
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Name = "content";
            this.content.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.content.TabIndex = 0;
            //
            // warningIcon
            //
            this.warningIcon.IconGap = 0;
            this.warningIcon.IconRole = FtpClient.Controls.TextRole.Warning;
            this.warningIcon.IconSize = 22;
            this.warningIcon.IconSvg = FtpClient.Controls.Icons.Warning;
            this.warningIcon.Location = new System.Drawing.Point(24, 19);
            this.warningIcon.Name = "warningIcon";
            this.warningIcon.Size = new System.Drawing.Size(22, 26);
            this.warningIcon.TabIndex = 0;
            //
            // titleLabel
            //
            this.titleLabel.Location = new System.Drawing.Point(58, 18);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Semibold = true;
            this.titleLabel.Size = new System.Drawing.Size(558, 26);
            this.titleLabel.SizePx = 18F;
            this.titleLabel.TabIndex = 1;
            //
            // pathLabel
            //
            this.pathLabel.Location = new System.Drawing.Point(58, 44);
            this.pathLabel.Monospace = true;
            this.pathLabel.Name = "pathLabel";
            this.pathLabel.Role = FtpClient.Controls.TextRole.Secondary;
            this.pathLabel.Size = new System.Drawing.Size(558, 20);
            this.pathLabel.SizePx = 13.5F;
            this.pathLabel.TabIndex = 2;
            //
            // incomingCard
            //
            this.incomingCard.Controls.Add(this.incomingCaption);
            this.incomingCard.Controls.Add(this.incomingSizeCaption);
            this.incomingCard.Controls.Add(this.incomingSize);
            this.incomingCard.Controls.Add(this.incomingModifiedCaption);
            this.incomingCard.Controls.Add(this.incomingModified);
            this.incomingCard.Controls.Add(this.incomingVerdict);
            this.incomingCard.CornerRadius = 7;
            this.incomingCard.Location = new System.Drawing.Point(24, 80);
            this.incomingCard.Name = "incomingCard";
            this.incomingCard.Size = new System.Drawing.Size(290, 118);
            this.incomingCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.incomingCard.TabIndex = 3;
            //
            // incomingCaption
            //
            this.incomingCaption.Location = new System.Drawing.Point(14, 12);
            this.incomingCaption.Name = "incomingCaption";
            this.incomingCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.incomingCaption.Semibold = true;
            this.incomingCaption.Size = new System.Drawing.Size(262, 16);
            this.incomingCaption.SizePx = 11F;
            this.incomingCaption.TabIndex = 0;
            this.incomingCaption.Text = "Local — uploading";
            this.incomingCaption.Uppercase = true;
            //
            // incomingSizeCaption
            //
            this.incomingSizeCaption.Location = new System.Drawing.Point(14, 34);
            this.incomingSizeCaption.Name = "incomingSizeCaption";
            this.incomingSizeCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.incomingSizeCaption.Size = new System.Drawing.Size(100, 22);
            this.incomingSizeCaption.SizePx = 12.5F;
            this.incomingSizeCaption.TabIndex = 1;
            this.incomingSizeCaption.Text = "Size";
            //
            // incomingSize
            //
            this.incomingSize.Location = new System.Drawing.Point(116, 34);
            this.incomingSize.Name = "incomingSize";
            this.incomingSize.Size = new System.Drawing.Size(160, 22);
            this.incomingSize.SizePx = 12.5F;
            this.incomingSize.TabIndex = 2;
            this.incomingSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // incomingModifiedCaption
            //
            this.incomingModifiedCaption.Location = new System.Drawing.Point(14, 58);
            this.incomingModifiedCaption.Name = "incomingModifiedCaption";
            this.incomingModifiedCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.incomingModifiedCaption.Size = new System.Drawing.Size(100, 22);
            this.incomingModifiedCaption.SizePx = 12.5F;
            this.incomingModifiedCaption.TabIndex = 3;
            this.incomingModifiedCaption.Text = "Modified";
            //
            // incomingModified
            //
            this.incomingModified.Location = new System.Drawing.Point(116, 58);
            this.incomingModified.Name = "incomingModified";
            this.incomingModified.Size = new System.Drawing.Size(160, 22);
            this.incomingModified.SizePx = 12.5F;
            this.incomingModified.TabIndex = 4;
            this.incomingModified.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // incomingVerdict
            //
            this.incomingVerdict.CornerRadius = 4;
            this.incomingVerdict.Location = new System.Drawing.Point(14, 88);
            this.incomingVerdict.Name = "incomingVerdict";
            this.incomingVerdict.PaddingX = 8;
            this.incomingVerdict.Semibold = false;
            this.incomingVerdict.Size = new System.Drawing.Size(52, 20);
            this.incomingVerdict.TabIndex = 5;
            this.incomingVerdict.TextSizePx = 11.5F;
            //
            // existingCard
            //
            this.existingCard.Controls.Add(this.existingCaption);
            this.existingCard.Controls.Add(this.existingSizeCaption);
            this.existingCard.Controls.Add(this.existingSize);
            this.existingCard.Controls.Add(this.existingModifiedCaption);
            this.existingCard.Controls.Add(this.existingModified);
            this.existingCard.Controls.Add(this.existingVerdict);
            this.existingCard.CornerRadius = 7;
            this.existingCard.Location = new System.Drawing.Point(326, 80);
            this.existingCard.Name = "existingCard";
            this.existingCard.Size = new System.Drawing.Size(290, 118);
            this.existingCard.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.existingCard.TabIndex = 4;
            //
            // existingCaption
            //
            this.existingCaption.Location = new System.Drawing.Point(14, 12);
            this.existingCaption.Name = "existingCaption";
            this.existingCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.existingCaption.Semibold = true;
            this.existingCaption.Size = new System.Drawing.Size(262, 16);
            this.existingCaption.SizePx = 11F;
            this.existingCaption.TabIndex = 0;
            this.existingCaption.Text = "Remote — on server";
            this.existingCaption.Uppercase = true;
            //
            // existingSizeCaption
            //
            this.existingSizeCaption.Location = new System.Drawing.Point(14, 34);
            this.existingSizeCaption.Name = "existingSizeCaption";
            this.existingSizeCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.existingSizeCaption.Size = new System.Drawing.Size(100, 22);
            this.existingSizeCaption.SizePx = 12.5F;
            this.existingSizeCaption.TabIndex = 1;
            this.existingSizeCaption.Text = "Size";
            //
            // existingSize
            //
            this.existingSize.Location = new System.Drawing.Point(116, 34);
            this.existingSize.Name = "existingSize";
            this.existingSize.Size = new System.Drawing.Size(160, 22);
            this.existingSize.SizePx = 12.5F;
            this.existingSize.TabIndex = 2;
            this.existingSize.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // existingModifiedCaption
            //
            this.existingModifiedCaption.Location = new System.Drawing.Point(14, 58);
            this.existingModifiedCaption.Name = "existingModifiedCaption";
            this.existingModifiedCaption.Role = FtpClient.Controls.TextRole.Tertiary;
            this.existingModifiedCaption.Size = new System.Drawing.Size(100, 22);
            this.existingModifiedCaption.SizePx = 12.5F;
            this.existingModifiedCaption.TabIndex = 3;
            this.existingModifiedCaption.Text = "Modified";
            //
            // existingModified
            //
            this.existingModified.Location = new System.Drawing.Point(116, 58);
            this.existingModified.Name = "existingModified";
            this.existingModified.Size = new System.Drawing.Size(160, 22);
            this.existingModified.SizePx = 12.5F;
            this.existingModified.TabIndex = 4;
            this.existingModified.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // existingVerdict
            //
            this.existingVerdict.CornerRadius = 4;
            this.existingVerdict.Location = new System.Drawing.Point(14, 88);
            this.existingVerdict.Name = "existingVerdict";
            this.existingVerdict.PaddingX = 8;
            this.existingVerdict.Semibold = false;
            this.existingVerdict.Size = new System.Drawing.Size(90, 20);
            this.existingVerdict.TabIndex = 5;
            this.existingVerdict.TextSizePx = 11.5F;
            //
            // overwriteOption
            //
            this.overwriteOption.GroupName = "conflict";
            this.overwriteOption.Location = new System.Drawing.Point(24, 212);
            this.overwriteOption.Name = "overwriteOption";
            this.overwriteOption.Size = new System.Drawing.Size(592, 48);
            this.overwriteOption.TabIndex = 5;
            this.overwriteOption.Title = "Overwrite";
            this.overwriteOption.DoubleClick += new System.EventHandler(this.option_DoubleClick);
            //
            // newerOption
            //
            this.newerOption.GroupName = "conflict";
            this.newerOption.Location = new System.Drawing.Point(24, 266);
            this.newerOption.Name = "newerOption";
            this.newerOption.Size = new System.Drawing.Size(592, 48);
            this.newerOption.TabIndex = 6;
            this.newerOption.Title = "Overwrite if newer";
            this.newerOption.DoubleClick += new System.EventHandler(this.option_DoubleClick);
            //
            // resumeOption
            //
            this.resumeOption.GroupName = "conflict";
            this.resumeOption.Location = new System.Drawing.Point(24, 320);
            this.resumeOption.Name = "resumeOption";
            this.resumeOption.Size = new System.Drawing.Size(592, 48);
            this.resumeOption.TabIndex = 7;
            this.resumeOption.Title = "Resume";
            this.resumeOption.DoubleClick += new System.EventHandler(this.option_DoubleClick);
            //
            // renameOption
            //
            this.renameOption.GroupName = "conflict";
            this.renameOption.Location = new System.Drawing.Point(24, 374);
            this.renameOption.Name = "renameOption";
            this.renameOption.Size = new System.Drawing.Size(592, 48);
            this.renameOption.TabIndex = 8;
            this.renameOption.Title = "Rename";
            this.renameOption.DoubleClick += new System.EventHandler(this.option_DoubleClick);
            //
            // applyAllBox
            //
            this.applyAllBox.Location = new System.Drawing.Point(24, 436);
            this.applyAllBox.Name = "applyAllBox";
            this.applyAllBox.Size = new System.Drawing.Size(592, 22);
            this.applyAllBox.TabIndex = 9;
            this.applyAllBox.Text = "Apply to the remaining conflicts in this queue";
            //
            // continueButton
            //
            this.continueButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.continueButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.continueButton.CornerRadius = 5;
            this.continueButton.Location = new System.Drawing.Point(336, 474);
            this.continueButton.Name = "continueButton";
            this.continueButton.PaddingX = 20;
            this.continueButton.Size = new System.Drawing.Size(96, 34);
            this.continueButton.TabIndex = 10;
            this.continueButton.Text = "Continue";
            this.continueButton.TextSizePx = 13.5F;
            this.continueButton.Click += new System.EventHandler(this.continueButton_Click);
            //
            // skipButton
            //
            this.skipButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.skipButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.skipButton.CornerRadius = 5;
            this.skipButton.Location = new System.Drawing.Point(440, 474);
            this.skipButton.Name = "skipButton";
            this.skipButton.PaddingX = 20;
            this.skipButton.Size = new System.Drawing.Size(88, 34);
            this.skipButton.TabIndex = 11;
            this.skipButton.Text = "Skip file";
            this.skipButton.TextSizePx = 13.5F;
            this.skipButton.Click += new System.EventHandler(this.skipButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(536, 474);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(80, 34);
            this.cancelButton.TabIndex = 12;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // TransferConflictDialog
            //
            this.ClientSize = new System.Drawing.Size(640, 562);
            this.Controls.Add(this.content);
            this.Name = "TransferConflictDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File already exists";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.content.ResumeLayout(false);
            this.incomingCard.ResumeLayout(false);
            this.existingCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.TextLabel warningIcon;
        private FtpClient.Controls.TextLabel titleLabel;
        private FtpClient.Controls.TextLabel pathLabel;
        private FtpClient.Controls.SurfacePanel incomingCard;
        private FtpClient.Controls.TextLabel incomingCaption;
        private FtpClient.Controls.TextLabel incomingSizeCaption;
        private FtpClient.Controls.TextLabel incomingSize;
        private FtpClient.Controls.TextLabel incomingModifiedCaption;
        private FtpClient.Controls.TextLabel incomingModified;
        private FtpClient.Controls.Chip incomingVerdict;
        private FtpClient.Controls.SurfacePanel existingCard;
        private FtpClient.Controls.TextLabel existingCaption;
        private FtpClient.Controls.TextLabel existingSizeCaption;
        private FtpClient.Controls.TextLabel existingSize;
        private FtpClient.Controls.TextLabel existingModifiedCaption;
        private FtpClient.Controls.TextLabel existingModified;
        private FtpClient.Controls.Chip existingVerdict;
        private FtpClient.Controls.OptionRow overwriteOption;
        private FtpClient.Controls.OptionRow newerOption;
        private FtpClient.Controls.OptionRow resumeOption;
        private FtpClient.Controls.OptionRow renameOption;
        private FtpClient.Controls.TokenCheckBox applyAllBox;
        private FtpClient.Controls.CommandButton continueButton;
        private FtpClient.Controls.CommandButton skipButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
