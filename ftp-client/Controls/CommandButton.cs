using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>Lays out a dialog's footer buttons at the width their captions need.</summary>
    public static class ButtonRow
    {
        /// <summary>
        /// Sizes each button to its content (never below <paramref name="minWidth"/>) and lines them
        /// up so the last one ends at <paramref name="right"/>. Tops and heights are kept.
        /// </summary>
        public static void ArrangeRight(int right, int gap, int minWidth, params CommandButton[] buttons)
        {
            for (int i = buttons.Length - 1; i >= 0; i--)
            {
                var button = buttons[i];
                if (button == null) continue;
                int width = Math.Max(minWidth, button.PreferredWidth);
                button.SetBounds(right - width, button.Top, width, button.Height);
                right -= width + gap;
            }
        }
    }

    /// <summary>The three button treatments in the redesign.</summary>
    public enum ButtonAppearance
    {
        /// <summary>Transparent until hovered (command-bar actions, header actions).</summary>
        Subtle,
        /// <summary>--fill with a 1 px --stroke border.</summary>
        Standard,
        /// <summary>--accfill with --accfg text; the primary action.</summary>
        Accent,
    }

    /// <summary>How a button's count badge is filled.</summary>
    public enum BadgeAppearance
    {
        /// <summary>--accfill with --accfg text.</summary>
        Accent,
        /// <summary>A muted foreground tint: the Download count.</summary>
        Neutral,
    }

    public enum ChevronMode
    {
        None,
        /// <summary>Chevron sits right after the label.</summary>
        Inline,
        /// <summary>Chevron is pushed to the right edge (the branch picker).</summary>
        Trailing,
        /// <summary>Chevron gets its own hit area behind a divider (a split button).</summary>
        Split,
    }

    /// <summary>
    /// The command bar / card button from the redesign: an icon, a label, an optional count badge
    /// and an optional chevron that can be a separate split half.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Text")]
    [DefaultEvent("Click")]
    public class CommandButton : ModernControl
    {
        private ButtonAppearance _appearance = ButtonAppearance.Subtle;
        private string _iconSvg;
        private Image _iconImage;
        private int _iconSize = 16;
        private TextRole _iconRole = TextRole.Secondary;
        private float _textSizePx = 13f;
        private bool _semibold;
        private string _badgeText;
        private ChevronMode _chevron = ChevronMode.None;
        private int _splitWidth = 26;
        private int _paddingX = 10;
        private int _gap = 8;
        private int _cornerRadius = 4;
        private ModernContextMenu _dropDownMenu;

        private bool _hot;
        private bool _hotChevron;
        private bool _pressed;

        /// <summary>Raised when the chevron half of a split button is clicked.</summary>
        public event EventHandler ChevronClick;

        public CommandButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            // Control raises Click from its own WM_LBUTTONUP handling, before OnMouseUp runs, so a
            // click on the chevron half of a split button would fire the main action as well.
            // Clicks are raised here instead.
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            TabStop = true;
            BackColor = Color.Transparent;
            Size = new Size(96, 32);
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue(ButtonAppearance.Subtle)]
        public ButtonAppearance Appearance
        {
            get => _appearance;
            set { _appearance = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public string IconSvg
        {
            get => _iconSvg;
            set { _iconSvg = value; Invalidate(); }
        }

        /// <summary>A bitmap shown in place of <see cref="IconSvg"/> - an avatar, for instance.</summary>
        [Category("Appearance"), DefaultValue(null)]
        public Image IconImage
        {
            get => _iconImage;
            set { _iconImage = value; Invalidate(); }
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

        [Category("Appearance"), DefaultValue(13f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(false)]
        public bool Semibold
        {
            get => _semibold;
            set { _semibold = value; Invalidate(); }
        }

        /// <summary>Count pill after the label; hidden when empty.</summary>
        [Category("Appearance"), DefaultValue(null)]
        public string BadgeText
        {
            get => _badgeText;
            set { _badgeText = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(BadgeAppearance.Accent)]
        public BadgeAppearance BadgeStyle { get; set; } = BadgeAppearance.Accent;

        [Category("Appearance"), DefaultValue(ChevronMode.None)]
        public ChevronMode Chevron
        {
            get => _chevron;
            set { _chevron = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(26)]
        public int SplitWidth
        {
            get => _splitWidth;
            set { _splitWidth = Math.Max(12, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(10)]
        public int PaddingX
        {
            get => _paddingX;
            set { _paddingX = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(8)]
        public int Gap
        {
            get => _gap;
            set { _gap = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(4)]
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Menu opened by the chevron, or by the whole button when it is not a split button.</summary>
        [Category("Behavior"), DefaultValue(null)]
        public ModernContextMenu DropDownMenu
        {
            get => _dropDownMenu;
            set => _dropDownMenu = value;
        }

        [Category("Appearance")]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
        }

        /// <summary>Width this button needs for its content.</summary>
        public int PreferredWidth
        {
            get
            {
                int width = _paddingX * 2;
                bool hasIcon = _iconImage != null || !string.IsNullOrEmpty(_iconSvg);
                bool hasText = !string.IsNullOrEmpty(Text);
                if (hasIcon) width += _iconSize;
                if (hasIcon && hasText) width += _gap;
                // Measured in the face it is painted in (accent buttons are always semibold), with
                // a little slack so the ellipsis never kicks in at the exact width.
                if (hasText) width += Draw.MeasureWidth(Text, Fonts.Ui(_textSizePx, _semibold || _appearance == ButtonAppearance.Accent)) + 2;
                if (!string.IsNullOrEmpty(_badgeText)) width += _gap + BadgeWidth;
                if (_chevron == ChevronMode.Inline) width += _gap + 10;
                else if (_chevron == ChevronMode.Trailing) width += _gap + 10;
                else if (_chevron == ChevronMode.Split) width += _splitWidth;
                return width;
            }
        }

        private int BadgeWidth => Math.Max(18, Draw.MeasureWidth(_badgeText ?? string.Empty, Fonts.Ui(11f, true)) + 12);

        private Rectangle ChevronArea =>
            _chevron == ChevronMode.Split ? new Rectangle(Width - _splitWidth, 0, _splitWidth, Height) : Rectangle.Empty;

        protected virtual void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        // ------------------------------------------------------------------ painting

        private Color BlendDisabled(Color color, Color surface)
        {
            // The design dims disabled controls to 45 %.
            return ThemePalette.Flatten(Color.FromArgb(115, color), surface);
        }

        private Color ParentSurface()
        {
            for (var c = Parent; c != null; c = c.Parent)
            {
                var surface = c as SurfacePanel;
                if (surface != null) return surface.SurfaceColor;
            }
            return Theme.Palette.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = ParentSurface();
            var bounds = new Rectangle(0, 0, Width, Height);
            bool enabled = Enabled;

            Color face, border, fore, iconColor;
            switch (_appearance)
            {
                case ButtonAppearance.Accent:
                    face = p.AccentFill;
                    if (_pressed) face = ControlPaint.Dark(face, 0.06f);
                    else if (_hot) face = ControlPaint.Light(face, 0.06f);
                    if (!enabled) face = BlendDisabled(p.AccentFill, surface);
                    border = Color.Transparent;
                    fore = enabled ? p.AccentForeground : BlendDisabled(p.AccentForeground, face);
                    iconColor = fore;
                    break;
                case ButtonAppearance.Standard:
                    face = _pressed ? p.FillOn(surface) : (_hot ? p.Fill2On(surface) : p.FillOn(surface));
                    border = p.StrokeOn(surface);
                    fore = enabled ? p.Foreground : p.Foreground3;
                    iconColor = enabled ? RoleColor(_iconRole) : p.Foreground3;
                    break;
                default:
                    face = _pressed ? p.Fill2On(surface) : (_hot ? p.HoverOn(surface) : Color.Transparent);
                    border = Color.Transparent;
                    fore = enabled ? p.Foreground2 : p.Foreground3;
                    iconColor = enabled ? RoleColor(_iconRole) : p.Foreground3;
                    break;
            }

            if (face.A > 0) Draw.FillRounded(g, bounds, _cornerRadius, face);
            if (border.A > 0) Draw.DrawRounded(g, new Rectangle(0, 0, Width - 1, Height - 1), _cornerRadius, border);

            var chevronArea = ChevronArea;
            int contentRight = chevronArea.IsEmpty ? Width - _paddingX : chevronArea.X - _paddingX / 2;
            int x = _paddingX;

            if (_iconImage != null)
            {
                var old = g.InterpolationMode;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(_iconImage, new Rectangle(x, (Height - _iconSize) / 2, _iconSize, _iconSize));
                g.InterpolationMode = old;
                x += _iconSize;
                if (!string.IsNullOrEmpty(Text)) x += _gap;
            }
            else if (!string.IsNullOrEmpty(_iconSvg))
            {
                IconCache.DrawLeft(g, _iconSvg, _iconSize, iconColor, new Rectangle(x, 0, _iconSize, Height));
                x += _iconSize;
                if (!string.IsNullOrEmpty(Text)) x += _gap;
            }

            int reserved = 0;
            if (!string.IsNullOrEmpty(_badgeText)) reserved += BadgeWidth + _gap;
            if (_chevron == ChevronMode.Inline || _chevron == ChevronMode.Trailing) reserved += 10 + _gap;

            if (!string.IsNullOrEmpty(Text))
            {
                var font = Fonts.Ui(_textSizePx, _semibold || _appearance == ButtonAppearance.Accent);
                int available = Math.Max(0, contentRight - x - reserved);
                Draw.Text(g, Text, font, new Rectangle(x, 0, available, Height), fore, Draw.LeftMiddle);
                x += Math.Min(available, Draw.MeasureWidth(Text, font));
            }

            if (!string.IsNullOrEmpty(_badgeText))
            {
                x += _gap;
                int badgeW = BadgeWidth;
                var badgeRect = new Rectangle(x, (Height - 18) / 2, badgeW, 18);
                var badgeFill = _appearance == ButtonAppearance.Accent ? p.AccentForeground : p.AccentFill;
                var badgeFore = _appearance == ButtonAppearance.Accent ? p.AccentFill : p.AccentForeground;
                if (BadgeStyle == BadgeAppearance.Neutral)
                {
                    badgeFill = ThemePalette.Tint(p.Foreground, p.Mode == ThemeMode.Light ? 0.12 : 0.2, face.A > 0 ? face : surface);
                    badgeFore = p.Foreground;
                }
                Draw.FillRounded(g, badgeRect, 9f, enabled ? badgeFill : BlendDisabled(badgeFill, surface));
                Draw.Text(g, _badgeText, Fonts.Ui(11f, true), badgeRect, badgeFore, Draw.CenterMiddle);
                x += badgeW;
            }

            if (_chevron == ChevronMode.Inline)
            {
                IconCache.DrawCentered(g, Icons.ChevronDown, 10, enabled ? p.Foreground3 : p.Foreground3, x + _gap + 5, Height / 2);
            }
            else if (_chevron == ChevronMode.Trailing)
            {
                IconCache.DrawCentered(g, Icons.ChevronDown, 10, enabled ? p.Foreground3 : p.Foreground3, Width - _paddingX - 5, Height / 2);
            }
            else if (_chevron == ChevronMode.Split)
            {
                if (_hotChevron && enabled)
                {
                    var hotRect = new Rectangle(chevronArea.X, 1, chevronArea.Width - 1, Height - 2);
                    Draw.FillRounded(g, hotRect, _cornerRadius, p.Fill2On(surface));
                }
                Draw.VLine(g, chevronArea.X, (Height - 18) / 2, 18, p.DividerOn(face.A > 0 ? face : surface));
                IconCache.DrawCentered(g, Icons.ChevronDown, 10, enabled ? p.Foreground3 : p.Foreground3,
                    chevronArea.X + chevronArea.Width / 2, Height / 2);
            }

            if (Focused && enabled)
            {
                Draw.DrawRounded(g, new Rectangle(1, 1, Width - 3, Height - 3), _cornerRadius, p.Accent, 2f);
            }
        }

        private Color RoleColor(TextRole role)
        {
            var p = Theme.Palette;
            switch (role)
            {
                case TextRole.Primary: return p.Foreground;
                case TextRole.Tertiary: return p.Foreground3;
                case TextRole.Accent: return p.Accent;
                case TextRole.Lane: return p.Lane;
                case TextRole.Warning: return p.Warning;
                case TextRole.Up: return p.Up;
                case TextRole.Down: return p.Down;
                case TextRole.Error: return p.Error;
                case TextRole.Folder: return p.Folder;
                case TextRole.Lane2: return p.Lane2;
                default: return p.Foreground2;
            }
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _hot = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hot = false;
            _hotChevron = false;
            _pressed = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool overChevron = ChevronArea.Contains(e.Location);
            if (overChevron != _hotChevron)
            {
                _hotChevron = overChevron;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _pressed = true;
            Focus();
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool wasPressed = _pressed;
            _pressed = false;
            Invalidate();
            if (e.Button != MouseButtons.Left || !wasPressed || !ClientRectangle.Contains(e.Location))
            {
                base.OnMouseUp(e);
                return;
            }

            if (_chevron == ChevronMode.Split && ChevronArea.Contains(e.Location))
            {
                base.OnMouseUp(e);
                ChevronClick?.Invoke(this, EventArgs.Empty);
                ShowDropDown();
                return;
            }

            base.OnMouseUp(e);
            if (!Enabled) return;

            // Click first, then the menu: a handler is what fills a menu whose items depend on the
            // moment it opens, and ShowDropDown does nothing while the menu is empty.
            OnClick(EventArgs.Empty);
            if (_chevron != ChevronMode.Split && _dropDownMenu != null) ShowDropDown();
        }

        /// <summary>Opens the drop-down menu flush under the button.</summary>
        public void ShowDropDown()
        {
            if (_dropDownMenu == null || _dropDownMenu.Items.Count == 0) return;
            _dropDownMenu.Show(this, PointToScreen(new Point(0, Height + 2)));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Space || (keyData & Keys.KeyCode) == Keys.Enter) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                PerformClick();
            }
            else if (e.KeyCode == Keys.Down && _dropDownMenu != null)
            {
                e.Handled = true;
                ShowDropDown();
            }
        }

        public void PerformClick()
        {
            if (!Enabled) return;
            OnClick(EventArgs.Empty);
            if (_chevron != ChevronMode.Split && _dropDownMenu != null) ShowDropDown();
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
