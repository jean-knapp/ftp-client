using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Deploy;
using FtpClient.Services;

namespace FtpClient.Controls
{
    /// <summary>
    /// The Commits tab's history: 44 px rows with a lane node, the subject over its short sha,
    /// file count and line totals, the author's initials and name, the date, and a deploy chip.
    /// </summary>
    [ToolboxItem(true)]
    public sealed class CommitListControl : VirtualListControl
    {
        private const int Header = 30;
        private const int RowHeight = 44;
        private const int SidePadding = 14;
        private const int Gutter = 26;
        private const int AuthorWidth = 150;
        private const int DateWidth = 130;
        private const int DeployedWidth = 120;

        private readonly List<CommitEntry> _entries = new List<CommitEntry>();

        public CommitListControl()
        {
            EmptyText = "No commits on this branch yet.";
        }

        protected override int HeaderHeight => Header;
        protected override int RowCount => _entries.Count;
        protected override int GetRowHeight(int index) => RowHeight;
        protected override int DefaultScrollStep => RowHeight;

        public CommitEntry EntryAt(int index) => index >= 0 && index < _entries.Count ? _entries[index] : null;

        [Browsable(false)]
        public CommitEntry SelectedEntry => EntryAt(SelectedIndex);

        [Browsable(false)]
        public IReadOnlyList<CommitEntry> Entries => _entries;

        public void SetEntries(IEnumerable<CommitEntry> entries, string selectSha)
        {
            int scroll = ScrollOffset;
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            int index = selectSha == null ? -1 : _entries.FindIndex(e => e.Commit.Sha == selectSha);
            RestoreState(index, scroll);
            ContentChanged();
            if (index >= 0) EnsureVisible(index);
        }

        /// <summary>The Deployed chip was clicked; the view offers to change the commit's state.</summary>
        public event EventHandler<RowMouseEventArgs> DeployChipClick;

