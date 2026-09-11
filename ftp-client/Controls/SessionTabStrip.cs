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

        private readonly List<SessionTab> _tabs = new List<SessionTab>();
        private int _selectedIndex = -1;
        private int _hotIndex = -1;
        private bool _hotClose;
        private bool _hotAdd;

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
                Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>Sets the selected index without raising the change event.</summary>
        public void SetSelectedIndexQuiet(int index)
        {
            _selectedIndex = _tabs.Count == 0 ? -1 : Math.Max(-1, Math.Min(_tabs.Count - 1, index));
            Invalidate();
        }

        public void SetTabs(IEnumerable<SessionTab> tabs, int selectedIndex)
        {
            _tabs.Clear();
            if (tabs != null) _tabs.AddRange(tabs);
            _selectedIndex = _tabs.Count == 0 ? -1 : Math.Max(0, Math.Min(_tabs.Count - 1, selectedIndex));
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

        private int TabWidth(int index)
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

        private Rectangle TabBounds(int index)
        {
            int x = StripPadding;
            for (int i = 0; i < _tabs.Count; i++)
            {
                int w = TabWidth(i);
                if (i == index) return new Rectangle(x, (Height - TabHeight) / 2, w, TabHeight);
                x += w + TabGap;
            }
            return Rectangle.Empty;
        }

        private Rectangle AddBounds()
        {
            int x = StripPadding;
            for (int i = 0; i < _tabs.Count; i++) x += TabWidth(i) + TabGap;
            return new Rectangle(x, (Height - AddSize) / 2, AddSize, AddSize);
        }

        private Rectangle CloseBounds(int index)
        {
            var bounds = TabBounds(index);
            if (bounds.IsEmpty) return Rectangle.Empty;
            return new Rectangle(bounds.Right - TabPaddingRight - CloseSize, bounds.Y + (TabHeight - CloseSize) / 2, CloseSize, CloseSize);
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
            return Theme.Palette.Background;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var p = Theme.Palette;
            var surface = ParentSurface();
            Draw.Fill(g, ClientRectangle, surface);

            for (int i = 0; i < _tabs.Count; i++)
            {
                var bounds = TabBounds(i);
                if (bounds.IsEmpty || bounds.X > Width) continue;
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

                if (!string.IsNullOrEmpty(tab.Protocol) && x < right)
                {
                    Draw.Text(g, tab.Protocol, Fonts.Ui(12f), new Rectangle(x, bounds.Y, Math.Max(0, right - x), bounds.Height), p.Foreground3, Draw.LeftMiddle);
                }

                var close = CloseBounds(i);
                bool hotClose = _hotClose && i == _hotIndex;
                if (hotClose) Draw.FillRounded(g, close, 4f, p.Fill2On(active ? p.FillOn(surface) : surface));
                IconCache.DrawCentered(g, Icons.Cross, 10, hotClose ? p.Foreground : p.Foreground3,
                    close.X + close.Width / 2, close.Y + close.Height / 2);
            }

            var add = AddBounds();
            if (add.Right <= Width)
            {
                if (_hotAdd) Draw.FillRounded(g, add, 5f, p.HoverOn(surface));
                IconCache.DrawCentered(g, Icons.Plus, 14, p.Foreground2, add.X + add.Width / 2, add.Y + add.Height / 2);
            }
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = TabIndexAt(e.Location);
            bool hotClose = index >= 0 && CloseBounds(index).Contains(e.Location);
            bool hotAdd = AddBounds().Contains(e.Location);
            if (index != _hotIndex || hotClose != _hotClose || hotAdd != _hotAdd)
            {
                _hotIndex = index;
                _hotClose = hotClose;
                _hotAdd = hotAdd;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex != -1 || _hotAdd || _hotClose)
            {
                _hotIndex = -1;
                _hotClose = false;
                _hotAdd = false;
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
