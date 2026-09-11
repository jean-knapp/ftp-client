using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    public enum TabBadgeKind
    {
        /// <summary>--fill2 badge, or the accent pair on the selected tab.</summary>
        Default,
        /// <summary>--err at 18 %: the Failed count.</summary>
        Error,
    }

    /// <summary>One tab of a <see cref="CardTabStrip"/>.</summary>
    [TypeConverter(typeof(CardTabConverter))]
    public sealed class CardTab
    {
        public CardTab() { }

        public CardTab(string text, string iconSvg)
        {
            Text = text;
            IconSvg = iconSvg;
        }

        public string Text { get; set; }
        public string IconSvg { get; set; }
        public string Badge { get; set; }
        public TabBadgeKind BadgeKind { get; set; }
        public bool Enabled { get; set; } = true;
        public object Tag { get; set; }

        public override string ToString() => Text;
    }

    /// <summary>Lets the designer write <c>new CardTab("Files", icon)</c> for each tab.</summary>
    internal sealed class CardTabConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor) && value is CardTab tab)
            {
                var ctor = typeof(CardTab).GetConstructor(new[] { typeof(string), typeof(string) });
                return new InstanceDescriptor(ctor, new object[] { tab.Text, tab.IconSvg }, false);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// The pill tabs inside a card: Files and Commits above the browser, Active / Queued /
    /// Completed / Failed in the transfer queue. The selected tab is filled with --fill2 and set
    /// semibold; every tab may carry an 18 px count badge.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public sealed class CardTabStrip : ModernControl
    {
        private const int BadgeHeight = 18;

        private readonly List<CardTab> _tabs = new List<CardTab>();
        private int _selectedIndex;
        private int _hotIndex = -1;
        private int _tabHeight = 30;
        private float _textSizePx = 13.5f;
        private int _iconSize = 14;
        private int _paddingX = 12;
        private int _tabGap = 4;
        private bool _accentSelectedBadge = true;

        public event EventHandler SelectedIndexChanged;

        public CardTabStrip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(260, 42);
            Theme.Changed += OnThemeChanged;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<CardTab> Tabs => _tabs;

        [Category("Behavior"), DefaultValue(0)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = _tabs.Count == 0 ? 0 : Math.Max(0, Math.Min(_tabs.Count - 1, value));
                if (clamped == _selectedIndex) return;
                _selectedIndex = clamped;
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetSelectedIndexQuiet(int index)
        {
            _selectedIndex = _tabs.Count == 0 ? 0 : Math.Max(0, Math.Min(_tabs.Count - 1, index));
            Invalidate();
        }

        [Category("Appearance"), DefaultValue(30)]
        public int TabHeight
        {
            get => _tabHeight;
            set { _tabHeight = Math.Max(16, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(13.5f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(14)]
        public int IconSize
        {
            get => _iconSize;
            set { _iconSize = Math.Max(4, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(12)]
        public int TabPaddingX
        {
            get => _paddingX;
            set { _paddingX = Math.Max(0, value); Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(4)]
        public int TabGap
        {
            get => _tabGap;
            set { _tabGap = Math.Max(0, value); Invalidate(); }
        }

        /// <summary>Paints the selected tab's badge with the accent pair instead of --fill2.</summary>
        [Category("Appearance"), DefaultValue(true)]
        public bool AccentSelectedBadge
        {
            get => _accentSelectedBadge;
            set { _accentSelectedBadge = value; Invalidate(); }
        }

        /// <summary>
        /// Marks the selected tab with a 2 px accent rule under its label instead of a fill - the
        /// site manager's General / Advanced / Transfer tabs.
        /// </summary>
        [Category("Appearance"), DefaultValue(false)]
        public bool Underline { get; set; }

        /// <summary>Updates one tab's count badge; null or empty hides it.</summary>
        public void SetBadge(int index, string badge)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_tabs[index].Badge == badge) return;
            _tabs[index].Badge = badge;
            Invalidate();
        }

        public void SetEnabled(int index, bool enabled)
        {
            if (index < 0 || index >= _tabs.Count) return;
            if (_tabs[index].Enabled == enabled) return;
            _tabs[index].Enabled = enabled;
            Invalidate();
        }

        public int PreferredWidth
        {
            get
            {
                int width = 0;
                for (int i = 0; i < _tabs.Count; i++) width += TabWidth(i) + (i > 0 ? _tabGap : 0);
                return width;
            }
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        // ------------------------------------------------------------------ geometry

        private static int BadgeWidth(string badge) => Math.Max(BadgeHeight, Draw.MeasureWidth(badge, Fonts.Ui(11f, true)) + 10);

        private int TabWidth(int index)
        {
            var tab = _tabs[index];
            int width = _paddingX * 2;
            if (!string.IsNullOrEmpty(tab.IconSvg)) width += _iconSize + 7;
            // Semibold either way, so the strip does not shift when the selection moves.
            width += Draw.MeasureWidth(tab.Text ?? string.Empty, Fonts.Ui(_textSizePx, true));
            if (!string.IsNullOrEmpty(tab.Badge)) width += 7 + BadgeWidth(tab.Badge);
            return width;
        }

        private Rectangle TabBounds(int index)
        {
            int x = 0;
            for (int i = 0; i < _tabs.Count; i++)
            {
                int w = TabWidth(i);
                if (i == index) return new Rectangle(x, (Height - _tabHeight) / 2, w, _tabHeight);
                x += w + _tabGap;
            }
            return Rectangle.Empty;
        }

        public int TabIndexAt(Point point)
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (TabBounds(i).Contains(point)) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------ painting

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
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = ParentSurface();
            Draw.Fill(g, ClientRectangle, surface);

            for (int i = 0; i < _tabs.Count; i++)
            {
                var tab = _tabs[i];
                var bounds = TabBounds(i);
                if (bounds.X > Width) break;
                bool selected = i == _selectedIndex;
                bool hot = i == _hotIndex && tab.Enabled;

                var face = surface;
                if (Underline)
                {
                    if (hot && !selected) Draw.FillRounded(g, bounds, 5f, p.HoverOn(surface));
                    if (selected) Draw.FillRounded(g, new Rectangle(bounds.X + 10, bounds.Bottom - 2, Math.Max(0, bounds.Width - 20), 2), 1f, p.Accent);
                }
                else if (selected)
                {
                    face = p.Fill2On(surface);
                    Draw.FillRounded(g, bounds, 5f, face);
                }
                else if (hot)
                {
                    face = p.HoverOn(surface);
                    Draw.FillRounded(g, bounds, 5f, face);
                }

                var fore = !tab.Enabled ? p.Foreground3 : selected ? p.Foreground : p.Foreground2;
                int x = bounds.X + _paddingX;
                if (!string.IsNullOrEmpty(tab.IconSvg))
                {
                    IconCache.DrawLeft(g, tab.IconSvg, _iconSize, fore, new Rectangle(x, bounds.Y, _iconSize, bounds.Height));
                    x += _iconSize + 7;
                }

                var font = Fonts.Ui(_textSizePx, selected);
                int textWidth = Draw.MeasureWidth(tab.Text ?? string.Empty, font);
                Draw.Text(g, tab.Text, font, new Rectangle(x, bounds.Y, textWidth + 2, bounds.Height), fore, Draw.LeftMiddle);
                x += textWidth + 7;

                if (!string.IsNullOrEmpty(tab.Badge))
                {
                    var badge = new Rectangle(x, bounds.Y + (bounds.Height - BadgeHeight) / 2, BadgeWidth(tab.Badge), BadgeHeight);
                    Color fill, text;
                    if (tab.BadgeKind == TabBadgeKind.Error)
                    {
                        fill = ThemePalette.Tint(p.Error, 0.18, face);
                        text = p.Error;
                    }
                    else if (selected && _accentSelectedBadge)
                    {
                        fill = p.AccentFill;
                        text = p.AccentForeground;
                    }
                    else
                    {
                        fill = p.Fill2On(face);
                        text = tab.Enabled ? p.Foreground2 : p.Foreground3;
                    }
                    Draw.FillRounded(g, badge, BadgeHeight / 2f, fill);
                    Draw.Text(g, tab.Badge, Fonts.Ui(11f, true), badge, text, Draw.CenterMiddle);
                }
            }
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = TabIndexAt(e.Location);
            if (hot != _hotIndex)
            {
                _hotIndex = hot;
                Cursor = hot >= 0 && _tabs[hot].Enabled ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex != -1)
            {
                _hotIndex = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int index = TabIndexAt(e.Location);
            if (index >= 0 && _tabs[index].Enabled) SelectedIndex = index;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