        private bool InDeployedColumn(Point point)
        {
            int start = ColumnsStart(Math.Max(0, Width - ScrollBarSpace)) + AuthorWidth + DateWidth;
            return point.X >= start && point.X < start + DeployedWidth;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var entry = EntryAt(RowIndexAt(e.Location));
            Cursor = entry != null && entry.State != DeployState.None && InDeployedColumn(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            int index = RowIndexAt(e.Location);
            if (index >= 0 && InDeployedColumn(e.Location)) DeployChipClick?.Invoke(this, new RowMouseEventArgs(index, e.Location, e.Button));
        }

        private int ColumnsStart(int width) => Math.Max(SidePadding + Gutter + 120, width - SidePadding - AuthorWidth - DateWidth - DeployedWidth);

        protected override void PaintHeader(Graphics g, Rectangle bounds)
        {
            var p = P;
            Draw.Fill(g, bounds, p.FillOn(RowSurface));
            var font = Fonts.Ui(12f);
            int x = ColumnsStart(Math.Max(0, Width - ScrollBarSpace));
            Draw.Text(g, "Commit", font, new Rectangle(SidePadding + Gutter, bounds.Y, 120, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            Draw.Text(g, "Author", font, new Rectangle(x, bounds.Y, AuthorWidth, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            Draw.Text(g, "Date", font, new Rectangle(x + AuthorWidth, bounds.Y, DateWidth, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            Draw.Text(g, "Deployed", font, new Rectangle(x + AuthorWidth + DateWidth, bounds.Y, DeployedWidth, bounds.Height), p.Foreground3, Draw.RightMiddle);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var entry = _entries[index];
            var commit = entry.Commit;
            bool selected = (state & RowState.Selected) != 0;

            var face = surface;
            if (selected)
            {
                face = p.SelectionOn(surface);
                Draw.Fill(g, bounds, face);
                Draw.Fill(g, new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), p.AccentFill);
            }
            else if ((state & RowState.Hot) != 0)
            {
                face = p.HoverOn(surface);
                Draw.Fill(g, bounds, face);
            }

            // The lane node: a 2 px ring on the layer, filled when selected.
            var node = new RectangleF(SidePadding + 4f, bounds.Y + bounds.Height / 2f - 7f, 14f, 14f);
            var oldSmoothing = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fill = new SolidBrush(selected ? p.Lane : p.Layer)) g.FillEllipse(fill, node);
            using (var pen = new Pen(p.Lane, 2f)) g.DrawEllipse(pen, node);
            g.SmoothingMode = oldSmoothing;

            int columns = ColumnsStart(bounds.Width);
            int textX = SidePadding + Gutter;
            int textWidth = Math.Max(0, columns - 16 - textX);

            Draw.Text(g, commit.Subject, Fonts.Ui(13.5f, selected), new Rectangle(textX, bounds.Y + 4, textWidth, 19), p.Foreground, Draw.LeftMiddle);

            // Meta line: sha · files · +added −removed
            int x = textX;
            int metaY = bounds.Y + 23;
            var metaFont = Fonts.Ui(11.5f);
            x = Run(g, commit.ShortSha, Fonts.Code(11.5f), p.Foreground3, x, metaY, columns - 16);
            x = Run(g, "  ·  " + Format.Count(entry.FileCount, "file") + "  ", metaFont, p.Foreground3, x, metaY, columns - 16);
            x = Run(g, "+" + entry.Added.ToString("N0"), metaFont, p.Up, x, metaY, columns - 16);
            Run(g, "  −" + entry.Removed.ToString("N0"), metaFont, p.Error, x, metaY, columns - 16);

            // Author
            int centreY = bounds.Y + bounds.Height / 2;
            var disc = new Rectangle(columns, centreY - 10, 20, 20);
            Draw.Avatar(g, disc, Draw.Initials(commit.AuthorName), Fonts.Ui(8.5f, true), ThemePalette.Tint(p.Lane2, 0.22, face), p.Lane2);
            Draw.Text(g, commit.AuthorName, Fonts.Ui(13f), new Rectangle(columns + 28, bounds.Y, AuthorWidth - 36, bounds.Height), p.Foreground2, Draw.LeftMiddle);

            Draw.Text(g, Format.CommitDate(commit.AuthorDate), Fonts.Ui(13f), new Rectangle(columns + AuthorWidth, bounds.Y, DateWidth - 8, bounds.Height), p.Foreground3, Draw.LeftMiddle);

            DeployChip(g, entry.State, columns + AuthorWidth + DateWidth + DeployedWidth, centreY, face);
        }

        private static int Run(Graphics g, string text, Font font, Color color, int x, int y, int limit)
        {
            if (x >= limit) return x;
            int w = Math.Min(Draw.MeasureWidth(text, font), limit - x);
            Draw.Text(g, text, font, new Rectangle(x, y, w + 1, 16), color, Draw.LeftMiddle);
            return x + w;
        }

        /// <summary>Paints the Deploying / Pending / Deployed chip with its right edge at <paramref name="right"/>.</summary>
        public static void DeployChip(Graphics g, DeployState state, int right, int centreY, Color face)
        {
            var p = Theme.Palette;
            string text;
            Color fill, fore;
            switch (state)
            {
                case DeployState.Deploying:
                    text = "Deploying"; fill = ThemePalette.Tint(p.Accent, 0.18, face); fore = p.Accent; break;
                case DeployState.Deployed:
                    text = "Deployed"; fill = ThemePalette.Tint(p.Up, 0.16, face); fore = p.Up; break;
                case DeployState.Pending:
                    text = "Pending"; fill = p.Fill2On(face); fore = p.Foreground3; break;
                default:
                    return;
            }
            var font = Fonts.Ui(11.5f, true);
            int width = Draw.MeasureWidth(text, font) + 16;
            Draw.Tag(g, right - width, centreY - 10, 20, text, font, fill, fore, 8);
        }
    }
}
