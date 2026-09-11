namespace FtpClient.Forms
{
    partial class MessageDialog
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
            this.messageLabel = new FtpClient.Controls.TextLabel();
            this.primaryButton = new FtpClient.Controls.CommandButton();
            this.secondaryButton = new FtpClient.Controls.CommandButton();
            this.cancelButton = new FtpClient.Controls.CommandButton();
            this.content.SuspendLayout();
            this.SuspendLayout();
            //
            // content
            //
            this.content.Controls.Add(this.messageLabel);
            this.content.Controls.Add(this.primaryButton);
            this.content.Controls.Add(this.secondaryButton);
            this.content.Controls.Add(this.cancelButton);
            this.content.CornerRadius = 0;
            this.content.Dock = System.Windows.Forms.DockStyle.Fill;
            this.content.Location = new System.Drawing.Point(0, 32);
            this.content.Name = "content";
            this.content.Size = new System.Drawing.Size(500, 162);
            this.content.Surface = FtpClient.Controls.SurfaceKind.Base;
            this.content.TabIndex = 0;
            //
            // messageLabel
            //
            this.messageLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messageLabel.Location = new System.Drawing.Point(24, 20);
            this.messageLabel.MultiLine = true;
            this.messageLabel.Name = "messageLabel";
            this.messageLabel.Role = FtpClient.Controls.TextRole.Secondary;
            this.messageLabel.Size = new System.Drawing.Size(452, 104);
            this.messageLabel.SizePx = 13.5F;
            this.messageLabel.TabIndex = 0;
            this.messageLabel.Text = "Message";
            this.messageLabel.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            //
            // primaryButton
            //
            this.primaryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.primaryButton.Appearance = FtpClient.Controls.ButtonAppearance.Accent;
            this.primaryButton.CornerRadius = 5;
            this.primaryButton.Location = new System.Drawing.Point(180, 140);
            this.primaryButton.Name = "primaryButton";
            this.primaryButton.PaddingX = 20;
            this.primaryButton.Size = new System.Drawing.Size(92, 34);
            this.primaryButton.TabIndex = 1;
            this.primaryButton.Text = "OK";
            this.primaryButton.TextSizePx = 13.5F;
            this.primaryButton.Click += new System.EventHandler(this.primaryButton_Click);
            //
            // secondaryButton
            //
            this.secondaryButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.secondaryButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.secondaryButton.CornerRadius = 5;
            this.secondaryButton.Location = new System.Drawing.Point(280, 140);
            this.secondaryButton.Name = "secondaryButton";
            this.secondaryButton.PaddingX = 20;
            this.secondaryButton.Size = new System.Drawing.Size(92, 34);
            this.secondaryButton.TabIndex = 2;
            this.secondaryButton.Text = "No";
            this.secondaryButton.TextSizePx = 13.5F;
            this.secondaryButton.Visible = false;
            this.secondaryButton.Click += new System.EventHandler(this.secondaryButton_Click);
            //
            // cancelButton
            //
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.Appearance = FtpClient.Controls.ButtonAppearance.Standard;
            this.cancelButton.CornerRadius = 5;
            this.cancelButton.Location = new System.Drawing.Point(380, 140);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.PaddingX = 20;
            this.cancelButton.Size = new System.Drawing.Size(96, 34);
            this.cancelButton.TabIndex = 3;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.TextSizePx = 13.5F;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            //
            // MessageDialog
            //
            this.ClientSize = new System.Drawing.Size(500, 194);
            this.Controls.Add(this.content);
            this.MinimumSize = new System.Drawing.Size(360, 160);
            this.Name = "MessageDialog";
            this.Skin = this.skin;
            this.Sizeable = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Git Client";
            this.TitleBar.ShowMaximizeBox = false;
            this.TitleBar.ShowMinimizeBox = false;
            this.content.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private ModernWinForms.ModernSkin skin;
        private FtpClient.Controls.SurfacePanel content;
        private FtpClient.Controls.TextLabel messageLabel;
        private FtpClient.Controls.CommandButton primaryButton;
        private FtpClient.Controls.CommandButton secondaryButton;
        private FtpClient.Controls.CommandButton cancelButton;
    }
}
