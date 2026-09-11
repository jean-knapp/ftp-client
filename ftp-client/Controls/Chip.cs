using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>Which token pair fills a chip.</summary>
    public enum ChipStyle
    {
        /// <summary>--fill2 background with --fg2 text: the Changes count badge.</summary>
        Neutral,
        /// <summary>--fill background with --fg2 text: the sha chip.</summary>
        Subtle,
        /// <summary>Translucent lane fill with --lane text: the branch chip.</summary>
        Lane,
        /// <summary>--accfill background with --accfg text.</summary>
        Accent,
        /// <summary>--up at 14 % with --up text: the conflict dialog's "Newer".</summary>
        Up,
    }

    /// <summary>A small rounded chip carrying a count, a sha or a branch name.</summary>
    [ToolboxItem(true)]
    [DefaultProperty("Text")]
    public sealed class Chip : ModernControl
    {
        private ChipStyle _style = ChipStyle.Neutral;
        private float _textSizePx = 11f;
        private bool _semibold = true;
        private bool _monospace;
        private string _iconSvg;
        private int _iconSize = 11;
        private int _cornerRadius = 9;
        private int _paddingX = 6;
        private bool _autoWidth = true;

        public Chip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(24, 18);
            Theme.Changed += (s, e) => { UpdateWidth(); Invalidate(); };
        }

        [Category("Appearance"), DefaultValue(ChipStyle.Neutral)]
        public ChipStyle Style
        {
            get => _style;
            set { _style = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(11f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); UpdateWidth(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(true)]
        public bool Semibold
        {
            get => _semibold;
            set { _semibold = value; UpdateWidth(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool Monospace
        {
            get => _monospace;
            set { _monospace = value; UpdateWidth(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public string IconSvg
        {
            get => _iconSvg;
            set { _iconSvg = value; UpdateWidth(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(11)]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(4, value); UpdateWidth(); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(9)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(6)]
        public int PaddingX
        {
            get => _paddingX;
            set { _paddingX = Math.Max(0, value); UpdateWidth(); Invalidate(); }
        }

        /// <summary>Resizes the chip to fit its content whenever the text changes.</summary>
        [Category("Layout"), DefaultValue(true)]
        public bool AutoWidth
        {
            get => _autoWidth;
            set { _autoWidth = value; UpdateWidth(); }
        }

        [Category("Appearance")]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; UpdateWidth(); Invalidate(); }
        }

        private Font ChipFont => _monospace ? Fonts.Code(_textSizePx, _semibold) : Fonts.Ui(_textSizePx, _semibold);

        public int PreferredWidth
        {
            get
            {
                int width = _paddingX * 2 + Draw.MeasureWidth(Text, ChipFont);
                if (!string.IsNullOrEmpty(_iconSvg)) width += _iconSize + 5;
                return Math.Max(18, width);
            }
        }

        private void UpdateWidth()
        {
            if (!_autoWidth) return;
            int width = PreferredWidth;
            if (Width != width) Width = width;
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
            var bounds = new Rectangle(0, 0, Width, Height);

            Color fill, fore;
            switch (_style)
            {
                case ChipStyle.Subtle: fill = p.FillOn(surface); fore = p.Foreground2; break;
                case ChipStyle.Lane:
                    fill = ThemePalette.Flatten(Color.FromArgb(p.Mode == ThemeMode.Light ? 31 : 41, p.Lane), surface);
                    fore = p.Lane;
                    break;
                case ChipStyle.Accent: fill = p.AccentFill; fore = p.AccentForeground; break;
                case ChipStyle.Up: fill = ThemePalette.Tint(p.Up, 0.14, surface); fore = p.Up; break;
                default: fill = p.Fill2On(surface); fore = p.Foreground2; break;
            }

            Draw.FillRounded(g, bounds, _cornerRadius, fill);

            int x = _paddingX;
            if (!string.IsNullOrEmpty(_iconSvg))
            {
                IconCache.DrawLeft(g, _iconSvg, _iconSize, fore, new Rectangle(x, 0, _iconSize, Height));
                x += _iconSize + 5;
            }
            Draw.Text(g, Text, ChipFont, new Rectangle(x, 0, Math.Max(0, Width - x - _paddingX), Height), fore,
                string.IsNullOrEmpty(_iconSvg) ? Draw.CenterMiddle : Draw.LeftMiddle);
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            UpdateWidth();
        }
    }

    /// <summary>A circular avatar showing a person's initials, used in the commit detail header.</summary>
    [ToolboxItem(true)]
    public sealed class AvatarBox : ModernControl
    {
        private string _personName = string.Empty;
        private float _textSizePx = 13f;

        public AvatarBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(38, 38);
            Theme.Changed += (s, e) => Invalidate();
        }

        [Category("Appearance"), DefaultValue("")]
        public string PersonName
        {
            get => _personName;
            set { _personName = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(13f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Palette;
            if (string.IsNullOrEmpty(_personName)) return;
            var fill = Color.FromArgb(p.Mode == ThemeMode.Light ? 36 : 51, p.Lane2);
            Draw.Avatar(e.Graphics, new Rectangle(0, 0, Width - 1, Height - 1), Draw.Initials(_personName),
                Fonts.Ui(_textSizePx, true), fill, p.Lane2);
        }
    }
}
