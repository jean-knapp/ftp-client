using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>Which text token a label uses.</summary>
    public enum TextRole
    {
        /// <summary>--fg</summary>
        Primary,
        /// <summary>--fg2</summary>
        Secondary,
        /// <summary>--fg3</summary>
        Tertiary,
        /// <summary>--acc</summary>
        Accent,
        /// <summary>--lane</summary>
        Lane,
        /// <summary>--warn</summary>
        Warning,
        /// <summary>--up: uploads, connected, additions</summary>
        Up,
        /// <summary>--down: downloads</summary>
        Down,
        /// <summary>--err: failures, deletions</summary>
        Error,
        /// <summary>--folder</summary>
        Folder,
        /// <summary>--lane2: pairing and git</summary>
        Lane2,
    }

    /// <summary>
    /// A label that takes its colour from a design token and its size in CSS pixels, so the whole
    /// application restyles on a theme switch without touching each control.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Text")]
    public class TextLabel : ModernControl
    {
        private TextRole _role = TextRole.Primary;
        private float _sizePx = 13f;
        private bool _semibold;
        private bool _uppercase;
        private string _iconSvg;
        private int _iconSize = 16;
        private TextRole _iconRole = TextRole.Secondary;
        private int _iconGap = 8;
        private ContentAlignment _align = ContentAlignment.MiddleLeft;

        public TextLabel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue(TextRole.Primary)]
        public TextRole Role
        {
            get => _role;
            set { _role = value; Invalidate(); }
        }

        /// <summary>Font size in CSS pixels, matching the design tokens.</summary>
        [Category("Appearance"), DefaultValue(13f)]
        public float SizePx
        {
            get => _sizePx;
            set { _sizePx = Math.Max(6f, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool Semibold
        {
            get => _semibold;
            set { _semibold = value; Invalidate(); }
        }

        /// <summary>Uppercases the text and adds letter spacing, for the group-header bands.</summary>
        [Category("Appearance"), DefaultValue(false)]
        public bool Uppercase
        {
            get => _uppercase;
            set { _uppercase = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public string IconSvg
        {
            get => _iconSvg;
            set { _iconSvg = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(16)]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(4, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(TextRole.Secondary)]
        public TextRole IconRole
        {
            get => _iconRole;
            set { _iconRole = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(8)]
        public int IconGap
        {
            get => _iconGap;
            set { _iconGap = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(ContentAlignment.MiddleLeft)]
        public ContentAlignment TextAlign
        {
            get => _align;
            set { _align = value; Invalidate(); }
        }

        /// <summary>Wraps the text over several lines instead of ellipsising it.</summary>
        [Category("Appearance"), DefaultValue(false)]
        public bool MultiLine { get; set; }

        [Category("Appearance")]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
        }

        public Color RoleColor(TextRole role)
        {
            var p = Theme.Palette;
            switch (role)
            {
                case TextRole.Secondary: return p.Foreground2;
                case TextRole.Tertiary: return p.Foreground3;
                case TextRole.Accent: return p.Accent;
                case TextRole.Lane: return p.Lane;
                case TextRole.Warning: return p.Warning;
                case TextRole.Up: return p.Up;
                case TextRole.Down: return p.Down;
                case TextRole.Error: return p.Error;
                case TextRole.Folder: return p.Folder;
                case TextRole.Lane2: return p.Lane2;
                default: return p.Foreground;
            }
        }

        /// <summary>Sets the text in the monospace face, for paths and hashes.</summary>
        [Category("Appearance"), DefaultValue(false)]
        public bool Monospace
        {
            get => _monospace;
            set { _monospace = value; Invalidate(); }
        }

        private bool _monospace;

        private Font CurrentFont => _monospace ? Fonts.Code(_sizePx, _semibold) : Fonts.Ui(_sizePx, _semibold);

        /// <summary>Width the label needs for its current text and icon.</summary>
        public int PreferredWidth
        {
            get
            {
                int width = Draw.MeasureWidth(DisplayText, CurrentFont);
                if (!string.IsNullOrEmpty(_iconSvg)) width += _iconSize + _iconGap;
                return width;
            }
        }

        private string DisplayText
        {
            get
            {
                var text = Text ?? string.Empty;
                if (!_uppercase) return text;
                // Approximates the design's 0.04em tracking on uppercase labels.
                var upper = text.ToUpperInvariant();
                if (upper.Length < 2) return upper;
                var sb = new System.Text.StringBuilder(upper.Length * 2);
                for (int i = 0; i < upper.Length; i++)
                {
                    if (i > 0) sb.Append(' ');
                    sb.Append(upper[i]);
                }
                return sb.ToString();
            }
        }

        protected virtual void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var bounds = new Rectangle(0, 0, Width, Height);
            int x = 0;
            if (!string.IsNullOrEmpty(_iconSvg))
            {
                IconCache.DrawLeft(g, _iconSvg, _iconSize, RoleColor(_iconRole), new Rectangle(0, 0, _iconSize, Height));
                x = _iconSize + _iconGap;
                bounds = new Rectangle(x, 0, Math.Max(0, Width - x), Height);
            }

            var flags = MultiLine
                ? TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.WordBreak
                : TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine;
            switch (_align)
            {
                case ContentAlignment.MiddleCenter:
                case ContentAlignment.TopCenter:
                case ContentAlignment.BottomCenter:
                    flags |= TextFormatFlags.HorizontalCenter; break;
                case ContentAlignment.MiddleRight:
                case ContentAlignment.TopRight:
                case ContentAlignment.BottomRight:
                    flags |= TextFormatFlags.Right; break;
                default:
                    flags |= TextFormatFlags.Left; break;
            }
            switch (_align)
            {
                case ContentAlignment.TopLeft:
                case ContentAlignment.TopCenter:
                case ContentAlignment.TopRight:
                    flags |= TextFormatFlags.Top; break;
                case ContentAlignment.BottomLeft:
                case ContentAlignment.BottomCenter:
                case ContentAlignment.BottomRight:
                    flags |= TextFormatFlags.Bottom; break;
                default:
                    flags |= TextFormatFlags.VerticalCenter; break;
            }

            Draw.Text(g, DisplayText, CurrentFont, bounds, RoleColor(_role), flags);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
