using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The grid view of a directory: 120 × 104 px tiles with a 32 px glyph, the name on up to two
    /// lines and its size or item count. It shows the rows the list view has already filtered and
    /// sorted, so both views always agree.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("EntryActivated")]
    public sealed class RemoteTileControl : VirtualListControl
    {
        private const int TileWidth = 120;
        private const int TileHeight = 104;
        private const int Gap = 8;
        private const int Padding12 = 12;

        private readonly List<RemoteEntry> _rows = new List<RemoteEntry>();
        private readonly HashSet<int> _selected = new HashSet<int>();
        private string _directory = RemotePath.Root;
        private int _anchor = -1;
        private int _focus = -1;
        private int _hotItem = -1;
        private int _dropItem = -1;
        private bool _dragOverView;

        public event EventHandler<RemoteEntryEventArgs> EntryActivated;
        public event EventHandler ItemSelectionChanged;
        public event EventHandler ParentRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler RenameRequested;
        public event EventHandler<RemoteDropEventArgs> FilesDropped;
        public event EventHandler<bool> DragHoverChanged;

        public RemoteTileControl()
        {
            EmptyText = "This directory is empty.";
            AllowDrop = true;
        }

        protected override int HeaderHeight => Padding12;
        protected override int RowCount => Columns == 0 ? 0 : (_rows.Count + Columns - 1) / Columns;
        protected override int GetRowHeight(int index) => TileHeight + Gap;
        protected override bool IsSelectable(int index) => false;
        protected override int DefaultScrollStep => 40;

        private int Columns => Math.Max(1, (Math.Max(0, Width - ScrollBarWidth) - Padding12 * 2 + Gap) / (TileWidth + Gap));

        [Browsable(false)]
        public List<RemoteEntry> SelectedEntries =>
            _selected.OrderBy(i => i).Where(i => i < _rows.Count).Select(i => _rows[i]).Where(e => e.Name != "..").ToList();

        /// <summary>Takes the list view's rows, keeping the selection when the directory is the same.</summary>
        public void SetRows(string directory, IEnumerable<RemoteEntry> rows)
        {
            var normalized = RemotePath.Normalize(directory);
            var keep = normalized == _directory ? new HashSet<string>(SelectedEntries.Select(e => e.Name), StringComparer.Ordinal) : null;
            int scroll = normalized == _directory ? ScrollOffset : 0;
            _directory = normalized;
            _rows.Clear();
            if (rows != null) _rows.AddRange(rows);
            _selected.Clear();
            if (keep != null)
            {
                for (int i = 0; i < _rows.Count; i++) if (keep.Contains(_rows[i].Name)) _selected.Add(i);
            }
            _focus = _selected.Count > 0 ? _selected.Min() : -1;
            _anchor = _focus;
            RestoreState(-1, scroll);
            ContentChanged();
        }

        public void SelectNames(IEnumerable<string> names)
        {
            var set = new HashSet<string>(names ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            _selected.Clear();
            for (int i = 0; i < _rows.Count; i++) if (set.Contains(_rows[i].Name)) _selected.Add(i);
            _focus = _anchor = _selected.Count > 0 ? _selected.Min() : -1;
            if (_focus >= 0) EnsureVisible(_focus / Columns);
            Invalidate();
            ItemSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        private Rectangle TileBounds(int item, int rowTop)
        {
            int column = item % Columns;
            return new Rectangle(Padding12 + column * (TileWidth + Gap), rowTop, TileWidth, TileHeight);
        }

        private int ItemAt(Point point)
        {
            int row = RowIndexAt(point);
            if (row < 0) return -1;
            int column = (point.X - Padding12) / (TileWidth + Gap);
            if (point.X < Padding12 || column >= Columns) return -1;
            int item = row * Columns + column;
            if (item >= _rows.Count) return -1;
            return TileBounds(item, RowTop(row)).Contains(point) ? item : -1;
        }

        protected override void PaintHeader(Graphics g, Rectangle bounds)
        {
            Draw.Fill(g, bounds, RowSurface);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            int columns = Columns;
            for (int column = 0; column < columns; column++)
            {
                int item = index * columns + column;
                if (item >= _rows.Count) break;
                var entry = _rows[item];
                var tile = TileBounds(item, bounds.Y);
                bool parent = entry.Name == "..";

                var face = surface;
                if (item == _dropItem)
                {
                    face = p.SelectionOn(surface);
                    Draw.FillRounded(g, tile, 6f, face);
                    Draw.DrawRounded(g, tile, 6f, p.Accent);
                }
                else if (_selected.Contains(item))
                {
                    face = p.SelectionOn(surface);
                    Draw.FillRounded(g, tile, 6f, face);
                    Draw.DrawRounded(g, tile, 6f, ThemePalette.Tint(p.Accent, 0.4, surface));
                }
                else if (item == _hotItem)
                {
                    face = p.HoverOn(surface);
                    Draw.FillRounded(g, tile, 6f, face);
                }
                if (item == _focus && Focused) Draw.DrawRounded(g, new Rectangle(tile.X + 1, tile.Y + 1, tile.Width - 2, tile.Height - 2), 5f, p.Accent, 2f);

                string glyph;
                Color color;
                if (parent) { glyph = Icons.Back; color = p.Folder; }
                else if (entry.IsDirectory) { glyph = Icons.Folder; color = p.Folder; }
                else if (entry.Kind == RemoteEntryKind.Symlink) { glyph = entry.OpensAsFolder ? Icons.FolderLink : Icons.Symlink; color = p.Lane2; }
                else { glyph = Icons.File; color = p.Foreground3; }
                IconCache.DrawCentered(g, glyph, 32, color, tile.X + tile.Width / 2, tile.Y + 30);

                var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
                Draw.Text(g, entry.Name, Fonts.Ui(13f, entry.OpensAsFolder && !parent),new Rectangle(tile.X + 6, tile.Y + 52, tile.Width - 12, 34), p.Foreground, flags);

                string meta;
                if (parent) meta = "Parent directory";
                else if (entry.IsDirectory) meta = entry.ItemCount.HasValue ? Format.Count(entry.ItemCount.Value, "item") : "Folder";
                else if (entry.Kind == RemoteEntryKind.Symlink) meta = entry.LinksToDirectory == true ? "Folder link" : "Symlink";
                else meta = Format.Bytes(entry.Size);
                Draw.Text(g, meta, Fonts.Ui(11.5f), new Rectangle(tile.X + 4, tile.Y + 86, tile.Width - 8, 14), p.Foreground3, Draw.CenterMiddle | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ContentChanged();
        }

        // ------------------------------------------------------------------ selection input

        private void SelectOnly(int item)
        {
            _selected.Clear();
            if (item >= 0) _selected.Add(item);
            _anchor = _focus = item;
            Invalidate();
            ItemSelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = ItemAt(e.Location);
            if (hot == _hotItem) return;
            _hotItem = hot;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotItem == -1) return;
            _hotItem = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int item = ItemAt(e.Location);
            if (e.Button == MouseButtons.Right)
            {
                if (item >= 0 && !_selected.Contains(item)) SelectOnly(item);
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (item < 0)
            {
                if (_selected.Count > 0) SelectOnly(-1);
                return;
            }
            if ((ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (!_selected.Remove(item)) _selected.Add(item);
                _anchor = _focus = item;
                Invalidate();
                ItemSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            else if ((ModifierKeys & Keys.Shift) == Keys.Shift && _anchor >= 0)
            {
                _selected.Clear();
                for (int i = Math.Min(_anchor, item); i <= Math.Max(_anchor, item); i++) _selected.Add(i);
                _focus = item;
                Invalidate();
                ItemSelectionChanged?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                SelectOnly(item);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int item = ItemAt(e.Location);
            if (item >= 0 && e.Button == MouseButtons.Left) EntryActivated?.Invoke(this, new RemoteEntryEventArgs(_rows[item]));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left: case Keys.Right: case Keys.Enter: case Keys.Back: case Keys.Delete:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int columns = Columns;
            int target = _focus;
            switch (e.KeyData)
            {
                case Keys.Left: target = Math.Max(0, _focus - 1); break;
                case Keys.Right: target = Math.Min(_rows.Count - 1, _focus + 1); break;
                case Keys.Up: target = _focus - columns >= 0 ? _focus - columns : _focus; break;
                case Keys.Down: target = _focus + columns < _rows.Count ? _focus + columns : _focus; break;
                case Keys.Home: target = 0; break;
                case Keys.End: target = _rows.Count - 1; break;
                case Keys.Enter:
                    if (_focus >= 0 && _focus < _rows.Count) EntryActivated?.Invoke(this, new RemoteEntryEventArgs(_rows[_focus]));
                    e.Handled = true;
                    return;
                case Keys.Back: ParentRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return;
                case Keys.Delete: DeleteRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return;
                case Keys.F2: RenameRequested?.Invoke(this, EventArgs.Empty); e.Handled = true; return;
                case Keys.Control | Keys.A:
                    _selected.Clear();
                    for (int i = 0; i < _rows.Count; i++) if (_rows[i].Name != "..") _selected.Add(i);
                    Invalidate();
                    ItemSelectionChanged?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                default:
                    base.OnKeyDown(e);
                    return;
            }
            e.Handled = true;
            if (_rows.Count == 0) return;
            if (target < 0) target = 0;
            SelectOnly(target);
            EnsureVisible(target / columns);
        }

        // ------------------------------------------------------------------ drag and drop

        private void UpdateDrop(DragEventArgs e)
        {
            if (DropTargetControl.GetFiles(e.Data) == null)
            {
                e.Effect = DragDropEffects.None;
                SetDropState(-1, false);
                return;
            }
            e.Effect = DragDropEffects.Copy;
            int item = ItemAt(PointToClient(new Point(e.X, e.Y)));
            bool folder = item >= 0 && _rows[item].OpensAsFolder && _rows[item].Name != "..";
            SetDropState(folder ? item : -1, !folder);
        }

        private void SetDropState(int item, bool overView)
        {
            if (item != _dropItem)
            {
                _dropItem = item;
                Invalidate();
            }
            if (overView != _dragOverView)
            {
                _dragOverView = overView;
                DragHoverChanged?.Invoke(this, overView);
            }
        }

        protected override void OnDragEnter(DragEventArgs e) { base.OnDragEnter(e); UpdateDrop(e); }
        protected override void OnDragOver(DragEventArgs e) { base.OnDragOver(e); UpdateDrop(e); }
        protected override void OnDragLeave(EventArgs e) { base.OnDragLeave(e); SetDropState(-1, false); }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            var files = DropTargetControl.GetFiles(e.Data);
            var target = _dropItem >= 0 ? _rows[_dropItem].FullPath : _directory;
            SetDropState(-1, false);
            if (files != null && files.Length > 0) FilesDropped?.Invoke(this, new RemoteDropEventArgs(files, target));
        }
    }

    /// <summary>
    /// The transfer queue's footer: a 160 px aggregate track with "62% of 6 transfers" and the
    /// byte totals, and upload speed, download speed and connections in use on the right.
    /// </summary>
    [ToolboxItem(true)]
    public sealed class QueueSummaryControl : ModernControl
    {
        private Transfers.QueueStats _stats = new Transfers.QueueStats();

        public QueueSummaryControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(800, 34);
            Theme.Changed += OnThemeChanged;
        }

        public void SetStats(Transfers.QueueStats stats)
        {
            _stats = stats ?? new Transfers.QueueStats();
            Invalidate();
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = p.Layer;
            for (var c = Parent; c != null; c = c.Parent)
            {
                if (c is SurfacePanel panel) { surface = panel.SurfaceColor; break; }
            }
            Draw.Fill(g, ClientRectangle, surface);
            Draw.HLine(g, 0, 0, Width, p.DividerOn(surface));

            var s = _stats;
            var font = Fonts.Ui(12.5f);
            int centreY = Height / 2;
            int x = 14;

            double fraction = s.TotalBytes > 0 ? (double)s.TransferredBytes / s.TotalBytes : s.Counted > 0 ? (double)s.Completed / s.Counted : 0;
            fraction = Math.Max(0, Math.Min(1, fraction));
            var track = new Rectangle(x, centreY - 2, 160, 4);
            Draw.FillRounded(g, track, 2f, p.Fill2On(surface));
            int filled = (int)Math.Round(track.Width * fraction);
            if (filled > 0) Draw.FillRounded(g, new Rectangle(track.X, track.Y, filled, track.Height), 2f, p.AccentFill);
            x = track.Right + 12;

            var summary = s.Counted == 0 ? "No transfers" : (int)Math.Floor(fraction * 100) + "% of " + Format.Count(s.Counted, "transfer");
            x = Run(g, summary, font, p.Foreground2, x) + 18;
            if (s.TotalBytes > 0) Run(g, Format.Bytes(s.TransferredBytes) + " of " + Format.Bytes(s.TotalBytes), font, p.Foreground3, x);

            int right = Width - 14;
            var connections = s.OpenConnections == 0 ? "No connections" : s.BusyConnections + " of " + Format.Count(s.OpenConnections, "connection");
            right = RunRight(g, connections, font, p.Foreground2, right) - 14;
            right = RunRight(g, "↓ " + (s.DownloadBytesPerSecond >= 1 ? Format.Speed(s.DownloadBytesPerSecond) : "0 B/s"), font, p.Down, right) - 14;
            RunRight(g, "↑ " + (s.UploadBytesPerSecond >= 1 ? Format.Speed(s.UploadBytesPerSecond) : "0 B/s"), font, p.Up, right);
        }

        private int Run(Graphics g, string text, Font font, Color color, int x)
        {
            int w = Draw.MeasureWidth(text, font);
            Draw.Text(g, text, font, new Rectangle(x, 0, w + 2, Height), color, Draw.LeftMiddle);
            return x + w;
        }

        private int RunRight(Graphics g, string text, Font font, Color color, int right)
        {
            int w = Draw.MeasureWidth(text, font);
            Draw.Text(g, text, font, new Rectangle(right - w, 0, w + 2, Height), color, Draw.LeftMiddle);
            return right - w;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
