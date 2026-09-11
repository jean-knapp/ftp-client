using System;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>Single-line prompt used for branch names, tags and stash messages.</summary>
    public partial class TextInputDialog : ModernForm
    {
        public TextInputDialog()
        {
            InitializeComponent();
            Theme.Apply(skin);
        }

        public string Caption
        {
            get => Text;
            set => Text = value;
        }

        public string Prompt
        {
            get => promptLabel.Text;
            set => promptLabel.Text = value;
        }

        private string _pendingValue;

        public string Value
        {
            // A password is taken exactly as typed; spaces can be part of it.
            get => _pendingValue ?? (Password ? valueBox.Text : valueBox.Text.Trim());
            set
            {
                // ModernTextBox drops text assigned before its edit box exists, so the value waits
                // for the dialog to be shown.
                if (valueBox.IsHandleCreated) valueBox.Text = value ?? string.Empty;
                else _pendingValue = value ?? string.Empty;
                UpdateOkButton();
            }
        }

        /// <summary>When true the OK button stays enabled for an empty value.</summary>
        public bool AllowEmpty { get; set; }

        /// <summary>Masks what is typed, for passwords.</summary>
        public bool Password
        {
            get => valueBox.PasswordChar != '\0';
            set => valueBox.PasswordChar = value ? '●' : '\0';
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_pendingValue != null)
            {
                var value = _pendingValue;
                _pendingValue = null;
                valueBox.Text = value;
            }
            UpdateOkButton();
            valueBox.Focus();
        }

        private void valueBox_TextChanged(object sender, EventArgs e) => UpdateOkButton();

        private void UpdateOkButton() => okButton.Enabled = AllowEmpty || (_pendingValue ?? valueBox.Text).Trim().Length > 0;

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!okButton.Enabled) return;
            DialogResult = DialogResult.OK;
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
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            if (keyData == Keys.Enter)
            {
                okButton_Click(okButton, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
