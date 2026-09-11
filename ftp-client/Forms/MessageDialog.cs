using System;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>A themed replacement for MessageBox with one to three buttons.</summary>
    public partial class MessageDialog : ModernForm
    {
        public MessageDialog()
        {
            InitializeComponent();
            Theme.Apply(skin);
        }

        public string Message
        {
            get => messageLabel.Text;
            set => messageLabel.Text = value ?? string.Empty;
        }

        public string PrimaryText
        {
            get => primaryButton.Text;
            set => primaryButton.Text = value;
        }

        public string SecondaryText
        {
            get => secondaryButton.Text;
            set
            {
                secondaryButton.Text = value;
                secondaryButton.Visible = !string.IsNullOrEmpty(value);
            }
        }

        public string CancelText
        {
            get => cancelButton.Text;
            set
            {
                cancelButton.Text = value;
                cancelButton.Visible = !string.IsNullOrEmpty(value);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutDialog();
        }

        /// <summary>
        /// Sizes the dialog to its message and each button to its caption. Everything is laid out
        /// inside the docked content panel, which already excludes the title bar.
        /// </summary>
        private void LayoutDialog()
        {
            const int edge = 24;
            const int gap = 8;
            const int buttonHeight = 34;
            var buttons = new[] { cancelButton, secondaryButton, primaryButton };

            int required = edge * 2;
            foreach (var button in buttons)
            {
                if (!button.Visible) continue;
                button.Width = Math.Max(92, button.PreferredWidth);
                required += button.Width + gap;
            }

            int width = Math.Max(ClientSize.Width, Math.Min(760, required));
            var messageSize = TextRenderer.MeasureText(messageLabel.Text, Fonts.Ui(13.5f),
                new Size(width - edge * 2, int.MaxValue), TextFormatFlags.WordBreak);
            int messageHeight = Math.Max(40, messageSize.Height + 6);
            int contentHeight = 22 + messageHeight + 20 + buttonHeight + 20;
            contentHeight = Math.Min(560, contentHeight);
            ClientSize = new Size(width, contentHeight + ModernTitleBar.BarHeight + 1);   // +1 for the title bar's top inset

            messageLabel.SetBounds(edge, 22, content.ClientSize.Width - edge * 2, messageHeight);

            int right = content.ClientSize.Width - edge;
            int y = content.ClientSize.Height - 20 - buttonHeight;
            foreach (var button in buttons)
            {
                if (!button.Visible) continue;
                button.SetBounds(right - button.Width, y, button.Width, buttonHeight);
                right -= button.Width + gap;
            }
        }

        private void primaryButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void secondaryButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = cancelButton.Visible ? DialogResult.Cancel : DialogResult.OK;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                DialogResult = DialogResult.OK;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }

    /// <summary>Convenience wrappers around <see cref="MessageDialog"/>.</summary>
    public static class Dialogs
    {
        public static void Information(IWin32Window owner, string title, string message) => Show(owner, title, message, "OK", null, null);

        public static void Warning(IWin32Window owner, string title, string message) => Show(owner, title, message, "OK", null, null);

        public static void Error(IWin32Window owner, string title, string message) => Show(owner, title, message, "OK", null, null);

        public static bool Confirm(IWin32Window owner, string title, string message, string confirmText)
        {
            return Show(owner, title, message, confirmText ?? "OK", null, "Cancel") == DialogResult.OK;
        }

        public static DialogResult Show(IWin32Window owner, string title, string message, string primary, string secondary, string cancel)
        {
            using (var dialog = new MessageDialog())
            {
                dialog.Text = title;
                dialog.Message = message;
                dialog.PrimaryText = primary;
                dialog.SecondaryText = secondary;
                dialog.CancelText = cancel;
                return owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
            }
        }
    }
}
