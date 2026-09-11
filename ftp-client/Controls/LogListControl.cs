using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FtpClient.Remote;
using FtpClient.Services;

namespace FtpClient.Controls
{
    /// <summary>
    /// The protocol log: 20 px monospace lines with the time, a coloured STATUS / SEND / RECV /
    /// ERROR tag and the message. Warnings and errors carry a wash; the view follows new lines
    /// while it is scrolled to the bottom.
    /// </summary>
    [ToolboxItem(true)]
    public sealed class LogListControl : VirtualListControl
    {
        private const int RowHeight = 20;
        private const int SidePadding = 12;
        private const int TimeWidth = 86;
        private const int TagWidth = 66;

        private readonly List<LogEntry> _all = new List<LogEntry>();
        private readonly List<LogEntry> _rows = new List<LogEntry>();
        private string _find;

        public LogListControl()
        {
            EmptyText = "Nothing logged yet.";
        }

        protected override int RowCount => _rows.Count;
        protected override int GetRowHeight(int index) => RowHeight;
        protected override int DefaultScrollStep => RowHeight;
        protected override bool MultiSelect => true;

        [Browsable(false)] public bool ShowStatus { get; private set; } = true;
        [Browsable(false)] public bool ShowCommands { get; private set; } = true;
        [Browsable(false)] public bool ShowResponses { get; private set; } = true;
        [Browsable(false)] public bool ShowErrors { get; private set; } = true;

        public void SetFilter(bool status, bool commands, bool responses, bool errors, string find)
        {
            ShowStatus = status;
            ShowCommands = commands;
            ShowResponses = responses;
            ShowErrors = errors;
            _find = string.IsNullOrWhiteSpace(find) ? null : find.Trim();
            Rebuild(true);
        }

        public void SetEntries(IEnumerable<LogEntry> entries)
        {
            _all.Clear();
            if (entries != null) _all.AddRange(entries);
            Rebuild(true);
        }

        public void Append(LogEntry entry)
        {
            if (entry == null) return;
            _all.Add(entry);
            if (_all.Count > 5000) _all.RemoveRange(0, _all.Count - 5000);
            if (!Passes(entry)) return;
            bool follow = ScrollOffset >= TotalHeight - ViewportHeight - RowHeight;
            _rows.Add(entry);
            if (_rows.Count > 5000) _rows.RemoveRange(0, _rows.Count - 5000);
            ContentChanged();
            if (follow) ScrollOffset = int.MaxValue;
        }

        public void Clear()
        {
            _all.Clear();
            Rebuild(false);
        }

        /// <summary>The selected lines, or every visible line when nothing is selected.</summary>
        public string CopyText()
        {
            var lines = SelectedIndices.Count > 0
                ? SelectedIndices.OrderBy(i => i).Where(i => i < _rows.Count).Select(i => _rows[i])
                : _rows;
            var text = new StringBuilder();
            foreach (var entry in lines) text.AppendLine(entry.ToString());
            return text.ToString();
        }

        private bool Passes(LogEntry entry)
        {
            switch (entry.Kind)
            {
                case LogKind.Send: if (!ShowCommands) return false; break;
                case LogKind.Receive: if (!ShowResponses) return false; break;
                case LogKind.Error: if (!ShowErrors) return false; break;
                default: if (!ShowStatus) return false; break;
            }
            return _find == null || entry.Text.IndexOf(_find, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void Rebuild(bool toBottom)
        {
            _rows.Clear();
            _rows.AddRange(_all.Where(Passes));
            RestoreState(-1, 0);
            ContentChanged();
            if (toBottom) ScrollOffset = int.MaxValue;
        }

        public static Color TagColor(LogKind kind)
        {
            var p = Theme.Palette;
            switch (kind)
            {
                case LogKind.Success: return p.Up;
                case LogKind.Warning: return p.Warning;
                case LogKind.Send: return p.Accent;
                case LogKind.Receive: return p.Lane2;
                case LogKind.Error: return p.Error;
                default: return p.Foreground3;
            }
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var entry = _rows[index];

            var face = surface;
            if ((state & RowState.Selected) != 0) face = p.SelectionOn(surface);
            else if (entry.Kind == LogKind.Error) face = ThemePalette.Tint(p.Error, 0.09, surface);
            else if (entry.Kind == LogKind.Warning) face = ThemePalette.Tint(p.Warning, 0.07, surface);
            else if ((state & RowState.Hot) != 0) face = p.HoverOn(surface);
            if (face != surface) Draw.Fill(g, bounds, face);

            var mono = Fonts.Code(12.5f);
            int x = SidePadding;
            Draw.Text(g, entry.Time.ToString("HH:mm:ss"), mono, new Rectangle(x, bounds.Y, TimeWidth, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            x += TimeWidth;
            Draw.Text(g, entry.Tag, Fonts.Code(12.5f, true), new Rectangle(x, bounds.Y, TagWidth, bounds.Height), TagColor(entry.Kind), Draw.LeftMiddle);
            x += TagWidth;
            Draw.Text(g, entry.Text, mono, new Rectangle(x, bounds.Y, Math.Max(0, bounds.Width - x - SidePadding), bounds.Height),
                entry.Kind == LogKind.Error ? p.Error : p.Foreground2, Draw.LeftMiddle);
        }
    }
}
