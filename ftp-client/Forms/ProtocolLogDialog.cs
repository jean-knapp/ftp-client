using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using FtpClient.Controls;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Forms
{
    /// <summary>
    /// Protocol log: every status line, command and response a site's connections produced, with
    /// class filters, find, copy, clear, and a raw command line for protocols that take one.
    /// </summary>
    public partial class ProtocolLogDialog : ModernForm
    {
        private readonly Site _site;
        private readonly SessionLog _log;
        private readonly SiteConnection _connection;
        private readonly ConcurrentQueue<LogEntry> _pending = new ConcurrentQueue<LogEntry>();

        public ProtocolLogDialog(Site site, SessionLog log, SiteConnection connection)
        {
            InitializeComponent();
            Theme.Apply(skin);
            rawBox.OverrideSkinFont = true;
            rawBox.Font = Fonts.Code(12.5f);
            findBox.LeadingSvgIcon = Icons.Search;

            _site = site;
            _log = log ?? new SessionLog();
            _connection = connection;
            Text = "Protocol log — " + (site?.Name ?? "session");

            logList.SetEntries(_log.Snapshot());
            _log.Added += Log_Added;
            _log.Cleared += Log_Cleared;
            UpdateRawCommandState();
            if (_connection != null) _connection.StateChanged += Connection_StateChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ButtonRow.ArrangeRight(content.ClientSize.Width - 14, 4, 0, copyButton, clearButton);
            findBox.Left = copyButton.Left - 8 - findBox.Width;
        }

        public SessionLog Log => _log;

        private void Log_Added(object sender, LogEntry entry)
        {
            // Appends come from worker threads, sometimes thousands a second; the timer drains them.
            _pending.Enqueue(entry);
        }

        private void Log_Cleared(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke((Action)(() =>
            {
                while (_pending.TryDequeue(out _)) { }
                logList.Clear();
            }));
        }

        private void Connection_StateChanged(object sender, EventArgs e)
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke((Action)UpdateRawCommandState);
        }

        private void drainTimer_Tick(object sender, EventArgs e)
        {
            int count = 0;
            while (count < 2000 && _pending.TryDequeue(out var entry))
            {
                logList.Append(entry);
                count++;
            }
        }

        private void UpdateRawCommandState()
        {
            bool supported = _connection != null && _connection.State == ConnectionState.Connected && _connection.SupportsRawCommands;
            rawBox.Enabled = supported;
            sendButton.Enabled = supported;
            rawBox.PlaceholderText = supported
                ? "SITE CHMOD 644 index.php"
                : _connection != null && _connection.State == ConnectionState.Connected
                    ? "This connection does not take raw commands"
                    : "Not connected";
        }

        private void ApplyFilter()
        {
            logList.SetFilter(filterChips.IsActive(0), filterChips.IsActive(1), filterChips.IsActive(2), filterChips.IsActive(3), findBox.Text);
        }

        private void filterChips_ChipsChanged(object sender, EventArgs e) => ApplyFilter();

        private void findBox_TextChanged(object sender, EventArgs e) => ApplyFilter();

        private void copyButton_Click(object sender, EventArgs e)
        {
            var text = logList.CopyText();
            if (string.IsNullOrEmpty(text)) return;
            try { Clipboard.SetText(text); }
            catch (System.Runtime.InteropServices.ExternalException) { }
        }

        private void clearButton_Click(object sender, EventArgs e) => _log.Clear();

        private async void sendButton_Click(object sender, EventArgs e)
        {
            var command = (rawBox.Text ?? string.Empty).Trim();
            if (command.Length == 0 || _connection == null || !sendButton.Enabled) return;
            sendButton.Enabled = false;
            try
            {
                await _connection.RunAsync(s => s.SendRawCommandAsync(command, CancellationToken.None), CancellationToken.None);
                rawBox.Text = string.Empty;
            }
            catch (Exception ex)
            {
                _log.Error(command + ": " + ex.Message);
            }
            finally
            {
                UpdateRawCommandState();
                rawBox.Focus();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && rawBox.ContainsFocus)
            {
                sendButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (keyData == (Keys.Control | Keys.F))
            {
                findBox.Focus();
                return true;
            }
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            drainTimer.Stop();
            _log.Added -= Log_Added;
            _log.Cleared -= Log_Cleared;
            if (_connection != null) _connection.StateChanged -= Connection_StateChanged;
            base.OnFormClosed(e);
        }
    }
}
