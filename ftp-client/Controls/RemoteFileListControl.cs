using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using FtpClient.Remote;
using FtpClient.Services;

namespace FtpClient.Controls
{
    public enum FileSortColumn
    {
        Name,
        Size,
        Kind,
        Modified,
        Permissions,
        Owner,
    }

    public sealed class RemoteEntryEventArgs : EventArgs
    {
        public RemoteEntryEventArgs(RemoteEntry entry) { Entry = entry; }
        public RemoteEntry Entry { get; }
    }

    public sealed class RemoteDropEventArgs : EventArgs
    {
        public RemoteDropEventArgs(string[] paths, string targetDirectory)
        {
            Paths = paths;
            TargetDirectory = targetDirectory;
        }

        public string[] Paths { get; }
        public string TargetDirectory { get; }
    }

    /// <summary>
    /// The Files tab: a 30 px column header (Name, Size, Kind, Modified, Permissions, Owner) over
    /// rows 26 to 38 px tall. Folders sort first and are set semibold; the parent row leads; rows
    /// being transferred carry an --up wash. Files dropped on a folder row upload into it.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("EntryActivated")]
    public sealed class RemoteFileListControl : VirtualListControl
    {
        private const int Header = 30;
        private const int SidePadding = 14;
        private const int Gutter = 26;
        private const int SizeWidth = 110;
        private const int SizeRightPadding = 24;
        private const int KindWidth = 150;
        private const int ModifiedWidth = 180;
        private const int PermissionsWidth = 130;
        private const int OwnerWidth = 110;
        private const int MinimumNameWidth = 200;

        private readonly List<RemoteEntry> _all = new List<RemoteEntry>();
        private readonly List<RemoteEntry> _rows = new List<RemoteEntry>();
        private readonly HashSet<string> _transferring = new HashSet<string>(StringComparer.Ordinal);
        private string _directory = RemotePath.Root;
        private string _filter;
        private bool _showHidden;
        private int _rowHeight = 32;
        private FileSortColumn _sortColumn = FileSortColumn.Name;
        private bool _ascending = true;
        private int _dropRow = -1;
        private bool _dragOverList;

        public event EventHandler<RemoteEntryEventArgs> EntryActivated;
        public event EventHandler ParentRequested;
        public event EventHandler DeleteRequested;
        public event EventHandler RenameRequested;
        public event EventHandler<RemoteDropEventArgs> FilesDropped;

        /// <summary>Files are dragged over the list but not over a folder: the drop zone should light.</summary>
        public event EventHandler<bool> DragHoverChanged;

        private struct Columns
        {
            public int NameX;
            public int NameRight;
            public int SizeX;
            public int KindX;
            public int ModifiedX;
            public int PermissionsX;
            public int OwnerX;
            public bool Kind;
            public bool Modified;
            public bool Permissions;
            public bool Owner;
        }

        public RemoteFileListControl()
        {
            EmptyText = "This directory is empty.";
            AllowDrop = true;
        }

        protected override bool MultiSelect => true;
        protected override int HeaderHeight => Header;
        protected override int RowCount => _rows.Count;
        protected override int GetRowHeight(int index) => _rowHeight;
        protected override int DefaultScrollStep => _rowHeight;

        [Category("Appearance"), DefaultValue(32)]
        public int RowHeight
        {
            get => _rowHeight;
            set
            {
                int clamped = Math.Max(26, Math.Min(38, value));
                if (clamped == _rowHeight) return;
                _rowHeight = clamped;
                ContentChanged();
            }
        }

        [Category("Behavior"), DefaultValue(false)]
        public bool ShowHidden
        {
            get => _showHidden;
            set
            {
                if (_showHidden == value) return;
                _showHidden = value;
                Rebuild(SelectedNames(), ScrollOffset);
            }
        }

        /// <summary>Case-insensitive name filter for the current directory.</summary>
        [Browsable(false)]
        public string Filter
        {
            get => _filter;
            set
            {
                var filter = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
                if (filter == _filter) return;
                _filter = filter;
                Rebuild(SelectedNames(), 0);
            }
        }

