using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>One filter chip of a <see cref="FilterChipGroup"/>.</summary>
    [TypeConverter(typeof(FilterChipConverter))]
    public sealed class FilterChip
    {
        public FilterChip() { }

        public FilterChip(string text, TextRole dotRole)
        {
            Text = text;
            DotRole = dotRole;
        }

        public string Text { get; set; }
        public TextRole DotRole { get; set; } = TextRole.Tertiary;
        public bool Active { get; set; } = true;

        public override string ToString() => Text;
    }

    internal sealed class FilterChipConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is FilterChip chip)
            {
                var ctor = typeof(FilterChip).GetConstructor(new[] { typeof(string), typeof(TextRole) });
                return new InstanceDescriptor(ctor, new object[] { chip.Text, chip.DotRole }, false);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// The protocol log's toggle chips: a 7 px dot in the class colour and a label; an active chip
    /// sits on --fill with a hairline, an inactive one is bare and dimmed.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("ChipsChanged")]
    public sealed class FilterChipGroup : ModernControl
    {
        private const int ChipHeight = 28;
        private const int ChipGap = 6;
        private const int ChipPadding = 10;

        private readonly List<FilterChip> _chips = new List<FilterChip>();
        private int _hot = -1;

        public event EventHandler ChipsChanged;

        public FilterChipGroup()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(360, 28);
            Theme.Changed += OnThemeChanged;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<FilterChip> Chips => _chips;

        public bool IsActive(int index) => index >= 0 && index < _chips.Count && _chips[index].Active;

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        private Rectangle ChipBounds(int index)
        {
            int x = 0;
            for (int i = 0; i < _chips.Count; i++)
            {
                int w = ChipPadding * 2 + 7 + 7 + Draw.MeasureWidth(_chips[i].Text, Fonts.Ui(13f));
                if (i == index) return new Rectangle(x, (Height - ChipHeight) / 2, w, ChipHeight);
                x += w + ChipGap;
            }
            return Rectangle.Empty;
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

            var label = new TextLabel();
            for (int i = 0; i < _chips.Count; i++)
            {
                var chip = _chips[i];
                var bounds = ChipBounds(i);
                if (chip.Active)
                {
                    Draw.FillRounded(g, bounds, 4f, p.FillOn(surface));
                    Draw.DrawRounded(g, bounds, 4f, p.StrokeOn(surface));
                }
                else if (i == _hot)
                {
                    Draw.FillRounded(g, bounds, 4f, p.HoverOn(surface));
                }
                var dot = label.RoleColor(chip.DotRole);
                Draw.Dot(g, bounds.X + ChipPadding, bounds.Y + bounds.Height / 2f, 7, chip.Active ? dot : ThemePalette.Flatten(Color.FromArgb(128, dot), surface));
                Draw.Text(g, chip.Text, Fonts.Ui(13f), new Rectangle(bounds.X + ChipPadding + 14, bounds.Y, bounds.Width - ChipPadding - 14, bounds.Height),
                    chip.Active ? p.Foreground : p.Foreground3, Draw.LeftMiddle);
            }
            label.Dispose();
        }

        private int ChipAt(Point point)
        {
            for (int i = 0; i < _chips.Count; i++) if (ChipBounds(i).Contains(point)) return i;
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = ChipAt(e.Location);
            if (hot == _hot) return;
            _hot = hot;
            Cursor = hot >= 0 ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hot == -1) return;
            _hot = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int index = ChipAt(e.Location);
            if (e.Button != MouseButtons.Left || index < 0) return;
            _chips[index].Active = !_chips[index].Active;
            Invalidate();
            ChipsChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A radio choice drawn as a card - the conflict dialog's Overwrite / Overwrite if newer /
    /// Resume / Rename rows. The chosen row takes a 9 % accent fill, a 32 % accent border and a
    /// semibold title. Rows sharing a parent and a <see cref="GroupName"/> exclude each other.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("CheckedChanged")]
    public sealed class OptionRow : ModernControl
    {
        private string _title = "Option";
        private string _description = string.Empty;
        private bool _checked;
        private bool _hot;

        public event EventHandler CheckedChanged;

        public OptionRow()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Size = new Size(560, 48);
            Cursor = Cursors.Hand;
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue("Option")]
        public string Title
        {
            get => _title;
            set { _title = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue("")]
        public string Description
        {
            get => _description;
            set { _description = value ?? string.Empty; Invalidate(); }
        }

        [Category("Behavior"), DefaultValue("")]
        public string GroupName { get; set; } = string.Empty;

        [Category("Behavior"), DefaultValue(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                if (value && Parent != null)
                {
                    foreach (Control sibling in Parent.Controls)
                    {
                        if (sibling is OptionRow other && !ReferenceEquals(other, this) && other.GroupName == GroupName) other.Checked = false;
                    }
                }
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
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

            bool enabled = Enabled;
            Color face, border;
            if (_checked)
            {
                face = ThemePalette.Tint(p.Accent, 0.09, surface);
                border = ThemePalette.Tint(p.Accent, 0.32, surface);
            }
            else
            {
                face = _hot && enabled ? p.Fill2On(surface) : p.FillOn(surface);
                border = p.StrokeOn(surface);
            }
            var bounds = new Rectangle(0, 0, Width, Height);
            Draw.FillRounded(g, bounds, 6f, face);
            Draw.DrawRounded(g, bounds, 6f, border);

            var radio = new RectangleF(16.5f, Height / 2f - 8f, 16f, 16f);
            var oldSmoothing = g.SmoothingMode;
            var oldOffset = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            if (_checked)
            {
                using (var brush = new SolidBrush(p.AccentFill)) g.FillEllipse(brush, radio);
                using (var inner = new SolidBrush(p.AccentForeground)) g.FillEllipse(inner, radio.X + 5f, radio.Y + 5f, 6f, 6f);
            }
            else
            {
                using (var pen = new Pen(enabled ? p.Foreground3 : p.StrokeOn(face), 1.5f)) g.DrawEllipse(pen, radio.X + 0.75f, radio.Y + 0.75f, 14.5f, 14.5f);
            }
            g.SmoothingMode = oldSmoothing;
            g.PixelOffsetMode = oldOffset;

            int x = 44;
            int width = Math.Max(0, Width - x - 14);
            var titleColor = enabled ? p.Foreground : p.Foreground3;
            if (string.IsNullOrEmpty(_description))
            {
                Draw.Text(g, _title, Fonts.Ui(13.5f, _checked), new Rectangle(x, 0, width, Height), titleColor, Draw.LeftMiddle);
            }
            else
            {
                Draw.Text(g, _title, Fonts.Ui(13.5f, _checked), new Rectangle(x, Height / 2 - 18, width, 19), titleColor, Draw.LeftMiddle);
                Draw.Text(g, _description, Fonts.Ui(12f), new Rectangle(x, Height / 2 + 1, width, 17), p.Foreground3, Draw.LeftMiddle);
            }

            if (Focused) Draw.DrawRounded(g, new Rectangle(1, 1, Width - 2, Height - 2), 5f, p.Accent, 2f);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left || !Enabled) return;
            Focus();
            Checked = true;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            OnDoubleClick(EventArgs.Empty);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space && Enabled)
            {
                e.Handled = true;
                Checked = true;
            }
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

    /// <summary>
    /// The pairing chip on the right of the Files / Commits strip: a --lane2 tinted pill reading
    /// "Paired with ~/src/site · master" with a small settings button at its end.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("Click")]
    public sealed class PairingChip : ModernControl
    {
        private const int ButtonSize = 18;
        private string _localPath = string.Empty;
        private string _branch;
        private bool _hot;
        private bool _hotButton;

        /// <summary>The settings button at the end of the chip.</summary>
        public event EventHandler SettingsClick;

        public PairingChip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(260, 28);
            Cursor = Cursors.Hand;
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue("")]
        public string LocalPath
        {
            get => _localPath;
            set { _localPath = value ?? string.Empty; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        public string Branch
        {
            get => _branch;
            set { _branch = value; Invalidate(); }
        }

        public int PreferredWidth
        {
            get
            {
                int width = 10 + 12 + 7 + Draw.MeasureWidth("Paired with", Fonts.Ui(12.5f)) + 6 + Draw.MeasureWidth(_localPath, Fonts.Code(12.5f));
                if (!string.IsNullOrEmpty(_branch)) width += Draw.MeasureWidth("  ·  ", Fonts.Ui(12.5f)) + Draw.MeasureWidth(_branch, Fonts.Ui(12.5f));
                return width + 8 + ButtonSize + 5;
            }
        }

        private Rectangle ButtonBounds => new Rectangle(Width - 5 - ButtonSize, (Height - ButtonSize) / 2, ButtonSize, ButtonSize);

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

            var bounds = new Rectangle(0, 0, Width, Height);
            var face = ThemePalette.Tint(p.Lane2, _hot && !_hotButton ? 0.2 : 0.14, surface);
            Draw.FillRounded(g, bounds, 5f, face);
            Draw.DrawRounded(g, bounds, 5f, ThemePalette.Tint(p.Lane2, 0.32, surface));

            int x = 10;
            IconCache.DrawLeft(g, Icons.Link, 12, p.Lane2, new Rectangle(x, 0, 12, Height));
            x += 12 + 7;
            int limit = ButtonBounds.X - 8;
            x = Run(g, "Paired with", Fonts.Ui(12.5f), p.Foreground2, x, limit) + 6;
            x = Run(g, _localPath, Fonts.Code(12.5f), p.Foreground, x, limit);
            if (!string.IsNullOrEmpty(_branch))
            {
                x = Run(g, "  ·  ", Fonts.Ui(12.5f), p.Foreground3, x, limit);
                Run(g, _branch, Fonts.Ui(12.5f), p.Foreground2, x, limit);
            }

            var button = ButtonBounds;
            if (_hotButton) Draw.FillRounded(g, button, 4f, p.Fill2On(face));
            IconCache.DrawCentered(g, Icons.Settings, 12, p.Foreground2, button.X + button.Width / 2, button.Y + button.Height / 2);
        }

        private int Run(Graphics g, string text, Font font, Color color, int x, int limit)
        {
            if (string.IsNullOrEmpty(text) || x >= limit) return x;
            int w = Math.Min(Draw.MeasureWidth(text, font), limit - x);
            Draw.Text(g, text, font, new Rectangle(x, 0, w + 1, Height), color, Draw.LeftMiddle);
            return x + w;
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hot = false;
            _hotButton = false;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool overButton = ButtonBounds.Contains(e.Location);
            if (overButton == _hotButton) return;
            _hotButton = overButton;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left || !ClientRectangle.Contains(e.Location)) return;
            if (ButtonBounds.Contains(e.Location)) SettingsClick?.Invoke(this, EventArgs.Empty);
            else OnClick(EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
