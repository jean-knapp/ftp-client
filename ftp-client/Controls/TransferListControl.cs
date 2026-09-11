using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Services;
using FtpClient.Transfers;

namespace FtpClient.Controls
{
    public sealed class TransferItemEventArgs : EventArgs
    {
        public TransferItemEventArgs(TransferItem item) { Item = item; }
        public TransferItem Item { get; }
    }

    /// <summary>
    /// The transfer queue's rows: a direction glyph, the file over its destination path, what is
    /// happening to it, a 4 px progress track with its status, then size, speed and ETA. A failed
    /// row keeps the server's words in the Direction column and offers "retry" in the ETA column.
    /// </summary>
    [ToolboxItem(true)]
    public sealed class TransferListControl : VirtualListControl
    {
        private const int Header = 28;
        private const int RowHeight = 36;
        private const int GroupHeight = 28;
        private const int SidePadding = 14;
        private const int Gutter = 26;
        private const int DirectionWidth = 220;
        private const int ProgressWidth = 240;
        private const int SizeWidth = 110;
        private const int SpeedWidth = 110;
        private const int EtaWidth = 90;
        private const int NumberRightPadding = 24;
        private const int StatusWidth = 52;

        private sealed class Row
        {
            public TransferItem Item;
            public string Group;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Func<TransferItem, int> _positionOf;
        private int _hotRetry = -1;

        public event EventHandler<TransferItemEventArgs> RetryRequested;

        private struct Columns
        {
            public int FileX;
            public int FileRight;
            public int DirectionX;
            public int ProgressX;
            public int SizeX;
            public int SpeedX;
            public int EtaX;
            public bool Direction;
            public bool Speed;
        }

        public TransferListControl()
        {
            EmptyText = "Nothing to transfer. Drop files on the browser or press Upload.";
        }

        protected override bool MultiSelect => true;
        protected override int HeaderHeight => Header;
        protected override int RowCount => _rows.Count;
        protected override int GetRowHeight(int index) => _rows[index].Item == null ? GroupHeight : RowHeight;
        protected override bool IsSelectable(int index) => _rows[index].Item != null;
        protected override int DefaultScrollStep => RowHeight;

        public TransferItem ItemAt(int index) => index >= 0 && index < _rows.Count ? _rows[index].Item : null;

        [Browsable(false)]
        public List<TransferItem> SelectedItems => SelectedIndices.Select(ItemAt).Where(i => i != null).ToList();

        private static int Rank(TransferItem item)
        {
            switch (item.Status)
            {
                case TransferStatus.Active: return 0;
                case TransferStatus.Queued:
                case TransferStatus.Paused: return 1;
                case TransferStatus.Failed: return 2;
                default: return 3;
            }
        }

        /// <summary>Replaces the rows, keeping the selection and scroll position.</summary>
        public void SetItems(IEnumerable<TransferItem> items, QueueGrouping grouping, Func<TransferItem, int> positionOf)
        {
            _positionOf = positionOf;
            var selected = new HashSet<long>(SelectedItems.Select(i => i.Id));
            int scroll = ScrollOffset;

            var ordered = (items ?? Enumerable.Empty<TransferItem>())
                .OrderBy(Rank)
                .ThenBy(i => Rank(i) == 3 ? -(i.FinishedAt ?? i.QueuedAt).Ticks : i.Id)
                .ToList();

            _rows.Clear();
            if (grouping == QueueGrouping.Flat)
            {
                foreach (var item in ordered) _rows.Add(new Row { Item = item });
            }
            else
            {
                foreach (var group in ordered.GroupBy(i => GroupKey(i, grouping)))
                {
                    _rows.Add(new Row { Group = group.Key + "  ·  " + group.Count() });
                    foreach (var item in group) _rows.Add(new Row { Item = item });
                }
            }

            RestoreState(-1, scroll);
            var indices = new List<int>();
            for (int i = 0; i < _rows.Count; i++) if (_rows[i].Item != null && selected.Contains(_rows[i].Item.Id)) indices.Add(i);
            SelectIndices(indices, false);
            ContentChanged();
        }

        private static string GroupKey(TransferItem item, QueueGrouping grouping)
        {
            if (grouping == QueueGrouping.BySite) return item.Site?.Name ?? item.Site?.Host ?? "Site";
            switch (item.Direction)
            {
                case TransferDirection.Upload: return "Uploads";
                case TransferDirection.Download: return "Downloads";
                default: return "Deletions";
            }
        }

        // ------------------------------------------------------------------ columns

        private Columns ComputeColumns(int width)
        {
            var c = new Columns { Direction = true, Speed = true };
            int available = width - SidePadding * 2 - Gutter;
            int fixedWidth = DirectionWidth + ProgressWidth + SizeWidth + SpeedWidth + EtaWidth;
            if (available - fixedWidth < 180) { c.Direction = false; fixedWidth -= DirectionWidth; }
            if (available - fixedWidth < 180) { c.Speed = false; fixedWidth -= SpeedWidth; }

            c.FileX = SidePadding + Gutter;
            int x = Math.Max(c.FileX + 80, width - SidePadding - fixedWidth);
            c.FileRight = x - 12;
            if (c.Direction) { c.DirectionX = x; x += DirectionWidth; }
            c.ProgressX = x; x += ProgressWidth;
            c.SizeX = x; x += SizeWidth;
            if (c.Speed) { c.SpeedX = x; x += SpeedWidth; }
            c.EtaX = x;
            return c;
        }