        [Browsable(false)]
        public string Directory => _directory;

        /// <summary>Every entry of the directory, hidden and filtered ones included.</summary>
        [Browsable(false)]
        public IReadOnlyList<RemoteEntry> AllEntries => _all;

        /// <summary>The entries on screen, without the parent row.</summary>
        [Browsable(false)]
        public IEnumerable<RemoteEntry> VisibleEntries => _rows.Where(e => e.Name != "..");

        [Browsable(false)]
        public List<RemoteEntry> SelectedEntries =>
            SelectedIndices.Where(i => i >= 0 && i < _rows.Count).Select(i => _rows[i]).Where(e => e.Name != "..").ToList();

        public RemoteEntry EntryAt(int index) => index >= 0 && index < _rows.Count ? _rows[index] : null;

        /// <summary>The rows as displayed - parent row, filter and sort applied - for the grid view.</summary>
        [Browsable(false)]
        public IReadOnlyList<RemoteEntry> DisplayedRows => _rows;

        /// <summary>Raised after the displayed rows change.</summary>
        public event EventHandler RowsChanged;

        public void SetEntries(string directory, IEnumerable<RemoteEntry> entries)
        {
            var normalized = RemotePath.Normalize(directory);
            bool same = normalized == _directory;
            var keep = same ? SelectedNames() : null;
            _directory = normalized;
            _all.Clear();
            if (entries != null) _all.AddRange(entries.Where(e => e.Name != "." && e.Name != ".."));
            Rebuild(keep, same ? ScrollOffset : 0);
        }

