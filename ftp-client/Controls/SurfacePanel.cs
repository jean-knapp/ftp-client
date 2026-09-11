using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>Which design token supplies a control's background.</summary>
    public enum SurfaceKind
    {
        /// <summary>Window chrome and the body base (--bg).</summary>
        Base,
        /// <summary>Cards and the command bar (--layer).</summary>
        Layer,
        /// <summary>A card: --layer plus a 1 px --cardstroke border and a corner radius.</summary>
        Card,
        /// <summary>Control fill (--fill) with a 1 px --stroke border.</summary>
        Fill,
        /// <summary>Paints nothing; the parent shows through.</summary>
        None,
        /// <summary>Uses <see cref="SurfacePanel.CustomFill"/> and <see cref="SurfacePanel.CustomBorder"/>.</summary>
        Custom,
    }

    /// <summary>
    /// The layered surface from the redesign: a container panel that paints one of the design
    /// tokens, with an optional corner radius and hairline top/bottom dividers. Used for the
    /// command bar, every card, and the banded regions inside them.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Surface")]
    public class SurfacePanel : ModernPanel
    {
        private SurfaceKind _surface = SurfaceKind.Card;
        private int _cornerRadius = 8;
        private bool _topDivider;
        private bool _bottomDivider;
        private bool _rightDivider;
        private bool _topCardStroke;

        public SurfacePanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue(SurfaceKind.Card)]
        public SurfaceKind Surface
        {
            get => _surface;
            set { _surface = value; ApplyBackColor(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(8)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool TopDivider
        {
            get => _topDivider;
            set { _topDivider = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool BottomDivider
        {
            get => _bottomDivider;
            set { _bottomDivider = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool RightDivider
        {
            get => _rightDivider;
            set { _rightDivider = value; Invalidate(); }
        }

        /// <summary>Draws the top rule in --cardstroke instead of --div (the command bar's top edge).</summary>
        [Category("Appearance"), DefaultValue(false)]
        public bool TopCardStroke
        {
            get => _topCardStroke;
            set { _topCardStroke = value; Invalidate(); }
        }

        /// <summary>Fill used when <see cref="Surface"/> is Custom; alpha is blended onto the parent.</summary>
        [Category("Appearance")]
        public Color CustomFill { get; set; } = Color.Transparent;

        /// <summary>Border used when <see cref="Surface"/> is Custom.</summary>
        [Category("Appearance")]
        public Color CustomBorder { get; set; } = Color.Transparent;

        /// <summary>The colour this surface paints, for children that need to blend into it.</summary>
        [Browsable(false)]
        public Color SurfaceColor
        {
            get
            {
                var p = Theme.Palette;
                switch (_surface)
                {
                    case SurfaceKind.Layer:
                    case SurfaceKind.Card: return p.Layer;
                    case SurfaceKind.Fill: return p.FillOn(ParentSurfaceColor());
                    case SurfaceKind.Custom: return ThemePalette.Flatten(CustomFill, ParentSurfaceColor());
                    case SurfaceKind.None: return ParentSurfaceColor();
                    default: return p.Background;
                }
            }
        }

        internal Color ParentSurfaceColor()
        {
            for (var c = Parent; c != null; c = c.Parent)
            {
                var surface = c as SurfacePanel;
                if (surface != null) return surface.SurfaceColor;
            }
            return Theme.Palette.Background;
        }

        protected virtual void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyBackColor();
            Invalidate();
        }

        private void ApplyBackColor()
        {
            // Children that inherit BackColor (and ModernScrollBar's transparent track) need an
            // opaque value to blend against.
            var color = SurfaceColor;
            if (BackColor != color) BackColor = color;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyBackColor();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyBackColor();
        }

        /// <summary>Radius this panel rounds its outline with, 0 when it draws a plain rectangle.</summary>
        private int EffectiveRadius =>
            _surface == SurfaceKind.Card || _surface == SurfaceKind.Fill || _surface == SurfaceKind.Custom ? _cornerRadius : 0;

        /// <summary>
        /// Paints the rounded outline of the nearest rounded ancestor over <paramref name="child"/>.
        /// A child docked into a card covers its corners with an opaque rectangle; instead of
        /// clipping the card to a region - which is a hard mask and leaves the corners jagged -
        /// every child paints back the few pixels outside the card's arc, so the corner keeps the
        /// anti-aliased edge the card drew.
        /// </summary>
        /// <summary>
        /// Finds the nearest rounded card above <paramref name="child"/> and where the child sits
        /// inside it. Returns null when there is no rounded ancestor.
        /// </summary>
        private static SurfacePanel FindRoundedCard(Control child, out int offsetX, out int offsetY)
        {
            offsetX = 0;
            offsetY = 0;
            if (child == null) return null;
            for (var c = child; c != null; c = c.Parent)
            {
                var surface = c as SurfacePanel;
                if (surface != null && !ReferenceEquals(surface, child) && surface.EffectiveRadius > 0)
                {
                    offsetX += child.Left;
                    offsetY += child.Top;
                    return surface;
                }
                if (!ReferenceEquals(c, child))
                {
                    offsetX += c.Left;
                    offsetY += c.Top;
                }
            }
            return null;
        }

        /// <summary>
        /// How far a child's own children (a scroll bar, say) must stay clear of its bottom edge
        /// to leave a rounded card's corner alone. 0 when the child does not sit on that edge.
        /// </summary>
        public static int RoundedParentBottomInset(Control child)
        {
            int offsetX, offsetY;
            var card = FindRoundedCard(child, out offsetX, out offsetY);
            if (card == null) return 0;
            int bottomGap = card.Height - (offsetY + child.Height);
            return bottomGap <= 2 ? card.EffectiveRadius : 0;
        }

        /// <summary>
        /// Paints the rounded outline of the nearest rounded ancestor over <paramref name="child"/>.
        /// A child docked into a card covers its corners with an opaque rectangle; instead of
        /// clipping the card to a region - which is a hard mask and leaves the corners jagged -
        /// every child paints back the few pixels outside the card's arc, so the corner keeps the
        /// anti-aliased edge the card drew.
        /// </summary>
        public static void PaintRoundedParentCorners(Graphics g, Control child)
        {
            if (child == null || g == null) return;

            int offsetX, offsetY;
            var card = FindRoundedCard(child, out offsetX, out offsetY);
            if (card == null) return;

            float radius = card.EffectiveRadius;
            var outline = new RectangleF(-offsetX, -offsetY, card.Width, card.Height);
            // Nothing to do unless this child actually reaches into a corner.
            var corners = new RectangleF(outline.X, outline.Y, outline.Width, outline.Height);
            corners.Inflate(-radius, -radius);
            if (corners.Contains(0, 0) && corners.Contains(child.Width - 1, 0)
                && corners.Contains(0, child.Height - 1) && corners.Contains(child.Width - 1, child.Height - 1))
            {
                return;
            }

            var p = Theme.Palette;
            var outside = card.ParentSurfaceColor();
            var border = card._surface == SurfaceKind.Fill
                ? p.StrokeOn(card.SurfaceColor)
                : (card._surface == SurfaceKind.Custom
                    ? ThemePalette.Flatten(card.CustomBorder, card.SurfaceColor)
                    : p.CardStrokeOn(card.SurfaceColor));

            var savedClip = g.Clip;
            var savedSmoothing = g.SmoothingMode;
            var savedOffset = g.PixelOffsetMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            using (var fillPath = Draw.RoundedRect(outline, radius))
            using (var region = new Region(new RectangleF(0, 0, child.Width, child.Height)))
            {
                region.Exclude(fillPath);
                g.Clip = region;
                using (var brush = new SolidBrush(outside)) g.FillRectangle(brush, 0, 0, child.Width, child.Height);
            }
            g.Clip = savedClip;

            using (var borderPath = Draw.RoundedRect(
                new RectangleF(outline.X + 0.5f, outline.Y + 0.5f, outline.Width - 1f, outline.Height - 1f), radius - 0.5f))
            using (var pen = new Pen(border))
            {
                g.DrawPath(pen, borderPath);
            }

            g.SmoothingMode = savedSmoothing;
            g.PixelOffsetMode = savedOffset;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var bounds = new Rectangle(0, 0, Width, Height);
            var fill = SurfaceColor;
            int radius = EffectiveRadius;

            if (radius == 0)
            {
                // A "None" surface still paints: children with a transparent BackColor ask their
                // parent for the backdrop, and a panel that painted nothing would leave the
                // previous frame behind them.
                Draw.Fill(g, bounds, fill);
            }
            else
            {
                // Same recipe as ModernPanel: clear with what is behind the card, then lay the
                // rounded body and its hairline on top, so the arc keeps its anti-aliased edge.
                var border = _surface == SurfaceKind.Fill
                    ? p.StrokeOn(fill)
                    : (_surface == SurfaceKind.Custom ? ThemePalette.Flatten(CustomBorder, fill) : p.CardStrokeOn(fill));

                var savedSmoothing = g.SmoothingMode;
                var savedOffset = g.PixelOffsetMode;
                var savedQuality = g.CompositingQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                Draw.Fill(g, bounds, ParentSurfaceColor());
                using (var body = Draw.RoundedRect(new RectangleF(0, 0, Width, Height), radius))
                using (var brush = new SolidBrush(fill))
                {
                    g.FillPath(brush, body);
                }
                using (var outline = Draw.RoundedRect(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), radius - 0.5f))
                using (var pen = new Pen(border))
                {
                    g.DrawPath(pen, outline);
                }

                g.SmoothingMode = savedSmoothing;
                g.CompositingQuality = savedQuality;
                g.PixelOffsetMode = savedOffset;
            }

            var divider = p.DividerOn(fill);
            if (_topDivider) Draw.HLine(g, 0, 0, Width, _topCardStroke ? p.CardStrokeOn(fill) : divider);
            if (_bottomDivider) Draw.HLine(g, 0, Height - 1, Width, divider);
            if (_rightDivider) Draw.VLine(g, Width - 1, 0, Height, divider);

            // This panel may itself be sitting in a card's corner.
            PaintRoundedParentCorners(g, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
