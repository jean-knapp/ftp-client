namespace FtpClient.Forms
{
    partial class ProtocolLogDialog
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
            this.filterChips = new FtpClient.Controls.FilterChipGroup();
            this.findBox = new ModernWinForms.ModernTextBox();
            this.copyButton = new FtpClient.Controls.CommandButton();
            this.clearButton = new FtpClient.Controls.CommandButton();
            this.logCard = new FtpClient.Controls.SurfacePanel();
            this.logList = new FtpClient.Controls.LogListControl();
            this.rawLabel = new FtpClient.Controls.TextLabel();
            this.rawBox = new ModernWinForms.ModernTextBox();
            this.sendButton = new FtpClient.Controls.CommandButton();
            this.drainTimer = new System.Windows.Forms.Timer(this.components);
            this.content.SuspendLayout();
            this.logCard.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Size = new System.Drawing.Size(1000, 620);
            this.content.Controls.Add(this.filterChips);
            this.content.Controls.Add(this.findBox);
            this.content.Controls.Add(this.copyButton);
            this.content.Controls.Add(this.clearButton);
            this.content.Controls.Add(this.logCard);
            this.content.Controls.Add(this.rawLabel);
            this.content.Controls.Add(this.rawBox);
            this.content.Controls.Add(this.sendButton);
            this.content.CornerRadius = 0;
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Name = "content";
            this.content.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.content.TabIndex = 0;
            //
            // filterChips
            //
            this.filterChips.Chips.Add(new FtpClient.Controls.FilterChip("Status", FtpClient.Controls.TextRole.Tertiary));
            this.filterChips.Chips.Add(new FtpClient.Controls.FilterChip("Commands", FtpClient.Controls.TextRole.Accent));
            this.filterChips.Chips.Add(new FtpClient.Controls.FilterChip("Responses", FtpClient.Controls.TextRole.Lane2));
            this.filterChips.Chips.Add(new FtpClient.Controls.FilterChip("Errors", FtpClient.Controls.TextRole.Error));
            this.filterChips.Location = new System.Drawing.Point(14, 14);
            this.filterChips.Name = "filterChips";
            this.filterChips.Size = new System.Drawing.Size(420, 28);
            this.filterChips.TabIndex = 0;
            this.filterChips.ChipsChanged += new System.EventHandler(this.filterChips_ChipsChanged);
            //
            // findBox
            //
            this.findBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.findBox.Location = new System.Drawing.Point(626, 13);
            this.findBox.Name = "findBox";
            this.findBox.PlaceholderText = "Find in log…";
            this.findBox.Size = new System.Drawing.Size(200, 30);
            this.findBox.TabIndex = 1;
            this.findBox.TextChanged += new System.EventHandler(this.findBox_TextChanged);
            //
            // copyButton
            //
            this.copyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.copyButton.IconSize = 13;
            this.copyButton.IconSvg = FtpClient.Controls.Icons.Copy;
            this.copyButton.Location = new System.Drawing.Point(834, 14);
            this.copyButton.Name = "copyButton";
            this.copyButton.Size = new System.Drawing.Size(70, 28);
            this.copyButton.TabIndex = 2;
            this.copyButton.Text = "Copy";
            this.copyButton.Click += new System.EventHandler(this.copyButton_Click);
            //
            // clearButton
            //
            this.clearButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clearButton.IconSize = 13;
            this.clearButton.IconSvg = FtpClient.Controls.Icons.Delete;
            this.clearButton.Location = new System.Drawing.Point(910, 14);
            this.clearButton.Name = "clearButton";
            this.clearButton.Size = new System.Drawing.Size(74, 28);
            this.clearButton.TabIndex = 3;
            this.clearButton.Text = "Clear";
            this.clearButton.Click += new System.EventHandler(this.clearButton_Click);
            //
            // logCard
            //
            this.logCard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.logCard.Controls.Add(this.logList);
            this.logCard.Location = new System.Drawing.Point(14, 54);
            this.logCard.Name = "logCard";
            this.logCard.Padding = new System.Windows.Forms.Padding(1, 6, 1, 6);
            this.logCard.Size = new System.Drawing.Size(972, 506);
            this.logCard.TabIndex = 4;
            //
            // logList
            //
            this.logList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logList.Location = new System.Drawing.Point(1, 6);
            this.logList.Name = "logList";
            this.logList.Size = new System.Drawing.Size(970, 494);
            this.logList.TabIndex = 0;
            //
            // rawLabel
            //
            this.rawLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.rawLabel.Location = new System.Drawing.Point(14, 574);
            this.rawLabel.Name = "rawLabel";
            this.rawLabel.Role = FtpClient.Controls.TextRole.Secondary;
            this.rawLabel.Size = new System.Drawing.Size(88, 28);
            this.rawLabel.TabIndex = 5;
            this.rawLabel.Text = "Raw command";
            //
            // rawBox
            //
            this.rawBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rawBox.Location = new System.Drawing.Point(106, 574);
            this.rawBox.Name = "rawBox";
            this.rawBox.Size = new System.Drawing.Size(812, 28);
            this.rawBox.TabIndex = 6;
            //
            // sendButton
            //
            this.sendButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.sendButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.sendButton.Location = new System.Drawing.Point(926, 574);
            this.sendButton.Name = "sendButton";
            this.sendButton.PaddingX = 14;
            this.sendButton.Size = new System.Drawing.Size(60, 28);
            this.sendButton.TabIndex = 7;
            this.sendButton.Text = "Send";
            this.sendButton.Click += new System.EventHandler(this.sendButton_Click);
            //
            // drainTimer
            //
            this.drainTimer.Enabled = true;
            this.drainTimer.Interval = 100;
            this.drainTimer.Tick += new System.EventHandler(this.drainTimer_Tick);
            //
            // ProtocolLogDialog
            //
            this.ClientSize = new System.Drawing.Size(1000, 652);
            this.Controls.Add(this.content);
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "ProtocolLogDialog";
            this.Skin = this.skin;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Protocol log";
            this.content.ResumeLayout(false);
            this.logCard.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.FilterChipGroup filterChips;
        private ModernWinForms.ModernTextBox findBox;
        private FtpClient.Controls.CommandButton copyButton;
        private FtpClient.Controls.CommandButton clearButton;
        private FtpClient.Controls.SurfacePanel logCard;
        private FtpClient.Controls.LogListControl logList;
        private FtpClient.Controls.TextLabel rawLabel;
        private ModernWinForms.ModernTextBox rawBox;
        private FtpClient.Controls.CommandButton sendButton;
        private System.Windows.Forms.Timer drainTimer;
    }
}
