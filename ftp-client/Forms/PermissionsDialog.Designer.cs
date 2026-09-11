namespace FtpClient.Forms
{
    partial class PermissionsDialog
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
            this.iconTile = new FtpClient.Controls.SurfacePanel();
            this.iconLabel = new FtpClient.Controls.TextLabel();
            this.nameLabel = new FtpClient.Controls.TextLabel();
            this.pathLabel = new FtpClient.Controls.TextLabel();
            this.gridCard = new FtpClient.Controls.SurfacePanel();
            this.publicRow = new FtpClient.Controls.SurfacePanel();
            this.publicTitle = new FtpClient.Controls.TextLabel();
            this.publicDetail = new FtpClient.Controls.TextLabel();
            this.publicRead = new FtpClient.Controls.TokenCheckBox();
            this.publicWrite = new FtpClient.Controls.TokenCheckBox();
            this.publicExecute = new FtpClient.Controls.TokenCheckBox();
            this.groupRow = new FtpClient.Controls.SurfacePanel();
            this.groupTitle = new FtpClient.Controls.TextLabel();
            this.groupDetail = new FtpClient.Controls.TextLabel();
            this.groupRead = new FtpClient.Controls.TokenCheckBox();
            this.groupWrite = new FtpClient.Controls.TokenCheckBox();
            this.groupExecute = new FtpClient.Controls.TokenCheckBox();
            this.ownerRow = new FtpClient.Controls.SurfacePanel();
            this.ownerTitle = new FtpClient.Controls.TextLabel();
            this.ownerDetail = new FtpClient.Controls.TextLabel();
            this.ownerRead = new FtpClient.Controls.TokenCheckBox();
            this.ownerWrite = new FtpClient.Controls.TokenCheckBox();
            this.ownerExecute = new FtpClient.Controls.TokenCheckBox();
            this.gridHeader = new FtpClient.Controls.SurfacePanel();
            this.whoLabel = new FtpClient.Controls.TextLabel();
            this.readLabel = new FtpClient.Controls.TextLabel();
            this.writeLabel = new FtpClient.Controls.TextLabel();
            this.executeLabel = new FtpClient.Controls.TextLabel();
            this.numericLabel = new FtpClient.Controls.TextLabel();
            this.octalBox = new ModernWinForms.ModernTextBox();
            this.symbolicLabel = new FtpClient.Controls.TextLabel();
            this.ownershipLabel = new FtpClient.Controls.TextLabel();
            this.recurseBox = new FtpClient.Controls.TokenCheckBox();
            this.modeNote = new FtpClient.Controls.TextLabel();
            this.localNote = new FtpClient.Controls.TextLabel();
            this.applyButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.content.SuspendLayout();
            this.iconTile.SuspendLayout();
            this.gridCard.SuspendLayout();
            this.publicRow.SuspendLayout();
            this.groupRow.SuspendLayout();
            this.ownerRow.SuspendLayout();
            this.gridHeader.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Size = new System.Drawing.Size(580, 418);
            this.content.Controls.Add(this.iconTile);
            this.content.Controls.Add(this.nameLabel);
            this.content.Controls.Add(this.pathLabel);
            this.content.Controls.Add(this.gridCard);
            this.content.Controls.Add(this.numericLabel);
            this.content.Controls.Add(this.octalBox);
            this.content.Controls.Add(this.symbolicLabel);
            this.content.Controls.Add(this.ownershipLabel);
            this.content.Controls.Add(this.recurseBox);
            this.content.Controls.Add(this.modeNote);
            this.content.Controls.Add(this.localNote);
            this.content.Controls.Add(this.applyButton);
            this.content.Controls.Add(this.cancelButton);
            this.content.CornerRadius = 0;
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Name = "content";
            this.content.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.content.TabIndex = 0;
            //
            // iconTile
            //
            this.iconTile.Controls.Add(this.iconLabel);
            this.iconTile.Location = new System.Drawing.Point(24, 20);
            this.iconTile.Name = "iconTile";
            this.iconTile.Size = new System.Drawing.Size(40, 40);
            this.iconTile.Surface = FtpClient.Controls.SurfaceKind.Fill;
            this.iconTile.TabIndex = 0;
            //
            // iconLabel
            //
            this.iconLabel.IconGap = 0;
            this.iconLabel.IconSize = 18;
            this.iconLabel.IconSvg = FtpClient.Controls.Icons.FileDetailed;
            this.iconLabel.Location = new System.Drawing.Point(11, 0);
            this.iconLabel.Name = "iconLabel";
            this.iconLabel.Size = new System.Drawing.Size(18, 40);
            this.iconLabel.TabIndex = 0;
            //
            // nameLabel
            //
            this.nameLabel.Location = new System.Drawing.Point(76, 17);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Semibold = true;
            this.nameLabel.Size = new System.Drawing.Size(480, 25);
            this.nameLabel.SizePx = 18F;
            this.nameLabel.TabIndex = 1;
            this.nameLabel.Text = "index.php";
            //
            // pathLabel
            //
            this.pathLabel.Location = new System.Drawing.Point(76, 42);
            this.pathLabel.Monospace = true;
            this.pathLabel.Name = "pathLabel";
            this.pathLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.pathLabel.Size = new System.Drawing.Size(480, 18);
            this.pathLabel.SizePx = 12.5F;
            this.pathLabel.TabIndex = 2;
            //
            // gridCard
            //
            this.gridCard.Controls.Add(this.publicRow);
            this.gridCard.Controls.Add(this.groupRow);
            this.gridCard.Controls.Add(this.ownerRow);
            this.gridCard.Controls.Add(this.gridHeader);
            this.gridCard.CornerRadius = 7;
            this.gridCard.Location = new System.Drawing.Point(24, 80);
            this.gridCard.Name = "gridCard";
            this.gridCard.Padding = new System.Windows.Forms.Padding(1);
            this.gridCard.Size = new System.Drawing.Size(532, 168);
            this.gridCard.TabIndex = 3;
            //
            // gridHeader
            //
            this.gridHeader.BottomDivider = true;
            this.gridHeader.Controls.Add(this.whoLabel);
            this.gridHeader.Controls.Add(this.readLabel);
            this.gridHeader.Controls.Add(this.writeLabel);
            this.gridHeader.Controls.Add(this.executeLabel);
            this.gridHeader.CornerRadius = 0;
            this.gridHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.gridHeader.Location = new System.Drawing.Point(1, 1);
            this.gridHeader.Name = "gridHeader";
            this.gridHeader.Size = new System.Drawing.Size(530, 34);
            this.gridHeader.Surface = FtpClient.Controls.SurfaceKind.None;
            this.gridHeader.TabIndex = 0;
            //
            // whoLabel
            //
            this.whoLabel.Location = new System.Drawing.Point(14, 0);
            this.whoLabel.Name = "whoLabel";
            this.whoLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.whoLabel.Size = new System.Drawing.Size(120, 34);
            this.whoLabel.SizePx = 12.5F;
            this.whoLabel.TabIndex = 0;
            this.whoLabel.Text = "Who";
            //
            // readLabel
            //
            this.readLabel.Location = new System.Drawing.Point(294, 0);
            this.readLabel.Name = "readLabel";
            this.readLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.readLabel.Size = new System.Drawing.Size(74, 34);
            this.readLabel.SizePx = 12.5F;
            this.readLabel.TabIndex = 1;
            this.readLabel.Text = "Read";
            this.readLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // writeLabel
            //
            this.writeLabel.Location = new System.Drawing.Point(368, 0);
            this.writeLabel.Name = "writeLabel";
            this.writeLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.writeLabel.Size = new System.Drawing.Size(74, 34);
            this.writeLabel.SizePx = 12.5F;
            this.writeLabel.TabIndex = 2;
            this.writeLabel.Text = "Write";
            this.writeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // executeLabel
            //
            this.executeLabel.Location = new System.Drawing.Point(442, 0);
            this.executeLabel.Name = "executeLabel";
            this.executeLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.executeLabel.Size = new System.Drawing.Size(74, 34);
            this.executeLabel.SizePx = 12.5F;
            this.executeLabel.TabIndex = 3;
            this.executeLabel.Text = "Execute";
            this.executeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // ownerRow
            //
            this.ownerRow.BottomDivider = true;
            this.ownerRow.Controls.Add(this.ownerTitle);
            this.ownerRow.Controls.Add(this.ownerDetail);
            this.ownerRow.Controls.Add(this.ownerRead);
            this.ownerRow.Controls.Add(this.ownerWrite);
            this.ownerRow.Controls.Add(this.ownerExecute);
            this.ownerRow.CornerRadius = 0;
            this.ownerRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.ownerRow.Location = new System.Drawing.Point(1, 35);
            this.ownerRow.Name = "ownerRow";
            this.ownerRow.Size = new System.Drawing.Size(530, 44);
            this.ownerRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.ownerRow.TabIndex = 1;
            //
            // ownerTitle
            //
            this.ownerTitle.Location = new System.Drawing.Point(14, 5);
            this.ownerTitle.Name = "ownerTitle";
            this.ownerTitle.Size = new System.Drawing.Size(200, 19);
            this.ownerTitle.SizePx = 13.5F;
            this.ownerTitle.TabIndex = 0;
            this.ownerTitle.Text = "Owner";
            //
            // ownerDetail
            //
            this.ownerDetail.Location = new System.Drawing.Point(14, 23);
            this.ownerDetail.Name = "ownerDetail";
            this.ownerDetail.Role = FtpClient.Controls.TextRole.Tertiary;
            this.ownerDetail.Size = new System.Drawing.Size(200, 16);
            this.ownerDetail.SizePx = 11.5F;
            this.ownerDetail.TabIndex = 1;
            //
            // ownerRead
            //
            this.ownerRead.Location = new System.Drawing.Point(322, 11);
            this.ownerRead.Name = "ownerRead";
            this.ownerRead.Size = new System.Drawing.Size(18, 22);
            this.ownerRead.TabIndex = 2;
            //
            // ownerWrite
            //
            this.ownerWrite.Location = new System.Drawing.Point(396, 11);
            this.ownerWrite.Name = "ownerWrite";
            this.ownerWrite.Size = new System.Drawing.Size(18, 22);
            this.ownerWrite.TabIndex = 3;
            //
            // ownerExecute
            //
            this.ownerExecute.Location = new System.Drawing.Point(470, 11);
            this.ownerExecute.Name = "ownerExecute";
            this.ownerExecute.Size = new System.Drawing.Size(18, 22);
            this.ownerExecute.TabIndex = 4;
            //
            // groupRow
            //
            this.groupRow.BottomDivider = true;
            this.groupRow.Controls.Add(this.groupTitle);
            this.groupRow.Controls.Add(this.groupDetail);
            this.groupRow.Controls.Add(this.groupRead);
            this.groupRow.Controls.Add(this.groupWrite);
            this.groupRow.Controls.Add(this.groupExecute);
            this.groupRow.CornerRadius = 0;
            this.groupRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupRow.Location = new System.Drawing.Point(1, 79);
            this.groupRow.Name = "groupRow";
            this.groupRow.Size = new System.Drawing.Size(530, 44);
            this.groupRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.groupRow.TabIndex = 2;
            //
            // groupTitle
            //
            this.groupTitle.Location = new System.Drawing.Point(14, 5);
            this.groupTitle.Name = "groupTitle";
            this.groupTitle.Size = new System.Drawing.Size(200, 19);
            this.groupTitle.SizePx = 13.5F;
            this.groupTitle.TabIndex = 0;
            this.groupTitle.Text = "Group";
            //
            // groupDetail
            //
            this.groupDetail.Location = new System.Drawing.Point(14, 23);
            this.groupDetail.Name = "groupDetail";
            this.groupDetail.Role = FtpClient.Controls.TextRole.Tertiary;
            this.groupDetail.Size = new System.Drawing.Size(200, 16);
            this.groupDetail.SizePx = 11.5F;
            this.groupDetail.TabIndex = 1;
            //
            // groupRead
            //
            this.groupRead.Location = new System.Drawing.Point(322, 11);
            this.groupRead.Name = "groupRead";
            this.groupRead.Size = new System.Drawing.Size(18, 22);
            this.groupRead.TabIndex = 2;
            //
            // groupWrite
            //
            this.groupWrite.Location = new System.Drawing.Point(396, 11);
            this.groupWrite.Name = "groupWrite";
            this.groupWrite.Size = new System.Drawing.Size(18, 22);
            this.groupWrite.TabIndex = 3;
            //
            // groupExecute
            //
            this.groupExecute.Location = new System.Drawing.Point(470, 11);
            this.groupExecute.Name = "groupExecute";
            this.groupExecute.Size = new System.Drawing.Size(18, 22);
            this.groupExecute.TabIndex = 4;
            //
            // publicRow
            //
            this.publicRow.Controls.Add(this.publicTitle);
            this.publicRow.Controls.Add(this.publicDetail);
            this.publicRow.Controls.Add(this.publicRead);
            this.publicRow.Controls.Add(this.publicWrite);
            this.publicRow.Controls.Add(this.publicExecute);
            this.publicRow.CornerRadius = 0;
            this.publicRow.Dock = System.Windows.Forms.DockStyle.Top;
            this.publicRow.Location = new System.Drawing.Point(1, 123);
            this.publicRow.Name = "publicRow";
            this.publicRow.Size = new System.Drawing.Size(530, 44);
            this.publicRow.Surface = FtpClient.Controls.SurfaceKind.None;
            this.publicRow.TabIndex = 3;
            //
            // publicTitle
            //
            this.publicTitle.Location = new System.Drawing.Point(14, 5);
            this.publicTitle.Name = "publicTitle";
            this.publicTitle.Size = new System.Drawing.Size(200, 19);
            this.publicTitle.SizePx = 13.5F;
            this.publicTitle.TabIndex = 0;
            this.publicTitle.Text = "Public";
            //
            // publicDetail
            //
            this.publicDetail.Location = new System.Drawing.Point(14, 23);
            this.publicDetail.Name = "publicDetail";
            this.publicDetail.Role = FtpClient.Controls.TextRole.Tertiary;
            this.publicDetail.Size = new System.Drawing.Size(200, 16);
            this.publicDetail.SizePx = 11.5F;
            this.publicDetail.TabIndex = 1;
            this.publicDetail.Text = "everyone else";
            //
            // publicRead
            //
            this.publicRead.Location = new System.Drawing.Point(322, 11);
            this.publicRead.Name = "publicRead";
            this.publicRead.Size = new System.Drawing.Size(18, 22);
            this.publicRead.TabIndex = 2;
            //
            // publicWrite
            //
            this.publicWrite.Location = new System.Drawing.Point(396, 11);
            this.publicWrite.Name = "publicWrite";
            this.publicWrite.Size = new System.Drawing.Size(18, 22);
            this.publicWrite.TabIndex = 3;
            //
            // publicExecute
            //
            this.publicExecute.Location = new System.Drawing.Point(470, 11);
            this.publicExecute.Name = "publicExecute";
            this.publicExecute.Size = new System.Drawing.Size(18, 22);
            this.publicExecute.TabIndex = 4;
            //
            // numericLabel
            //
            this.numericLabel.Location = new System.Drawing.Point(24, 266);
            this.numericLabel.Name = "numericLabel";
            this.numericLabel.Size = new System.Drawing.Size(60, 32);
            this.numericLabel.SizePx = 13.5F;
            this.numericLabel.TabIndex = 4;
            this.numericLabel.Text = "Numeric";
            //
            // octalBox
            //
            this.octalBox.Location = new System.Drawing.Point(90, 266);
            this.octalBox.MaxLength = 4;
            this.octalBox.Name = "octalBox";
            this.octalBox.Size = new System.Drawing.Size(96, 32);
            this.octalBox.TabIndex = 5;
            this.octalBox.TextChanged += new System.EventHandler(this.octalBox_TextChanged);
            //
            // symbolicLabel
            //
            this.symbolicLabel.Location = new System.Drawing.Point(198, 266);
            this.symbolicLabel.Monospace = true;
            this.symbolicLabel.Name = "symbolicLabel";
            this.symbolicLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.symbolicLabel.Size = new System.Drawing.Size(110, 32);
            this.symbolicLabel.SizePx = 12.5F;
            this.symbolicLabel.TabIndex = 6;
            this.symbolicLabel.Text = "-rw-r--r--";
            //
            // ownershipLabel
            //
            this.ownershipLabel.Location = new System.Drawing.Point(310, 266);
            this.ownershipLabel.Name = "ownershipLabel";
            this.ownershipLabel.Role = FtpClient.Controls.TextRole.Tertiary;
            this.ownershipLabel.Size = new System.Drawing.Size(246, 32);
            this.ownershipLabel.SizePx = 12.5F;
            this.ownershipLabel.TabIndex = 7;
            this.ownershipLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // recurseBox
            //
            this.recurseBox.Location = new System.Drawing.Point(24, 312);
            this.recurseBox.Name = "recurseBox";
            this.recurseBox.Size = new System.Drawing.Size(532, 22);
            this.recurseBox.TabIndex = 8;
            this.recurseBox.Text = "Recurse into subdirectories (files inside get the same bits without execute)";
            //
            // modeNote
            //
            this.modeNote.Location = new System.Drawing.Point(24, 340);
            this.modeNote.Name = "modeNote";
            this.modeNote.Role = FtpClient.Controls.TextRole.Tertiary;
            this.modeNote.Size = new System.Drawing.Size(532, 18);
            this.modeNote.SizePx = 12F;
            this.modeNote.TabIndex = 9;
            this.modeNote.Text = "The selected items have different permissions; Apply gives them all these.";
            this.modeNote.Visible = false;
            //
            // localNote
            //
            this.localNote.IconGap = 6;
            this.localNote.IconRole = FtpClient.Controls.TextRole.Warning;
            this.localNote.IconSize = 13;
            this.localNote.IconSvg = FtpClient.Controls.Icons.Warning;
            this.localNote.Location = new System.Drawing.Point(24, 340);
            this.localNote.Name = "localNote";
            this.localNote.Role = FtpClient.Controls.TextRole.Warning;
            this.localNote.Size = new System.Drawing.Size(532, 18);
            this.localNote.SizePx = 12F;
            this.localNote.TabIndex = 10;
            this.localNote.Text = "Windows folders only keep the owner's write bit (read-only).";
            this.localNote.Visible = false;
            //
            // applyButton
            //
            this.applyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.applyButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.applyButton.CornerRadius = 5;
            this.applyButton.Location = new System.Drawing.Point(378, 364);
            this.applyButton.Name = "applyButton";
            this.applyButton.PaddingX = 22;
            this.applyButton.Size = new System.Drawing.Size(76, 34);
            this.applyButton.TabIndex = 11;
            this.applyButton.Text = "Apply";
            this.applyButton.TextSizePx = 13.5F;
            this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(462, 364);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(94, 34);
            this.cancelButton.TabIndex = 12;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // PermissionsDialog
            //
            this.ClientSize = new System.Drawing.Size(580, 450);
            this.Controls.Add(this.content);
            this.MinimumSize = new System.Drawing.Size(580, 450);
            this.Name = "PermissionsDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Properties";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.content.ResumeLayout(false);
            this.iconTile.ResumeLayout(false);
            this.gridCard.ResumeLayout(false);
            this.publicRow.ResumeLayout(false);
            this.groupRow.ResumeLayout(false);
            this.ownerRow.ResumeLayout(false);
            this.gridHeader.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.SurfacePanel iconTile;
        private FtpClient.Controls.TextLabel iconLabel;
        private FtpClient.Controls.TextLabel nameLabel;
        private FtpClient.Controls.TextLabel pathLabel;
        private FtpClient.Controls.SurfacePanel gridCard;
        private FtpClient.Controls.SurfacePanel gridHeader;
        private FtpClient.Controls.TextLabel whoLabel;
        private FtpClient.Controls.TextLabel readLabel;
        private FtpClient.Controls.TextLabel writeLabel;
        private FtpClient.Controls.TextLabel executeLabel;
        private FtpClient.Controls.SurfacePanel ownerRow;
        private FtpClient.Controls.TextLabel ownerTitle;
        private FtpClient.Controls.TextLabel ownerDetail;
        private FtpClient.Controls.TokenCheckBox ownerRead;
        private FtpClient.Controls.TokenCheckBox ownerWrite;
        private FtpClient.Controls.TokenCheckBox ownerExecute;
        private FtpClient.Controls.SurfacePanel groupRow;
        private FtpClient.Controls.TextLabel groupTitle;
        private FtpClient.Controls.TextLabel groupDetail;
        private FtpClient.Controls.TokenCheckBox groupRead;
        private FtpClient.Controls.TokenCheckBox groupWrite;
        private FtpClient.Controls.TokenCheckBox groupExecute;
        private FtpClient.Controls.SurfacePanel publicRow;
        private FtpClient.Controls.TextLabel publicTitle;
        private FtpClient.Controls.TextLabel publicDetail;
        private FtpClient.Controls.TokenCheckBox publicRead;
        private FtpClient.Controls.TokenCheckBox publicWrite;
        private FtpClient.Controls.TokenCheckBox publicExecute;
        private FtpClient.Controls.TextLabel numericLabel;
        private ModernWinForms.ModernTextBox octalBox;
        private FtpClient.Controls.TextLabel symbolicLabel;
        private FtpClient.Controls.TextLabel ownershipLabel;
        private FtpClient.Controls.TokenCheckBox recurseBox;
        private FtpClient.Controls.TextLabel modeNote;
        private FtpClient.Controls.TextLabel localNote;
        private FtpClient.Controls.CommandButton applyButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