        /// <summary>Selects entries by name, e.g. a folder that was just created.</summary>
        public void SelectNames(IEnumerable<string> names)
        {
            var set = new HashSet<string>(names ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            var indices = new List<int>();
            for (int i = 0; i < _rows.Count; i++) if (set.Contains(_rows[i].Name)) indices.Add(i);
            SelectIndices(indices, true);
            if (indices.Count > 0) EnsureVisible(indices[0]);
        }

        public void SelectAll()
        {
            SelectIndices(Enumerable.Range(0, _rows.Count).Where(i => _rows[i].Name != ".."), true);
        }

        /// <summary>Remote paths with a transfer in flight, painted with the --up wash.</summary>
        public void SetTransferring(IEnumerable<string> remotePaths)
        {
            var next = new HashSet<string>(remotePaths ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (next.SetEquals(_transferring)) return;
            _transferring.Clear();
            _transferring.UnionWith(next);
            Invalidate();
        }

        private List<string> SelectedNames() => SelectedEntries.Select(e => e.Name).ToList();

        private void Rebuild(ICollection<string> selectNames, int scroll)
        {
            _rows.Clear();
            if (_directory != RemotePath.Root)
            {
                _rows.Add(new RemoteEntry { Name = "..", FullPath = RemotePath.Parent(_directory), Kind = RemoteEntryKind.Directory });
            }
            var visible = _all.Where(e => (_showHidden || !e.IsHidden) &&
                (_filter == null || e.Name.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            visible.Sort(CompareEntries);
            _rows.AddRange(visible);

            RestoreState(-1, scroll);
            if (selectNames != null && selectNames.Count > 0)
            {
                var set = new HashSet<string>(selectNames, StringComparer.Ordinal);
                SelectIndices(Enumerable.Range(0, _rows.Count).Where(i => set.Contains(_rows[i].Name)), false);
            }
            ContentChanged();
            RowsChanged?.Invoke(this, EventArgs.Empty);
        }

        private int CompareEntries(RemoteEntry a, RemoteEntry b)
        {
            // Folders first, and links to folders among them.
            if (a.OpensAsFolder != b.OpensAsFolder) return a.OpensAsFolder ? -1 : 1;
            int result;
            switch (_sortColumn)
            {
                case FileSortColumn.Size:
                    result = a.IsDirectory ? Nullable.Compare(a.ItemCount, b.ItemCount) : a.Size.CompareTo(b.Size);
                    break;
                case FileSortColumn.Kind:
                    result = string.Compare(FileKinds.Describe(a), FileKinds.Describe(b), StringComparison.CurrentCultureIgnoreCase);
                    break;
                case FileSortColumn.Modified:
                    result = Nullable.Compare(a.Modified, b.Modified);
                    break;
                case FileSortColumn.Permissions:
                    result = Nullable.Compare(a.Mode, b.Mode);
                    break;
                case FileSortColumn.Owner:
                    result = string.Compare(a.Owner, b.Owner, StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    result = 0;
                    break;
            }
            if (result == 0) result = NativeSort.Compare(a.Name, b.Name);
            return _ascending ? result : -result;
        }

        // ------------------------------------------------------------------ columns

        private Columns ComputeColumns(int width)
        {
            var c = new Columns { Kind = true, Modified = true, Permissions = true, Owner = true };
            int available = width - SidePadding * 2 - Gutter;
            int fixedWidth = SizeWidth + KindWidth + ModifiedWidth + PermissionsWidth + OwnerWidth;
            if (available - fixedWidth < MinimumNameWidth) { c.Owner = false; fixedWidth -= OwnerWidth; }
            if (available - fixedWidth < MinimumNameWidth) { c.Permissions = false; fixedWidth -= PermissionsWidth; }
            if (available - fixedWidth < MinimumNameWidth) { c.Kind = false; fixedWidth -= KindWidth; }
            if (available - fixedWidth < MinimumNameWidth) { c.Modified = false; fixedWidth -= ModifiedWidth; }

            c.NameX = SidePadding + Gutter;
            int x = Math.Max(c.NameX + 80, width - SidePadding - fixedWidth);
            c.NameRight = x;
            c.SizeX = x;
            x += SizeWidth;
            if (c.Kind) { c.KindX = x; x += KindWidth; }
            if (c.Modified) { c.ModifiedX = x; x += ModifiedWidth; }
            if (c.Permissions) { c.PermissionsX = x; x += PermissionsWidth; }
            if (c.Owner) c.OwnerX = x;
            return c;
        }

        private FileSortColumn? ColumnAt(int x)
        {
            var c = ComputeColumns(Math.Max(0, Width - ScrollBarSpace));
            if (c.Owner && x >= c.OwnerX) return FileSortColumn.Owner;
            if (c.Permissions && x >= c.PermissionsX) return FileSortColumn.Permissions;
            if (c.Modified && x >= c.ModifiedX) return FileSortColumn.Modified;
            if (c.Kind && x >= c.KindX) return FileSortColumn.Kind;
            if (x >= c.SizeX) return FileSortColumn.Size;
            if (x >= SidePadding) return FileSortColumn.Name;
            return null;
        }

        protected override void PaintHeader(Graphics g, Rectangle bounds)
        {
            var p = P;
            var surface = RowSurface;
            var band = p.FillOn(surface);
            Draw.Fill(g, bounds, band);

            var font = Fonts.Ui(12f);
            var c = ComputeColumns(Math.Max(0, Width - ScrollBarSpace));
            HeaderLabel(g, "Name", FileSortColumn.Name, c.NameX, c.NameRight - c.NameX - 12, false, font, bounds);
            HeaderLabel(g, "Size", FileSortColumn.Size, c.SizeX, SizeWidth - SizeRightPadding, true, font, bounds);
            if (c.Kind) HeaderLabel(g, "Kind", FileSortColumn.Kind, c.KindX, KindWidth - 12, false, font, bounds);
            if (c.Modified) HeaderLabel(g, "Modified", FileSortColumn.Modified, c.ModifiedX, ModifiedWidth - 12, false, font, bounds);
            if (c.Permissions) HeaderLabel(g, "Permissions", FileSortColumn.Permissions, c.PermissionsX, PermissionsWidth - 12, false, font, bounds);
            if (c.Owner) HeaderLabel(g, "Owner", FileSortColumn.Owner, c.OwnerX, OwnerWidth - 12, false, font, bounds);
        }

        private void HeaderLabel(Graphics g, string text, FileSortColumn column, int x, int width, bool rightAligned, Font font, Rectangle band)
        {
            var p = P;
            bool sorted = column == _sortColumn;
            int textWidth = Draw.MeasureWidth(text, font);
            int left = rightAligned ? x + width - textWidth - (sorted ? 14 : 0) : x;
            Draw.Text(g, text, font, new Rectangle(left, band.Y, textWidth + 2, band.Height), sorted ? p.Foreground2 : p.Foreground3, Draw.LeftMiddle);
            if (sorted)
            {
                IconCache.DrawCentered(g, _ascending ? Icons.ChevronDown : Icons.ChevronUp, 10, p.Foreground3, left + textWidth + 9, band.Y + band.Height / 2);
            }
        }

        // ------------------------------------------------------------------ rows

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var entry = _rows[index];
            bool parent = entry.Name == "..";
            bool selected = (state & RowState.Selected) != 0;
            bool transferring = !parent && _transferring.Contains(entry.FullPath);

            var face = surface;
            if (index == _dropRow)
            {
                face = p.SelectionOn(surface);
                Draw.Fill(g, bounds, face);
                Draw.DrawRounded(g, new Rectangle(bounds.X + 1, bounds.Y, bounds.Width - 2, bounds.Height), 3f, p.Accent);
            }
            else if (selected)
            {
                face = p.SelectionOn(surface);
                Draw.Fill(g, bounds, face);
                Draw.Fill(g, new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), p.AccentFill);
            }
            else if (transferring)
            {
                face = ThemePalette.Tint(p.Up, 0.07, surface);
                Draw.Fill(g, bounds, face);
                Draw.Fill(g, new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), p.Up);
            }
            else if ((state & RowState.Hot) != 0)
            {
                face = p.HoverOn(surface);
                Draw.Fill(g, bounds, face);
            }

            var c = ComputeColumns(bounds.Width);
            int centreY = bounds.Y + bounds.Height / 2;

            string glyph;
            Color glyphColor;
            if (parent) { glyph = Icons.Back; glyphColor = p.Folder; }
            else if (entry.Kind == RemoteEntryKind.Directory) { glyph = Icons.Folder; glyphColor = p.Folder; }
            else if (entry.Kind == RemoteEntryKind.Symlink) { glyph = entry.OpensAsFolder ? Icons.FolderLink : Icons.Symlink; glyphColor = p.Lane2; }
            else { glyph = Icons.File; glyphColor = p.Foreground3; }
            IconCache.DrawCentered(g, glyph, parent ? 14 : 16, glyphColor, SidePadding + 8, centreY);

            // Name and its pill.
            var nameFont = Fonts.Ui(14f, entry.OpensAsFolder && !parent);
            string pill = null;
            bool writablePill = false;
            if (!parent && entry.IsDirectory && entry.Mode.HasValue && (entry.Mode.Value & 0x12) != 0)
            {
                pill = "writable";
                writablePill = true;
            }
            else if (!parent && entry.IsHidden)
            {
                pill = "hidden";
            }
            var pillFont = Fonts.Ui(11.5f);
            int pillWidth = pill == null ? 0 : Draw.MeasureWidth(pill, pillFont) + 12;
            int nameAvailable = Math.Max(0, c.NameRight - 12 - c.NameX - (pill == null ? 0 : pillWidth + 8));
            int nameWidth = Math.Min(nameAvailable, Draw.MeasureWidth(entry.Name, nameFont));
            Draw.Text(g, entry.Name, nameFont, new Rectangle(c.NameX, bounds.Y, nameWidth + 1, bounds.Height), p.Foreground, Draw.LeftMiddle);
            if (pill != null)
            {
                var pillFill = writablePill ? ThemePalette.Tint(p.Up, 0.14, face) : p.Fill2On(face);
                var pillFore = writablePill ? p.Up : p.Foreground3;
                Draw.Tag(g, c.NameX + nameWidth + 8, centreY - 9, 18, pill, pillFont, pillFill, pillFore);
            }

            // Size: item count for folders, bytes for files.
            string size;
            if (parent || entry.Kind == RemoteEntryKind.Symlink) size = string.Empty;
            else if (entry.IsDirectory) size = entry.ItemCount.HasValue ? Format.Count(entry.ItemCount.Value, "item") : string.Empty;
            else size = Format.Bytes(entry.Size);
            var cellFont = Fonts.Ui(13f);
            Draw.Text(g, size, cellFont, new Rectangle(c.SizeX, bounds.Y, SizeWidth - SizeRightPadding, bounds.Height), p.Foreground2, Draw.RightMiddle);

            if (c.Kind) Draw.Text(g, FileKinds.Describe(entry), cellFont, new Rectangle(c.KindX, bounds.Y, KindWidth - 12, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            if (parent) return;
            if (c.Modified) Draw.Text(g, Format.Timestamp(entry.Modified), cellFont, new Rectangle(c.ModifiedX, bounds.Y, ModifiedWidth - 12, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            if (c.Permissions) Draw.Text(g, entry.PermissionString, Fonts.Code(12.5f), new Rectangle(c.PermissionsX, bounds.Y, PermissionsWidth - 12, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            if (c.Owner) Draw.Text(g, entry.Owner, cellFont, new Rectangle(c.OwnerX, bounds.Y, OwnerWidth - 12, bounds.Height), p.Foreground3, Draw.LeftMiddle);
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Cursor = e.Y < Header && ColumnAt(e.X).HasValue ? Cursors.Hand : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || e.Y >= Header) return;
            var column = ColumnAt(e.X);
            if (!column.HasValue) return;
            if (column.Value == _sortColumn) _ascending = !_ascending;
            else
            {
                _sortColumn = column.Value;
                // Sizes and dates read best largest and newest first.
                _ascending = !(column.Value == FileSortColumn.Size || column.Value == FileSortColumn.Modified);
            }
            Rebuild(SelectedNames(), ScrollOffset);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            var entry = EntryAt(RowIndexAt(e.Location));
            if (entry != null && e.Button == MouseButtons.Left) EntryActivated?.Invoke(this, new RemoteEntryEventArgs(entry));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Enter:
                case Keys.Back:
                case Keys.Delete:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyData)
            {
                case Keys.Enter:
                    var entry = EntryAt(SelectedIndex);
                    if (entry != null) EntryActivated?.Invoke(this, new RemoteEntryEventArgs(entry));
                    e.Handled = true;
                    return;
                case Keys.Back:
                    ParentRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                case Keys.Delete:
                    DeleteRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                case Keys.F2:
                    RenameRequested?.Invoke(this, EventArgs.Empty);
                    e.Handled = true;
                    return;
                case Keys.Control | Keys.A:
                    SelectAll();
                    e.Handled = true;
                    return;
            }
            base.OnKeyDown(e);
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
            int index = RowIndexAt(PointToClient(new Point(e.X, e.Y)));
            var entry = EntryAt(index);
            bool folder = entry != null && entry.OpensAsFolder && entry.Name != "..";
            SetDropState(folder ? index : -1, !folder);
        }

        private void SetDropState(int row, bool overList)
        {
            if (row != _dropRow)
            {
                _dropRow = row;
                Invalidate();
            }
            if (overList != _dragOverList)
            {
                _dragOverList = overList;
                DragHoverChanged?.Invoke(this, overList);
            }
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            UpdateDrop(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            UpdateDrop(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            SetDropState(-1, false);
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            var files = DropTargetControl.GetFiles(e.Data);
            var target = EntryAt(_dropRow)?.FullPath ?? _directory;
            SetDropState(-1, false);
            if (files != null && files.Length > 0) FilesDropped?.Invoke(this, new RemoteDropEventArgs(files, target));
        }
    }
}
