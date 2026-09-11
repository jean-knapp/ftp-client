using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The hairline between two panes inside one card, made draggable. A split container would put
    /// a 12 px gutter through the card; this keeps the design's single --div rule and resizes the
    /// pane it is docked into.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("PaneResized")]
    public sealed class PaneResizeGrip : ModernControl
    {
        private Control _target;
        private bool _hot;
        private bool _dragging;
        private int _startX;
        private int _startWidth;

        /// <summary>Raised when a drag ends, so the new width can be remembered.</summary>
        public event EventHandler PaneResized;

        public PaneResizeGrip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Cursor = Cursors.VSplit;
            Size = new Size(5, 200);
            Theme.Changed += OnThemeChanged;
        }

        /// <summary>The pane whose width follows the drag; the parent when unset.</summary>
        [Category("Behavior"), DefaultValue(null)]
        public Control Target
        {
            get => _target;
            set => _target = value;
        }

        /// <summary>
        /// The grip sits on the pane's left edge (a pane docked right), so dragging left widens it.
        /// </summary>
        [Category("Behavior"), DefaultValue(false)]
        public bool LeftEdge { get; set; }

        [Category("Behavior"), DefaultValue(180)]
        public int MinimumPaneWidth { get; set; } = 180;

        [Category("Behavior"), DefaultValue(560)]
        public int MaximumPaneWidth { get; set; } = 560;

        /// <summary>Space that must stay free for the pane beside the target.</summary>
        [Category("Behavior"), DefaultValue(360)]
        public int ReservedWidth { get; set; } = 360;

        private Control Pane => _target ?? Parent;

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        private Color ParentSurface()
        {
            for (var c = Parent; c != null; c = c.Parent)
            {
                if (c is SurfacePanel surface) return surface.SurfaceColor;
            }
            return Theme.Palette.Layer;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Palette;
            var surface = ParentSurface();
            Draw.Fill(e.Graphics, ClientRectangle, surface);
            Draw.VLine(e.Graphics, LeftEdge ? 0 : Width - 1, 0, Height, _dragging || _hot ? p.StrokeOn(p.Fill2On(surface)) : p.DividerOn(surface));
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var pane = Pane;
            if (e.Button != MouseButtons.Left || pane == null) return;
            _dragging = true;
            _startX = Cursor.Position.X;
            _startWidth = pane.Width;
            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var pane = Pane;
            if (!_dragging || pane == null) return;
            int max = MaximumPaneWidth;
            if (pane.Parent != null) max = Math.Min(max, pane.Parent.ClientSize.Width - ReservedWidth);
            int delta = Cursor.Position.X - _startX;
            int width = Math.Max(MinimumPaneWidth, Math.Min(Math.Max(MinimumPaneWidth, max), _startWidth + (LeftEdge ? -delta : delta)));
            if (width != pane.Width)
            {
                pane.Width = width;
                pane.Parent?.Update();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (!_dragging) return;
            _dragging = false;
            Capture = false;
            Invalidate();
            PaneResized?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    /// <summary>The 20 px vertical rule between command-bar groups.</summary>
    [ToolboxItem(true)]
    public sealed class ToolbarSeparator : ModernControl
    {
        public ToolbarSeparator()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(17, 32);
            Theme.Changed += OnThemeChanged;
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Palette;
            var surface = p.Layer;
            for (var c = Parent; c != null; c = c.Parent)
            {
                if (c is SurfacePanel panel) { surface = panel.SurfaceColor; break; }
            }
            Draw.Fill(e.Graphics, ClientRectangle, surface);
            Draw.VLine(e.Graphics, Width / 2, (Height - 20) / 2, 20, p.StrokeOn(surface));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    /// <summary>Adjacent 32 px icon toggles - the list / grid view switch.</summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public sealed class IconToggleGroup : ModernControl
    {
        private readonly System.Collections.Generic.List<string> _icons = new System.Collections.Generic.List<string>();
        private int _selectedIndex;
        private int _hotIndex = -1;
        private int _segmentSize = 32;

        public event EventHandler SelectedIndexChanged;

        public IconToggleGroup()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(64, 32);
            Theme.Changed += OnThemeChanged;
        }

        /// <summary>One SVG glyph per segment.</summary>
        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public System.Collections.Generic.List<string> Icons => _icons;

        [Category("Behavior"), DefaultValue(0)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = _icons.Count == 0 ? 0 : Math.Max(0, Math.Min(_icons.Count - 1, value));
                if (clamped == _selectedIndex) return;
                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("Appearance"), DefaultValue(32)]
        public int SegmentSize
        {
            get => _segmentSize;
            set { _segmentSize = Math.Max(16, value); Invalidate(); }
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        private int IndexAt(Point point)
        {
            if (point.Y < 0 || point.Y >= Height) return -1;
            int index = point.X / _segmentSize;
            return index >= 0 && index < _icons.Count ? index : -1;
        }

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
            for (int i = 0; i < _icons.Count; i++)
            {
                var bounds = new Rectangle(i * _segmentSize, (Height - _segmentSize) / 2, _segmentSize, _segmentSize);
                bool selected = i == _selectedIndex;
                if (selected) Draw.FillRounded(g, bounds, 4f, p.Fill2On(surface));
                else if (i == _hotIndex) Draw.FillRounded(g, bounds, 4f, p.HoverOn(surface));
                IconCache.DrawCentered(g, _icons[i], 16, selected ? p.Foreground : p.Foreground2, bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = IndexAt(e.Location);
            if (hot != _hotIndex) { _hotIndex = hot; Invalidate(); }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex != -1) { _hotIndex = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = IndexAt(e.Location);
            if (e.Button == MouseButtons.Left && index >= 0) SelectedIndex = index;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