        protected override void PaintHeader(Graphics g, Rectangle bounds)
        {
            var p = P;
            var band = p.FillOn(RowSurface);
            Draw.Fill(g, bounds, band);
            var font = Fonts.Ui(12f);
            var c = ComputeColumns(Math.Max(0, Width - ScrollBarSpace));
            Draw.Text(g, "File", font, new Rectangle(c.FileX, bounds.Y, 200, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            if (c.Direction) Draw.Text(g, "Direction", font, new Rectangle(c.DirectionX, bounds.Y, DirectionWidth, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            Draw.Text(g, "Progress", font, new Rectangle(c.ProgressX, bounds.Y, ProgressWidth, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            Draw.Text(g, "Size", font, new Rectangle(c.SizeX, bounds.Y, SizeWidth - NumberRightPadding, bounds.Height), p.Foreground3, Draw.RightMiddle);
            if (c.Speed) Draw.Text(g, "Speed", font, new Rectangle(c.SpeedX, bounds.Y, SpeedWidth - NumberRightPadding, bounds.Height), p.Foreground3, Draw.RightMiddle);
            Draw.Text(g, "ETA", font, new Rectangle(c.EtaX, bounds.Y, EtaWidth, bounds.Height), p.Foreground3, Draw.RightMiddle);
        }

        // ------------------------------------------------------------------ rows

        private static bool Settled(TransferItem item) =>
            item.Status == TransferStatus.Completed || item.Status == TransferStatus.Skipped || item.Status == TransferStatus.Cancelled;

        private Color DirectionColor(TransferItem item)
        {
            var p = P;
            if (item.Status == TransferStatus.Failed) return p.Error;
            if (Settled(item)) return p.Foreground3;
            switch (item.Direction)
            {
                case TransferDirection.Upload: return p.Up;
                case TransferDirection.Download: return p.Down;
                default: return p.Error;
            }
        }

        private static string DirectionGlyph(TransferItem item)
        {
            switch (item.Direction)
            {
                case TransferDirection.Upload: return Icons.Upload;
                case TransferDirection.Download: return Icons.Download;
                default: return Icons.Delete;
            }
        }

        private string Phrase(TransferItem item)
        {
            var site = item.Site?.Name ?? item.Site?.Host ?? "the server";
            switch (item.Status)
            {
                case TransferStatus.Active:
                    if (item.Direction == TransferDirection.Upload) return "Uploading to " + site;
                    if (item.Direction == TransferDirection.Download) return "Downloading from " + site;
                    return "Deleting on " + site;
                case TransferStatus.Queued:
                    int position = _positionOf != null ? _positionOf(item) : 0;
                    return position > 0 ? "Queued — position " + position : "Queued";
                case TransferStatus.Paused:
                    return "Paused";
                case TransferStatus.Failed:
                    return "Failed — " + (item.Error ?? "unknown error");
                case TransferStatus.Completed:
                    return (item.Direction == TransferDirection.Delete ? "Deleted " : "Completed ") + (item.FinishedAt ?? DateTime.Now).ToString("HH:mm");
                case TransferStatus.Skipped:
                    return item.Error ?? "Skipped";
                default:
                    return "Cancelled";
            }
        }

        private Rectangle RetryBounds(int index, Rectangle row)
        {
            var c = ComputeColumns(row.Width);
            return new Rectangle(c.EtaX + EtaWidth - 40, row.Y + 6, 40, row.Height - 12);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var row = _rows[index];

            if (row.Item == null)
            {
                Draw.Text(g, row.Group.ToUpperInvariant(), Fonts.Ui(11f, true), new Rectangle(SidePadding, bounds.Y, bounds.Width - SidePadding * 2, bounds.Height), p.Foreground3, Draw.LeftMiddle);
                Draw.HLine(g, 0, bounds.Bottom - 1, bounds.Width, p.DividerOn(surface));
                return;
            }

            var item = row.Item;
            var face = surface;
            if ((state & RowState.Selected) != 0)
            {
                face = p.SelectionOn(surface);
                Draw.Fill(g, bounds, face);
            }
            else if ((state & RowState.Hot) != 0)
            {
                face = p.HoverOn(surface);
                Draw.Fill(g, bounds, face);
            }

            var c = ComputeColumns(bounds.Width);
            var tint = DirectionColor(item);
            int centreY = bounds.Y + bounds.Height / 2;

            IconCache.DrawCentered(g, DirectionGlyph(item), 15, tint, SidePadding + 7, centreY);

            int fileWidth = Math.Max(0, c.FileRight - c.FileX);
            Draw.Text(g, item.FileName, Fonts.Ui(13.5f), new Rectangle(c.FileX, bounds.Y + 2, fileWidth, 18), p.Foreground, Draw.LeftMiddle);
            var target = item.Direction == TransferDirection.Download ? Format.HomeRelative(item.LocalPath) : item.RemotePath;
            Draw.Text(g, target, Fonts.Code(11.5f), new Rectangle(c.FileX, bounds.Y + 19, fileWidth, 15), p.Foreground3, Draw.LeftMiddle);

            var numberFont = Fonts.Ui(12.5f);
            if (c.Direction)
            {
                IconCache.DrawLeft(g, DirectionGlyph(item), 12, p.Foreground3, new Rectangle(c.DirectionX, bounds.Y, 12, bounds.Height));
                Draw.Text(g, Phrase(item), numberFont, new Rectangle(c.DirectionX + 18, bounds.Y, DirectionWidth - 18 - 12, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            }

            // Progress: the track, its fill in the row's colour, then the status word.
            int trackWidth = ProgressWidth - StatusWidth - 32;
            var track = new Rectangle(c.ProgressX, centreY - 2, trackWidth, 4);
            Draw.FillRounded(g, track, 2f, p.Fill2On(face));
            double fraction = item.Direction == TransferDirection.Delete ? (Settled(item) ? 1 : 0) : item.Fraction;
            if (item.Status == TransferStatus.Failed) fraction = Math.Max(0.12, fraction);
            if (item.Status == TransferStatus.Completed) fraction = 1;
            int filled = (int)Math.Round(trackWidth * fraction);
            if (filled > 0) Draw.FillRounded(g, new Rectangle(track.X, track.Y, filled, track.Height), 2f, tint);

            string status;
            var statusColor = p.Foreground2;
            switch (item.Status)
            {
                case TransferStatus.Active:
                    status = item.Direction == TransferDirection.Delete ? "…" : (int)Math.Floor(item.Fraction * 100) + "%";
                    break;
                case TransferStatus.Queued: status = "Queued"; break;
                case TransferStatus.Paused: status = "Paused"; break;
                case TransferStatus.Failed: status = "Failed"; statusColor = p.Error; break;
                case TransferStatus.Completed: status = "Done"; statusColor = p.Foreground3; break;
                case TransferStatus.Skipped: status = "Skipped"; statusColor = p.Foreground3; break;
                default: status = "Cancelled"; statusColor = p.Foreground3; break;
            }
            Draw.Text(g, status, numberFont, new Rectangle(track.Right + 8, bounds.Y, StatusWidth, bounds.Height), statusColor, Draw.RightMiddle);

            var size = item.Direction == TransferDirection.Delete || item.TotalBytes <= 0 && item.Status != TransferStatus.Completed ? "—" : Format.Bytes(item.TotalBytes);
            Draw.Text(g, size, numberFont, new Rectangle(c.SizeX, bounds.Y, SizeWidth - NumberRightPadding, bounds.Height), p.Foreground2, Draw.RightMiddle);

            if (c.Speed)
            {
                var speed = item.Status == TransferStatus.Active ? Format.Speed(item.BytesPerSecond) : "—";
                Draw.Text(g, speed, numberFont, new Rectangle(c.SpeedX, bounds.Y, SpeedWidth - NumberRightPadding, bounds.Height), p.Foreground2, Draw.RightMiddle);
            }

            if (item.Status == TransferStatus.Failed)
            {
                bool hot = index == _hotRetry;
                var retry = RetryBounds(index, bounds);
                Draw.Text(g, "retry", numberFont, new Rectangle(c.EtaX, bounds.Y, EtaWidth, bounds.Height), hot ? p.Accent : p.Foreground2, Draw.RightMiddle);
                if (hot)
                {
                    int w = Draw.MeasureWidth("retry", numberFont);
                    Draw.HLine(g, c.EtaX + EtaWidth - w, centreY + 8, w, p.Accent);
                }
            }
            else
            {
                var eta = item.Status == TransferStatus.Active ? Format.Eta(item.Eta) : "—";
                Draw.Text(g, eta, numberFont, new Rectangle(c.EtaX, bounds.Y, EtaWidth, bounds.Height), p.Foreground2, Draw.RightMiddle);
            }
        }

        // ------------------------------------------------------------------ input

        private int RetryIndexAt(Point point)
        {
            int index = RowIndexAt(point);
            var item = ItemAt(index);
            if (item == null || item.Status != TransferStatus.Failed) return -1;
            var bounds = new Rectangle(0, RowTop(index), Math.Max(0, Width - ScrollBarSpace), RowHeight);
            return RetryBounds(index, bounds).Contains(point) ? index : -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = RetryIndexAt(e.Location);
            if (hot == _hotRetry) return;
            _hotRetry = hot;
            Cursor = hot >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hotRetry = -1;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            int index = RetryIndexAt(e.Location);
            if (index >= 0) RetryRequested?.Invoke(this, new TransferItemEventArgs(ItemAt(index)));
        }
    }
}
