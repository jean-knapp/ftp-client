using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>A session's connection state, as its tab dot and the status bar show it.</summary>
    public enum SessionState
    {
        /// <summary>No session yet: the new-connection tab, which carries no dot.</summary>
        None,
        Connecting,
        Connected,
        Disconnected,
        Failed,
    }

    /// <summary>One session tab: the site name and its protocol.</summary>
    public sealed class SessionTab
    {
        public string Title { get; set; }
        public string Protocol { get; set; }
        public SessionState State { get; set; }
        public object Tag { get; set; }
    }

    public sealed class TabEventArgs : EventArgs
    {
        public TabEventArgs(int index) { Index = index; }
        public int Index { get; }
    }

    /// <summary>
    /// The 38 px session strip: 30 px tabs carrying a state dot, the site name and its protocol,
    /// a close button on every tab, and a trailing + button.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectedIndexChanged")]
    public sealed class SessionTabStrip : ModernControl
    {
        private const int StripPadding = 12;
        private const int TabHeight = 30;
        private const int TabGap = 4;
        private const int TabPaddingLeft = 12;
        private const int TabPaddingRight = 6;
        private const int DotSize = 7;
        private const int CloseSize = 20;
        private const int AddSize = 30;
        private const int MaxTabWidth = 260;
        private const int MinTabWidth = 128;
        private const int MinProtocolWidth = 40;
        private const int ArrowSize = 24;
        private const int ArrowGap = 9;

        private readonly List<SessionTab> _tabs = new List<SessionTab>();
        private readonly List<int> _widths = new List<int>();
        private int _selectedIndex = -1;
        private int _hotIndex = -1;
        private bool _hotClose;
        private bool _hotAdd;
        private bool _hotLeft;
        private bool _hotRight;

        // Laid out on demand: the widths the tabs ended up with, and how far the row is scrolled.
        private bool _layoutDirty = true;
        private int _layoutWidth = -1;
        private int _contentWidth;
        private int _viewportWidth;
        private bool _overflow;
        private int _scroll;

        public event EventHandler SelectedIndexChanged;
        public event EventHandler<TabEventArgs> TabCloseRequested;
        public event EventHandler<TabEventArgs> TabContextMenuRequested;
        public event EventHandler AddRequested;

        /// <summary>A tab was double-clicked, which renames it.</summary>
        public event EventHandler<TabEventArgs> TabDoubleClick;

        public SessionTabStrip()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Height = 38;
            Theme.Changed += OnThemeChanged;
        }

        [Browsable(false)]
        public IList<SessionTab> Tabs => _tabs;

        [Browsable(false)]
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int clamped = _tabs.Count == 0 ? -1 : Math.Max(0, Math.Min(_tabs.Count - 1, value));
                if (clamped == _selectedIndex) return;
                _selectedIndex = clamped;
                _layoutDirty = true;
                EnsureSelectedVisible();
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Sets the selected index without raising the change event.</summary>
        public void SetSelectedIndexQuiet(int index)
        {
            _selectedIndex = _tabs.Count == 0 ? -1 : Math.Max(-1, Math.Min(_tabs.Count - 1, index));
            _layoutDirty = true;
            EnsureSelectedVisible();
            Invalidate();
        }

        public void SetTabs(IEnumerable<SessionTab> tabs, int selectedIndex)
        {
            _tabs.Clear();
            if (tabs != null) _tabs.AddRange(tabs);
            _selectedIndex = _tabs.Count == 0 ? -1 : Math.Max(0, Math.Min(_tabs.Count - 1, selectedIndex));
            _layoutDirty = true;
            EnsureSelectedVisible();
            Invalidate();
        }

        /// <summary>The dot colour for a state; also used by the status bar and the site manager.</summary>
        public static Color StateColor(SessionState state)
        {
            var p = Theme.Palette;
            switch (state)
            {
                case SessionState.Connected: return p.Up;
                case SessionState.Connecting: return p.Warning;
                case SessionState.Failed: return p.Error;
                default: return p.Foreground3;
            }
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        // ------------------------------------------------------------------ geometry

        /// <summary>The width a tab would like: everything it carries, up to <see cref="MaxTabWidth"/>.</summary>
        private int NaturalWidth(int index)
        {
            var tab = _tabs[index];
            int width = TabPaddingLeft;
            if (tab.State != SessionState.None) width += DotSize + 7;
            // Measured semibold whether or not the tab is active, so selecting one never shifts the others.
            width += Draw.MeasureWidth(tab.Title ?? string.Empty, Fonts.Ui(13f, true));
            if (!string.IsNullOrEmpty(tab.Protocol)) width += 7 + Draw.MeasureWidth(tab.Protocol, Fonts.Ui(12f));
            width += 8 + CloseSize + TabPaddingRight;
            return Math.Min(MaxTabWidth, width);
        }

        /// <summary>
        /// Shares the room left of the + button between the tabs: they shrink to
        /// <see cref="MinTabWidth"/> before the row starts scrolling under the arrows.
        /// </summary>
        private void EnsureLayout()
        {
            if (!_layoutDirty && _layoutWidth == Width) return;
            _layoutDirty = false;
            _layoutWidth = Width;

            int room = Math.Max(0, Width - StripPadding * 2 - AddSize - TabGap);
            _widths.Clear();
            int natural = 0;
            for (int i = 0; i < _tabs.Count; i++)
            {
                int width = NaturalWidth(i);
                _widths.Add(width);
                natural += width + TabGap;
            }
            natural = Math.Max(0, natural - TabGap);

            if (natural > room && _tabs.Count > 0)
            {
                int each = Math.Max(MinTabWidth, (room - TabGap * (_tabs.Count - 1)) / _tabs.Count);
                for (int i = 0; i < _widths.Count; i++) _widths[i] = Math.Min(_widths[i], each);
            }

            _contentWidth = 0;
            foreach (var width in _widths) _contentWidth += width + TabGap;
            _contentWidth = Math.Max(0, _contentWidth - TabGap);
            _overflow = _contentWidth > room;
            _viewportWidth = Math.Max(0, room - (_overflow ? (ArrowSize + ArrowGap) * 2 : 0));
            _scroll = Math.Max(0, Math.Min(_scroll, Math.Max(0, _contentWidth - _viewportWidth)));
        }

        /// <summary>The strip the tabs scroll inside: between the arrows when the row overflows.</summary>
        private Rectangle Viewport
        {
            get
            {
                EnsureLayout();
                return new Rectangle(StripPadding + (_overflow ? ArrowSize + ArrowGap : 0), 0, _viewportWidth, Height);
            }
        }

        private int MaxScroll
        {
            get
            {
                EnsureLayout();
                return Math.Max(0, _contentWidth - _viewportWidth);
            }
        }

        private Rectangle TabBounds(int index)
        {
            EnsureLayout();
            if (index < 0 || index >= _widths.Count) return Rectangle.Empty;
            int x = Viewport.X - _scroll;
            for (int i = 0; i < index; i++) x += _widths[i] + TabGap;
            return new Rectangle(x, (Height - TabHeight) / 2, _widths[index], TabHeight);
        }

        private Rectangle AddBounds()
        {
            EnsureLayout();
            // While the row scrolls the + button stays at the right edge, always in reach.
            int x = _overflow ? Width - StripPadding - AddSize : Math.Min(Viewport.X + _contentWidth + TabGap, Width - StripPadding - AddSize);
            return new Rectangle(x, (Height - AddSize) / 2, AddSize, AddSize);
        }

        private Rectangle LeftArrowBounds()
        {
            EnsureLayout();
            return _overflow ? new Rectangle(StripPadding, (Height - ArrowSize) / 2, ArrowSize, ArrowSize) : Rectangle.Empty;
        }

        private Rectangle RightArrowBounds()
        {
            EnsureLayout();
            return _overflow ? new Rectangle(Viewport.Right + ArrowGap, (Height - ArrowSize) / 2, ArrowSize, ArrowSize) : Rectangle.Empty;
        }

        private Rectangle CloseBounds(int index)
        {
            var bounds = TabBounds(index);
            if (bounds.IsEmpty) return Rectangle.Empty;
            return new Rectangle(bounds.Right - TabPaddingRight - CloseSize, bounds.Y + (TabHeight - CloseSize) / 2, CloseSize, CloseSize);
        }

        public int TabIndexAt(Point point)
        {
            var viewport = Viewport;
            if (point.X < viewport.X || point.X >= viewport.Right) return -1;
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (TabBounds(i).Contains(point)) return i;
            }
            return -1;
        }

        // ------------------------------------------------------------------ scrolling

        private void SetScroll(int value)
        {
            int clamped = Math.Max(0, Math.Min(value, MaxScroll));
            if (clamped == _scroll) return;
            _scroll = clamped;
            Invalidate();
        }

        /// <summary>
        /// Scrolls until the next half-hidden tab on that side is whole. A tab clipped by a sliver
        /// would barely move the row, so the click takes the one after it instead.
        /// </summary>
        private void ScrollOneTab(int direction)
        {
            EnsureLayout();
            var viewport = Viewport;
            if (direction > 0)
            {
                for (int i = 0; i < _tabs.Count; i++)
                {
                    int step = TabBounds(i).Right - viewport.Right;
                    if (step <= 0) continue;
                    if (step < MinTabWidth / 2 && i + 1 < _tabs.Count) step = TabBounds(i + 1).Right - viewport.Right;
                    SetScroll(_scroll + step);
                    return;
                }
                SetScroll(MaxScroll);
                return;
            }
            for (int i = _tabs.Count - 1; i >= 0; i--)
            {
                int step = viewport.X - TabBounds(i).X;
                if (step <= 0) continue;
                if (step < MinTabWidth / 2 && i > 0) step = viewport.X - TabBounds(i - 1).X;
                SetScroll(_scroll - step);
                return;
            }
            SetScroll(0);
        }

        /// <summary>Brings the selected tab into view, e.g. after Ctrl+Tab or opening a site.</summary>
        public void EnsureSelectedVisible()
        {
            EnsureLayout();
            if (_selectedIndex < 0 || !_overflow) return;
            var viewport = Viewport;
            var bounds = TabBounds(_selectedIndex);
            if (bounds.X < viewport.X) SetScroll(_scroll - (viewport.X - bounds.X));
            else if (bounds.Right > viewport.Right) SetScroll(_scroll + (bounds.Right - viewport.Right));
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _layoutDirty = true;
            EnsureSelectedVisible();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            EnsureLayout();
            if (!_overflow) return;
            // A notch moves half a tab, so the row keeps up with the wheel without flying past.
            SetScroll(_scroll - e.Delta * (MinTabWidth / 2) / 120);
        }

        // ------------------------------------------------------------------ painting

        private Color ParentSurface()
        {
            for (var c = Parent; c != null; c = c.Parent)
            {
                if (c is SurfacePanel surface) return surface.SurfaceColor;
            }
            return Theme.Palette.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = ParentSurface();
            Draw.Fill(g, ClientRectangle, surface);

            var viewport = Viewport;
            if (_overflow && viewport.Width > 0)
            {
                // GDI text ignores a clipping region, so the scrolling row goes into a bitmap of its
                // own: no title can spill over the arrows or the + button.
                using (var layer = new Bitmap(viewport.Width, Height))
                using (var layerGraphics = Graphics.FromImage(layer))
                {
                    Draw.Fill(layerGraphics, new Rectangle(0, 0, viewport.Width, Height), surface);
                    PaintTabs(layerGraphics, surface, viewport.X);
                    g.DrawImage(layer, viewport.X, 0);
                }
            }
            else
            {
                PaintTabs(g, surface, 0);
            }

            if (_overflow)
            {
                PaintArrow(g, LeftArrowBounds(), Icons.ChevronLeft, _hotLeft, _scroll > 0, surface);
                PaintArrow(g, RightArrowBounds(), Icons.ChevronRight, _hotRight, _scroll < MaxScroll, surface);
            }

            var add = AddBounds();
            if (add.Right <= Width)
            {
                if (_hotAdd) Draw.FillRounded(g, add, 5f, p.HoverOn(surface));
                IconCache.DrawCentered(g, Icons.Plus, 14, p.Foreground2, add.X + add.Width / 2, add.Y + add.Height / 2);
            }
        }

        /// <summary>Paints the row of tabs, moved left by <paramref name="offsetX"/> when it is scrolled.</summary>
        private void PaintTabs(Graphics g, Color surface, int offsetX)
        {
            var p = Theme.Palette;
            var viewport = Viewport;
            for (int i = 0; i < _tabs.Count; i++)
            {
                var bounds = TabBounds(i);
                if (bounds.IsEmpty || bounds.Right < viewport.X || bounds.X > viewport.Right) continue;
                bounds.Offset(-offsetX, 0);
                bool active = i == _selectedIndex;
                bool hot = i == _hotIndex;

                if (active)
                {
                    Draw.FillRounded(g, bounds, 5f, p.FillOn(surface));
                    Draw.DrawRounded(g, bounds, 5f, p.StrokeOn(surface));
                }
                else if (hot)
                {
                    Draw.FillRounded(g, bounds, 5f, p.HoverOn(surface));
                }

                var tab = _tabs[i];
                int x = bounds.X + TabPaddingLeft;
                int right = bounds.Right - (TabPaddingRight + CloseSize + 8);

                if (tab.State != SessionState.None)
                {
                    Draw.Dot(g, x, bounds.Y + bounds.Height / 2f, DotSize, StateColor(tab.State));
                    x += DotSize + 7;
                }

                var titleFont = Fonts.Ui(13f, active);
                int titleWidth = Math.Min(Draw.MeasureWidth(tab.Title ?? string.Empty, titleFont), Math.Max(0, right - x));
                Draw.Text(g, tab.Title, titleFont, new Rectangle(x, bounds.Y, titleWidth, bounds.Height), active ? p.Foreground : p.Foreground2, Draw.LeftMiddle);
                x += titleWidth + 7;

                // A sliver of a protocol name reads as a typo, so it is shown only when it has room.
                if (!string.IsNullOrEmpty(tab.Protocol) && right - x >= MinProtocolWidth)
                {
                    Draw.Text(g, tab.Protocol, Fonts.Ui(12f), new Rectangle(x, bounds.Y, right - x, bounds.Height), p.Foreground3, Draw.LeftMiddle);
                }

                var close = CloseBounds(i);
                close.Offset(-offsetX, 0);
                bool hotClose = _hotClose && i == _hotIndex;
                if (hotClose) Draw.FillRounded(g, close, 4f, p.Fill2On(active ? p.FillOn(surface) : surface));
                IconCache.DrawCentered(g, Icons.Cross, 10, hotClose ? p.Foreground : p.Foreground3,
                    close.X + close.Width / 2, close.Y + close.Height / 2);
            }

        }

        /// <summary>A scroll arrow, greyed out at the end of the row.</summary>
        private void PaintArrow(Graphics g, Rectangle bounds, string icon, bool hot, bool enabled, Color surface)
        {
            var p = Theme.Palette;
            if (hot && enabled) Draw.FillRounded(g, bounds, 5f, p.HoverOn(surface));
            IconCache.DrawCentered(g, icon, 11, enabled ? (hot ? p.Foreground : p.Foreground2) : p.Foreground3,
                bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = TabIndexAt(e.Location);
            bool hotClose = index >= 0 && CloseBounds(index).Contains(e.Location);
            bool hotAdd = AddBounds().Contains(e.Location);
            bool hotLeft = LeftArrowBounds().Contains(e.Location);
            bool hotRight = RightArrowBounds().Contains(e.Location);
            if (index != _hotIndex || hotClose != _hotClose || hotAdd != _hotAdd || hotLeft != _hotLeft || hotRight != _hotRight)
            {
                _hotIndex = index;
                _hotClose = hotClose;
                _hotAdd = hotAdd;
                _hotLeft = hotLeft;
                _hotRight = hotRight;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex != -1 || _hotAdd || _hotClose || _hotLeft || _hotRight)
            {
                _hotIndex = -1;
                _hotClose = false;
                _hotAdd = false;
                _hotLeft = false;
                _hotRight = false;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Middle)
            {
                int middle = TabIndexAt(e.Location);
                if (middle >= 0) TabCloseRequested?.Invoke(this, new TabEventArgs(middle));
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (LeftArrowBounds().Contains(e.Location))
            {
                ScrollOneTab(-1);
                return;
            }
            if (RightArrowBounds().Contains(e.Location))
            {
                ScrollOneTab(1);
                return;
            }
            if (AddBounds().Contains(e.Location)) return;   // the menu opens on mouse up

            int tab = TabIndexAt(e.Location);
            if (tab < 0) return;
            if (CloseBounds(tab).Contains(e.Location)) return;   // closes on mouse up, like a button
            SelectedIndex = tab;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button != MouseButtons.Left) return;
            int tab = TabIndexAt(e.Location);
            if (tab >= 0 && !CloseBounds(tab).Contains(e.Location)) TabDoubleClick?.Invoke(this, new TabEventArgs(tab));
        }

        // Menus open on mouse up: a pop-up shown while the button is still down closes again as
        // soon as it is released.
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                int index = TabIndexAt(e.Location);
                if (index >= 0) TabContextMenuRequested?.Invoke(this, new TabEventArgs(index));
                return;
            }
            if (e.Button != MouseButtons.Left) return;
            if (AddBounds().Contains(e.Location))
            {
                AddRequested?.Invoke(this, EventArgs.Empty);
                return;
            }
            int tab = TabIndexAt(e.Location);
            if (tab >= 0 && CloseBounds(tab).Contains(e.Location)) TabCloseRequested?.Invoke(this, new TabEventArgs(tab));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
