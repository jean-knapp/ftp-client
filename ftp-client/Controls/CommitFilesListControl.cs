using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Deploy;
using FtpClient.Git;
using FtpClient.Services;

namespace FtpClient.Controls
{
    /// <summary>
    /// FILES IN THIS COMMIT: a checkbox, the change letter, the file name over the remote path it
    /// lands on, its size and what the upload will do to it. Ignored files stay unticked.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("IncludedChanged")]
    public sealed class CommitFilesListControl : VirtualListControl
    {
        private const int RowHeight = 44;
        private const int SidePadding = 14;
        private const int Gap = 10;
        private const int BoxSize = 18;
        private const int BadgeSize = 20;

        private readonly List<DeployFile> _files = new List<DeployFile>();
        private int _hotBox = -1;

        /// <summary>A file was ticked or unticked.</summary>
        public event EventHandler IncludedChanged;

        public CommitFilesListControl()
        {
            EmptyText = "Select a commit to see the files it changed.";
        }

        protected override int RowCount => _files.Count;
        protected override int GetRowHeight(int index) => RowHeight;
        protected override int DefaultScrollStep => RowHeight;

        [Browsable(false)]
        public IReadOnlyList<DeployFile> Files => _files;

        public void SetFiles(IEnumerable<DeployFile> files)
        {
            _files.Clear();
            if (files != null) _files.AddRange(files);
            RestoreState(-1, 0);
            ContentChanged();
        }

        public void SetAllIncluded(bool included)
        {
            bool changed = false;
            foreach (var file in _files)
            {
                bool value = included && file.CanInclude;
                if (file.Included == value) continue;
                file.Included = value;
                changed = true;
            }
            Invalidate();
            if (changed) IncludedChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Toggle(int index)
        {
            if (index < 0 || index >= _files.Count) return;
            var file = _files[index];
            if (!file.CanInclude) return;
            file.Included = !file.Included;
            Invalidate();
            IncludedChanged?.Invoke(this, EventArgs.Empty);
        }

        private static Rectangle BoxBounds(int top) => new Rectangle(SidePadding, top + (RowHeight - BoxSize) / 2, BoxSize, BoxSize);

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var file = _files[index];

            var face = surface;
            if ((state & RowState.Selected) != 0 && (state & RowState.Focused) != 0) face = p.SelectionOn(surface);
            else if ((state & RowState.Hot) != 0) face = p.HoverOn(surface);
            if (face != surface) Draw.Fill(g, bounds, face);

            // Checkbox
            var box = BoxBounds(bounds.Y);
            if (file.Included)
            {
                Draw.FillRounded(g, box, 4f, p.AccentFill);
                IconCache.DrawCentered(g, Icons.Check, 13, p.AccentForeground, box.X + box.Width / 2, box.Y + box.Height / 2);
            }
            else
            {
                if (index == _hotBox && file.CanInclude) Draw.FillRounded(g, box, 4f, p.HoverOn(face));
                var outline = file.CanInclude ? p.Foreground3 : p.StrokeOn(face);
                Draw.DrawRounded(g, new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1), 4f, outline, 1.5f);
            }

            // Change letter
            int x = box.Right + Gap;
            string letter;
            Color tone;
            double strength = 0.16;
            switch (file.Kind)
            {
                case FileChangeKind.Added:
                case FileChangeKind.Copied:
                case FileChangeKind.Untracked:
                    letter = "A"; tone = p.Up; break;
                case FileChangeKind.Deleted:
                    letter = "D"; tone = p.Error; break;
                case FileChangeKind.Renamed:
                    letter = "R"; tone = p.Folder; strength = 0.18; break;
                default:
                    letter = "M"; tone = p.Folder; strength = 0.18; break;
            }
            var badge = new Rectangle(x, bounds.Y + (RowHeight - BadgeSize) / 2, BadgeSize, BadgeSize);
            Draw.FillRounded(g, badge, 4f, ThemePalette.Tint(tone, strength, face));
            Draw.Text(g, letter, Fonts.Ui(10f, true), badge, tone, Draw.CenterMiddle);
            x = badge.Right + Gap;

            // Action chip, then size, from the right.
            int right = bounds.Right - SidePadding;
            string action;
            Color chipFill, chipFore;
            switch (file.Action)
            {
                case DeployAction.New: action = "New"; chipFill = ThemePalette.Tint(p.Up, 0.16, face); chipFore = p.Up; break;
                case DeployAction.Delete: action = "Delete"; chipFill = ThemePalette.Tint(p.Error, 0.16, face); chipFore = p.Error; break;
                case DeployAction.Ignored: action = "Ignored"; chipFill = face; chipFore = p.Foreground3; break;
                default: action = "Overwrite"; chipFill = p.Fill2On(face); chipFore = p.Foreground2; break;
            }
            var chipFont = Fonts.Ui(11.5f, true);
            int chipWidth = Draw.MeasureWidth(action, chipFont) + 16;
            int centreY = bounds.Y + bounds.Height / 2;
            Draw.Tag(g, right - chipWidth, centreY - 10, 20, action, chipFont, chipFill, chipFore, 8);
            right -= chipWidth + 12;

            var sizeFont = Fonts.Ui(12f);
            var size = file.Size.HasValue && file.Action != DeployAction.Delete ? Format.Bytes(file.Size.Value) : "—";
            int sizeWidth = Draw.MeasureWidth(size, sizeFont);
            Draw.Text(g, size, sizeFont, new Rectangle(right - sizeWidth, bounds.Y, sizeWidth + 1, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            right -= sizeWidth + 12;

            int textWidth = Math.Max(0, right - x);
            bool muted = !file.Included;
            Draw.Text(g, file.FileName, Fonts.Ui(13.5f), new Rectangle(x, bounds.Y + 4, textWidth, 19), muted ? p.Foreground3 : p.Foreground, Draw.LeftMiddle);
            var detail = file.Action == DeployAction.Ignored && !string.IsNullOrEmpty(file.Note) ? file.Note : file.RemotePath ?? file.RepoPath;
            Draw.Text(g, detail, Fonts.Code(11.5f), new Rectangle(x, bounds.Y + 23, textWidth, 16), p.Foreground3, Draw.LeftMiddle);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = RowIndexAt(e.Location);
            int hot = index >= 0 && e.X < BoxBounds(0).Right + Gap ? index : -1;
            if (hot == _hotBox) return;
            _hotBox = hot;
            Invalidate();
        }

        protected override void OnRowClicked(int index, MouseEventArgs e)
        {
            base.OnRowClicked(index, e);
            // The box and the space before the letter badge toggle the file.
            if (e.X < BoxBounds(0).Right + Gap) Toggle(index);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int index = RowIndexAt(e.Location);
            if (index >= 0 && e.X >= BoxBounds(0).Right + Gap) Toggle(index);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                Toggle(SelectedIndex);
                return;
            }
            base.OnKeyDown(e);
        }
    }
}
