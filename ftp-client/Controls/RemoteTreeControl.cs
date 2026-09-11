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
    /// <summary>A directory in the remote tree. Children arrive when the directory is listed.</summary>
    public sealed class RemoteTreeNode
    {
        internal RemoteTreeNode(string path, string name, RemoteTreeNode parent)
        {
            Path = path;
            Name = name;
            Parent = parent;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        public string Path { get; }
        public string Name { get; }
        public RemoteTreeNode Parent { get; }
        public int Depth { get; }
        public bool Expanded { get; internal set; }

        /// <summary>The directory has been listed, so <see cref="Children"/> is complete.</summary>
        public bool Loaded { get; internal set; }

        /// <summary>Items in the directory, shown on the right of the row.</summary>
        public int? ItemCount { get; internal set; }

        /// <summary>A symbolic link that leads to a directory rather than the directory itself.</summary>
        public bool IsLink { get; internal set; }

        public string LinkTarget { get; internal set; }

        internal List<RemoteTreeNode> Children { get; } = new List<RemoteTreeNode>();

        public bool IsRoot => Parent == null;

        /// <summary>Shows a chevron until a listing proves the directory has no subdirectories.</summary>
        public bool CanExpand => !Loaded || Children.Count > 0;
    }

    public sealed class TreeNodeEventArgs : EventArgs
    {
        public TreeNodeEventArgs(RemoteTreeNode node) { Node = node; }
        public RemoteTreeNode Node { get; }
    }

    /// <summary>
    /// The REMOTE TREE pane: 28 px rows indented 16 px a level, a chevron slot, the folder glyph
    /// (open when expanded, accent when selected, the server glyph for the root), the name, a
    /// pairing link badge and the item count on the right.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("NodeSelected")]
    public sealed class RemoteTreeControl : VirtualListControl
    {
        private const int RowHeight = 28;
        private const int Indent = 16;
        private const int LeftPadding = 8;
        private const int TwistySize = 16;
        private const int GlyphSize = 15;

        private readonly List<RemoteTreeNode> _visible = new List<RemoteTreeNode>();
        private RemoteTreeNode _root;
        private bool _showHidden;

        /// <summary>The user picked a directory.</summary>
        public event EventHandler<TreeNodeEventArgs> NodeSelected;

        /// <summary>A directory was expanded that has not been listed yet.</summary>
        public event EventHandler<TreeNodeEventArgs> ExpandRequested;

        public RemoteTreeControl()
        {
            EmptyText = null;
            _root = NewRoot();
            Rebuild(null);
            SelectionChanged += (s, e) =>
            {
                var node = SelectedNode;
                if (node != null) NodeSelected?.Invoke(this, new TreeNodeEventArgs(node));
            };
        }

        /// <summary>Tells the tree which directories carry a pairing badge.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<string, bool> IsPaired { get; set; }

        [Category("Behavior"), DefaultValue(false)]
        public bool ShowHidden
        {
            get => _showHidden;
            set
            {
                if (_showHidden == value) return;
                _showHidden = value;
                Rebuild(SelectedNode?.Path);
            }
        }

        [Browsable(false)]
        public RemoteTreeNode SelectedNode
        {
            get
            {
                int index = SelectedIndex;
                return index >= 0 && index < _visible.Count ? _visible[index] : null;
            }
        }

        private static RemoteTreeNode NewRoot() => new RemoteTreeNode(RemotePath.Root, RemotePath.Root, null) { Expanded = true };

        /// <summary>Forgets every directory, for a new connection.</summary>
        public void Reset()
        {
            _root = NewRoot();
            Rebuild(null);
        }

        public RemoteTreeNode Find(string path)
        {
            var node = _root;
            foreach (var segment in RemotePath.Segments(path))
            {
                node = node.Children.Find(c => string.Equals(c.Name, segment, StringComparison.Ordinal));
                if (node == null) return null;
            }
            return node;
        }

        private RemoteTreeNode Ensure(string path)
        {
            var node = _root;
            var current = string.Empty;
            foreach (var segment in RemotePath.Segments(path))
            {
                current += "/" + segment;
                var child = node.Children.Find(c => string.Equals(c.Name, segment, StringComparison.Ordinal));
                if (child == null)
                {
                    child = new RemoteTreeNode(current, segment, node);
                    node.Children.Add(child);
                    node.Children.Sort(CompareNodes);
                }
                node = child;
            }
            return node;
        }

        private static int CompareNodes(RemoteTreeNode a, RemoteTreeNode b) => NativeSort.Compare(a.Name, b.Name);

        /// <summary>Fills a directory from its listing, keeping what is already known of its subdirectories.</summary>
        public void SetChildren(string path, IEnumerable<RemoteEntry> entries)
        {
            var selected = SelectedNode?.Path;
            var node = Ensure(RemotePath.Normalize(path));
            var list = entries?.Where(e => e.Name != "." && e.Name != "..").ToList() ?? new List<RemoteEntry>();
            node.ItemCount = list.Count;

            var existing = new Dictionary<string, RemoteTreeNode>(StringComparer.Ordinal);
            foreach (var child in node.Children) existing[child.Name] = child;
            node.Children.Clear();
            foreach (var entry in list)
            {
                // Links to folders sit among the folders, in name order.
                if (!entry.OpensAsFolder) continue;
                if (!existing.TryGetValue(entry.Name, out var child)) child = new RemoteTreeNode(RemotePath.Combine(node.Path, entry.Name), entry.Name, node);
                child.IsLink = entry.Kind == RemoteEntryKind.Symlink;
                child.LinkTarget = entry.LinkTarget;
                if (!child.Loaded && entry.ItemCount.HasValue) child.ItemCount = entry.ItemCount;
                node.Children.Add(child);
            }
            node.Children.Sort(CompareNodes);
            node.Loaded = true;
            Rebuild(selected);
        }

        /// <summary>Selects a directory without raising <see cref="NodeSelected"/>, opening its ancestors.</summary>
        public void SelectPath(string path)
        {
            var node = Ensure(RemotePath.Normalize(path));
            var unloaded = new List<RemoteTreeNode>();
            for (var n = node.Parent; n != null; n = n.Parent)
            {
                n.Expanded = true;
                if (!n.Loaded) unloaded.Add(n);
            }
            Rebuild(node.Path);
            EnsureVisible(_visible.IndexOf(node));
            // Ancestors reached by typing a path have not been listed; their siblings fill in later.
            for (int i = unloaded.Count - 1; i >= 0; i--) ExpandRequested?.Invoke(this, new TreeNodeEventArgs(unloaded[i]));
        }

        // Selecting a row opens its directory, so a right-click only opens the menu.
        protected override bool SelectsOnRightClick => false;

        /// <summary>The directory on a row, e.g. the one a right-click landed on.</summary>
        public RemoteTreeNode NodeAt(int index) => index >= 0 && index < _visible.Count ? _visible[index] : null;

        /// <summary>Scrolls the selected directory into view, e.g. after listings above it filled in.</summary>
        public void RevealSelected()
        {
            int index = SelectedIndex;
            if (index >= 0) EnsureVisible(index);
        }

        /// <summary>Opens a listed directory so its subdirectories show, e.g. the one being browsed.</summary>
        public void Expand(string path)
        {
            var node = Find(RemotePath.Normalize(path));
            if (node == null || node.Expanded) return;
            var selected = SelectedNode?.Path;
            node.Expanded = true;
            Rebuild(selected);
        }

        public void Toggle(RemoteTreeNode node)
        {
            if (node == null || node.IsRoot) return;
            var selected = SelectedNode?.Path;
            node.Expanded = !node.Expanded;
            Rebuild(selected);
            if (node.Expanded && !node.Loaded) ExpandRequested?.Invoke(this, new TreeNodeEventArgs(node));
        }

        public void CollapseAll()
        {
            var selected = SelectedNode?.Path;
            Collapse(_root);
            _root.Expanded = true;
            Rebuild(selected);
        }

        private static void Collapse(RemoteTreeNode node)
        {
            node.Expanded = false;
            foreach (var child in node.Children) Collapse(child);
        }

        private void Rebuild(string selectedPath)
        {
            _visible.Clear();
            Flatten(_root);
            int index = selectedPath == null ? -1 : _visible.FindIndex(n => n.Path == selectedPath);
            RestoreState(index, ScrollOffset);
            ContentChanged();
        }

        private void Flatten(RemoteTreeNode node)
        {
            _visible.Add(node);
            if (!node.Expanded) return;
            foreach (var child in node.Children)
            {
                if (!_showHidden && child.Name.StartsWith(".", StringComparison.Ordinal)) continue;
                Flatten(child);
            }
        }

        // ------------------------------------------------------------------ list model

        protected override int RowCount => _visible.Count;
        protected override int GetRowHeight(int index) => RowHeight;
        protected override int DefaultScrollStep => RowHeight;

        private static Rectangle TwistyBounds(RemoteTreeNode node, int top)
        {
            return new Rectangle(LeftPadding + node.Depth * Indent, top, TwistySize, RowHeight);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var node = _visible[index];
            bool selected = (state & RowState.Selected) != 0;

            if (selected)
            {
                Draw.Fill(g, bounds, p.SelectionOn(surface));
                Draw.FillRounded(g, new Rectangle(bounds.X, bounds.Y + 4, 3, bounds.Height - 8), 1.5f, p.AccentFill);
            }
            else if ((state & RowState.Hot) != 0)
            {
                Draw.Fill(g, bounds, p.HoverOn(surface));
            }

            int x = LeftPadding + node.Depth * Indent;
            if (!node.IsRoot && node.CanExpand)
            {
                IconCache.DrawCentered(g, node.Expanded ? Icons.ChevronDown : Icons.ChevronRight, 10, p.Foreground3, x + TwistySize / 2, bounds.Y + bounds.Height / 2);
            }
            x += TwistySize;

            string glyph;
            Color glyphColor;
            if (node.IsRoot)
            {
                glyph = Icons.Server;
                glyphColor = p.Lane;
            }
            else if (node.IsLink)
            {
                glyph = Icons.FolderLink;
                glyphColor = selected ? p.Accent : p.Lane2;
            }
            else
            {
                glyph = node.Expanded ? Icons.FolderOpen : Icons.Folder;
                glyphColor = selected ? p.Accent : p.Folder;
            }
            IconCache.DrawLeft(g, glyph, GlyphSize, glyphColor, new Rectangle(x, bounds.Y, GlyphSize, bounds.Height));
            x += GlyphSize + 7;

            int right = bounds.Right - 12;
            if (node.ItemCount.HasValue && !node.IsRoot)
            {
                var countFont = Fonts.Ui(11.5f);
                var count = node.ItemCount.Value.ToString("N0");
                int w = Draw.MeasureWidth(count, countFont);
                Draw.Text(g, count, countFont, new Rectangle(right - w, bounds.Y, w + 2, bounds.Height), p.Foreground3, Draw.LeftMiddle);
                right -= w + 8;
            }

            bool paired = IsPaired != null && IsPaired(node.Path);
            var nameFont = Fonts.Ui(13f, selected);
            int nameWidth = Math.Min(Draw.MeasureWidth(node.Name, nameFont), Math.Max(0, right - x - (paired ? 17 : 0)));
            Draw.Text(g, node.Name, nameFont, new Rectangle(x, bounds.Y, nameWidth + 1, bounds.Height), selected ? p.Foreground : p.Foreground2, Draw.LeftMiddle);
            if (paired)
            {
                IconCache.DrawLeft(g, Icons.Link, 12, p.Lane2, new Rectangle(x + nameWidth + 5, bounds.Y, 12, bounds.Height));
            }
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int index = RowIndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0)
            {
                var node = _visible[index];
                if (!node.IsRoot && node.CanExpand && TwistyBounds(node, RowTop(index)).Contains(e.Location))
                {
                    Focus();
                    Toggle(node);
                    return;
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int index = RowIndexAt(e.Location);
            if (index >= 0) Toggle(_visible[index]);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            var key = keyData & Keys.KeyCode;
            if (key == Keys.Left || key == Keys.Right) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            var node = SelectedNode;
            if (node != null && e.KeyCode == Keys.Right)
            {
                e.Handled = true;
                if (!node.Expanded) Toggle(node);
                else if (node.Children.Count > 0)
                {
                    int child = _visible.IndexOf(node) + 1;
                    SetSelection(child, true);
                    EnsureVisible(child);
                }
                return;
            }
            if (node != null && e.KeyCode == Keys.Left)
            {
                e.Handled = true;
                if (node.Expanded && !node.IsRoot) Toggle(node);
                else if (node.Parent != null)
                {
                    int parent = _visible.IndexOf(node.Parent);
                    SetSelection(parent, true);
                    EnsureVisible(parent);
                }
                return;
            }
            base.OnKeyDown(e);
        }
    }

    /// <summary>Explorer's ordering: <c>file2</c> before <c>file10</c>, case ignored.</summary>
    internal static class NativeSort
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string x, string y);

        public static int Compare(string a, string b)
        {
            try
            {
                return StrCmpLogicalW(a ?? string.Empty, b ?? string.Empty);
            }
            catch (EntryPointNotFoundException)
            {
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
