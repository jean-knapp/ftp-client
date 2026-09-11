using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The two-part segmented control from the diff header: adjacent halves with the outer corners
    /// rounded, the selected half filled with --fill2 and the other with --fill.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public sealed class SegmentedControl : ModernControl
    {
        private readonly List<string> _items = new List<string>();
        private int _selectedIndex;
        private int _hotIndex = -1;
        private float _textSizePx = 12f;
        private int _segmentPadding = 10;

        public event EventHandler SelectedIndexChanged;

        public SegmentedControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(120, 24);
            Theme.Changed += (s, e) => Invalidate();
        }

        /// <summary>Segment captions, left to right.</summary>
        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public List<string> Items => _items;

        [Category("Behavior"), DefaultValue(0)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = _items.Count == 0 ? 0 : Math.Max(0, Math.Min(_items.Count - 1, value));
                if (clamped == _selectedIndex) return;
                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("Appearance"), DefaultValue(12f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(10)]
        public int SegmentPadding
        {
            get => _segmentPadding;
            set { _segmentPadding = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Width needed for the current captions.</summary>
        public int PreferredWidth
        {
            get
            {
                int width = 0;
                var font = Fonts.Ui(_textSizePx);
                foreach (var item in _items) width += Draw.MeasureWidth(item, font) + _segmentPadding * 2;
                return width + Math.Max(0, _items.Count - 1) * 2;
            }
        }

        private Rectangle SegmentBounds(int index)
        {
            var font = Fonts.Ui(_textSizePx);
            int x = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                int w = Draw.MeasureWidth(_items[i], font) + _segmentPadding * 2;
                if (i == index) return new Rectangle(x, 0, w, Height);
                x += w + 2;
            }
            return Rectangle.Empty;
        }

        private Color ParentSurface()
        {
            for (var c = Parent; c != null; c = c.Parent)
            {
                var surface = c as SurfacePanel;
                if (surface != null) return surface.SurfaceColor;
            }
            return Theme.Palette.Layer;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = ParentSurface();
            var font = Fonts.Ui(_textSizePx);

            for (int i = 0; i < _items.Count; i++)
            {
                var bounds = SegmentBounds(i);
                if (bounds.IsEmpty) continue;
                bool selected = i == _selectedIndex;
                var fill = selected ? p.Fill2On(surface) : p.FillOn(surface);
                if (!selected && i == _hotIndex) fill = p.Fill2On(surface);

                // Only the outer corners of the strip are rounded.
                bool first = i == 0, last = i == _items.Count - 1;
                using (var path = SegmentPath(bounds, 4f, first, last))
                using (var brush = new SolidBrush(fill))
                {
                    var old = g.SmoothingMode;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.FillPath(brush, path);
                    g.SmoothingMode = old;
                }
                Draw.Text(g, _items[i], font, bounds, selected ? p.Foreground : p.Foreground3, Draw.CenterMiddle);
            }
        }

        private static GraphicsPath SegmentPath(Rectangle r, float radius, bool roundLeft, bool roundRight)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            if (roundLeft)
            {
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddLine(r.X + radius, r.Y, r.Right - (roundRight ? radius : 0), r.Y);
            }
            else
            {
                path.AddLine(r.X, r.Y, r.Right - (roundRight ? radius : 0), r.Y);
            }
            if (roundRight)
            {
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddLine(r.Right, r.Y + radius, r.Right, r.Bottom - radius);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            }
            else
            {
                path.AddLine(r.Right, r.Y, r.Right, r.Bottom);
            }
            if (roundLeft)
            {
                path.AddLine(r.Right - (roundRight ? radius : 0), r.Bottom, r.X + radius, r.Bottom);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            }
            else
            {
                path.AddLine(r.Right, r.Bottom, r.X, r.Bottom);
            }
            path.CloseFigure();
            return path;
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
            if (index >= 0) SelectedIndex = index;
        }

        private int IndexAt(Point point)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (SegmentBounds(i).Contains(point)) return i;
            }
            return -1;
        }
    }
}
