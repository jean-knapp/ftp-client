using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>State flags passed to <see cref="VirtualListControl.PaintRow"/>.</summary>
    [Flags]
    public enum RowState
    {
        None = 0,
        Hot = 1,
        Selected = 2,
        Focused = 4,
    }

    /// <summary>
    /// Base for the owner-drawn lists in the redesign: variable row heights, wheel scrolling,
    /// hover and selection. Scrolling is driven by a <see cref="ModernScrollBar"/> from the
    /// control library, so every list in the application scrolls and looks the same.
    /// </summary>
    [ToolboxItem(false)]
    public abstract class VirtualListControl : ModernControl
    {
        /// <summary>Width the library's bar occupies down the right edge.</summary>
        protected const int ScrollBarWidth = 12;

        private readonly ModernScrollBar _scrollBar;
        private readonly ScrollCornerControl _scrollCorner;
        private int _scroll;
        private int _hotRow = -1;
        private readonly List<int> _selected = new List<int>();
        private int _anchor = -1;
        private bool _syncingScrollBar;

        /// <summary>Raised when the selection changes through user input or code.</summary>
        public event EventHandler SelectionChanged;

        public event EventHandler<RowMouseEventArgs> RowClick;
        public event EventHandler<RowMouseEventArgs> RowDoubleClick;
        public event EventHandler<RowMouseEventArgs> RowRightClick;

        protected VirtualListControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;

            _scrollBar = new ModernScrollBar
            {
                Orientation = Orientation.Vertical,
                Visible = false,
                TabStop = false,
                Width = ScrollBarWidth,
            };
            _scrollBar.Scroll += (s, e) =>
            {
                if (_syncingScrollBar) return;
                ScrollOffset = e.NewValue;
            };
            _scrollCorner = new ScrollCornerControl { Visible = false };
            Controls.Add(_scrollCorner);
            Controls.Add(_scrollBar);

            Theme.Changed += OnThemeChanged;
            ApplyScrollBarColors();
        }

        /// <summary>The library scroll bar, so subclasses can measure around it.</summary>
        protected ModernScrollBar ScrollBar => _scrollBar;

        /// <summary>Width taken by the scroll bar right now, 0 when it is hidden.</summary>
        protected int ScrollBarSpace => _scrollBar != null && _scrollBar.Visible ? ScrollBarWidth : 0;

        private void ApplyScrollBarColors()
        {
            if (_scrollBar == null) return;
            _scrollBar.UseParentSkin = false;
            _scrollBar.ScrollBarColors.TrackColor = Color.Transparent;
            _scrollBar.ScrollBarColors.ThumbColor = P.Fill2On(RowSurface);
            _scrollBar.ScrollBarColors.ThumbHoverColor = P.Foreground3;
            _scrollBar.BackColor = RowSurface;
            if (_scrollCorner != null) _scrollCorner.Surface = RowSurface;
        }

        /// <summary>Keeps the bar's range, position and visibility in step with the content.</summary>
        private void SyncScrollBar()
        {
            if (_scrollBar == null) return;
            int viewport = ViewportHeight;
            int total = TotalHeight;
            bool needed = viewport > 0 && total > viewport;

            _syncingScrollBar = true;
            try
            {
                if (_scrollBar.Visible != needed) _scrollBar.Visible = needed;
                if (_scrollCorner != null && _scrollCorner.Visible != needed) _scrollCorner.Visible = needed;
                if (!needed) return;

                // The bar runs to the bottom except for the corner square, which closes the strip
                // and keeps a rounded card's arc from being squared off by the bar's own window.
                bool corner = SurfacePanel.RoundedParentBottomInset(this) > 0;
                int reserved = corner ? ScrollBarWidth : 0;
                _scrollBar.SetBounds(Width - ScrollBarWidth, ViewportTop, ScrollBarWidth, Math.Max(0, viewport - reserved));
                if (_scrollCorner != null)
                {
                    _scrollCorner.Visible = corner;
                    _scrollCorner.SetBounds(Width - ScrollBarWidth, Height - ScrollBarWidth, ScrollBarWidth, ScrollBarWidth);
                }
                _scrollBar.Minimum = 0;
                _scrollBar.Maximum = Math.Max(0, total);
                _scrollBar.LargeChange = viewport;
                _scrollBar.SmallChange = DefaultScrollStep;
                int value = Math.Max(0, Math.Min(_scroll, _scrollBar.MaximumScrollValue));
                if (_scrollBar.Value != value) _scrollBar.Value = value;
            }
            finally
            {
                _syncingScrollBar = false;
            }
        }

        protected ThemePalette P => Theme.Palette;

        /// <summary>Surface the rows are painted on, used to flatten translucent tokens.</summary>
        protected virtual Color RowSurface
        {
            get
            {
                for (var c = Parent; c != null; c = c.Parent)
                {
                    var surface = c as SurfacePanel;
                    if (surface != null) return surface.SurfaceColor;
                }
                return P.Layer;
            }
        }

        // ------------------------------------------------------------------ model

        protected abstract int RowCount { get; }
        protected abstract int GetRowHeight(int index);
        protected abstract void PaintRow(Graphics g, int index, Rectangle bounds, RowState state);

        /// <summary>Height of the non-scrolling header drawn at the top; 0 for none.</summary>
        protected virtual int HeaderHeight => 0;
        protected virtual void PaintHeader(Graphics g, Rectangle bounds) { }

        /// <summary>Rows that cannot be selected, such as group bands.</summary>
        protected virtual bool IsSelectable(int index) => true;

        /// <summary>A right-click selects the row under it before the menu opens.</summary>
        protected virtual bool SelectsOnRightClick => true;

        protected virtual bool MultiSelect => false;

        /// <summary>Message shown centred when the list has no rows.</summary>
        [Category("Appearance"), DefaultValue(null)]
        public string EmptyText { get; set; }

        // ------------------------------------------------------------------ geometry

        [Browsable(false)]
        public int ScrollOffset
        {
            get => _scroll;
            set
            {
                int clamped = Math.Max(0, Math.Min(value, MaxScroll));
                if (clamped == _scroll) return;
                _scroll = clamped;
                SyncScrollBar();
                Invalidate();
            }
        }

        protected int ViewportTop => HeaderHeight;
        protected int ViewportHeight => Math.Max(0, Height - HeaderHeight);

        /// <summary>Height of every row, for callers that size a card to its list.</summary>
        [Browsable(false)]
        public int RowsHeight => TotalHeight;

        protected int TotalHeight
        {
            get
            {
                int total = 0;
                int count = RowCount;
                for (int i = 0; i < count; i++) total += GetRowHeight(i);
                return total;
            }
        }

        private int MaxScroll => Math.Max(0, TotalHeight - ViewportHeight);

        /// <summary>Y of a row's top edge in client coordinates.</summary>
        protected int RowTop(int index)
        {
            int y = ViewportTop - _scroll;
            for (int i = 0; i < index; i++) y += GetRowHeight(i);
            return y;
        }

        public int RowIndexAt(Point point)
        {
            if (point.Y < ViewportTop) return -1;
            int y = ViewportTop - _scroll;
            int count = RowCount;
            for (int i = 0; i < count; i++)
            {
                int h = GetRowHeight(i);
                if (point.Y >= y && point.Y < y + h) return i;
                y += h;
            }
            return -1;
        }

        public void EnsureVisible(int index)
        {
            if (index < 0 || index >= RowCount) return;
            int top = 0;
            for (int i = 0; i < index; i++) top += GetRowHeight(i);
            int height = GetRowHeight(index);
            if (top < _scroll) ScrollOffset = top;
            else if (top + height > _scroll + ViewportHeight) ScrollOffset = top + height - ViewportHeight;
        }

        // ------------------------------------------------------------------ selection

        [Browsable(false)]
        public int SelectedIndex
        {
            get => _selected.Count > 0 ? _selected[_selected.Count - 1] : -1;
            set => SetSelection(value, true);
        }

        [Browsable(false)]
        public IReadOnlyList<int> SelectedIndices => _selected;

        public void SetSelection(int index, bool raiseEvent)
        {
            _selected.Clear();
            if (index >= 0 && index < RowCount && IsSelectable(index)) _selected.Add(index);
            _anchor = index;
            Invalidate();
            if (raiseEvent) SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ClearSelection() => SetSelection(-1, true);

        /// <summary>Replaces the selection with several rows, for select-all and restoring a multi-selection.</summary>
        public void SelectIndices(IEnumerable<int> indices, bool raiseEvent)
        {
            _selected.Clear();
            if (indices != null)
            {
                foreach (var index in indices)
                {
                    if (index >= 0 && index < RowCount && IsSelectable(index) && !_selected.Contains(index)) _selected.Add(index);
                }
            }
            _anchor = _selected.Count > 0 ? _selected[_selected.Count - 1] : -1;
            Invalidate();
            if (raiseEvent) SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        protected bool IsSelected(int index) => _selected.Contains(index);

        // ------------------------------------------------------------------ painting

        protected virtual void OnThemeChanged(object sender, EventArgs e)
        {
            ApplyScrollBarColors();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int count = RowCount;
            if (count == 0 && !string.IsNullOrEmpty(EmptyText))
            {
                Draw.Text(g, EmptyText, Fonts.Ui(13f), new Rectangle(24, ViewportTop, Math.Max(0, Width - 48), ViewportHeight),
                    P.Foreground3, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
            }
            else
            {
                // Rows stop short of the scroll bar so right-aligned content is never under it.
                int rowWidth = Math.Max(0, Width - ScrollBarSpace);
                var clip = new Rectangle(0, ViewportTop, rowWidth, ViewportHeight);
                var saved = g.Clip;
                g.SetClip(clip);

                int y = ViewportTop - _scroll;
                for (int i = 0; i < count; i++)
                {
                    int h = GetRowHeight(i);
                    if (y + h > ViewportTop && y < Height)
                    {
                        var state = RowState.None;
                        if (i == _hotRow) state |= RowState.Hot;
                        if (IsSelected(i)) state |= RowState.Selected;
                        if (Focused) state |= RowState.Focused;
                        PaintRow(g, i, new Rectangle(0, y, rowWidth, h), state);
                    }
                    y += h;
                    if (y >= Height) break;
                }

                g.Clip = saved;
            }

            // The header paints over the rows, not under them: row text goes through GDI
            // (TextRenderer), which ignores the clip region above, so a row scrolled halfway under
            // the header would otherwise write its text across the column titles.
            if (HeaderHeight > 0) PaintHeader(g, new Rectangle(0, 0, Width, HeaderHeight));

            SurfacePanel.PaintRoundedParentCorners(g, this);
        }

        // ------------------------------------------------------------------ input

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int notches = e.Delta / SystemInformation.MouseWheelScrollDelta;
            ScrollOffset = _scroll - notches * 3 * DefaultScrollStep;
            UpdateHot(e.Location);
        }

        /// <summary>Pixels one wheel step scrolls, per notch line.</summary>
        protected virtual int DefaultScrollStep => 20;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateHot(e.Location);
        }

        private void UpdateHot(Point point)
        {
            int index = RowIndexAt(point);
            if (index == _hotRow) return;
            _hotRow = index;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotRow != -1)
            {
                _hotRow = -1;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            int index = RowIndexAt(e.Location);
            if (index < 0) return;

            if (e.Button == MouseButtons.Left)
            {
                if (!IsSelectable(index)) { OnRowClicked(index, e); return; }
                if (MultiSelect && (ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (_selected.Contains(index)) _selected.Remove(index); else _selected.Add(index);
                    _anchor = index;
                    Invalidate();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
                else if (MultiSelect && (ModifierKeys & Keys.Shift) == Keys.Shift && _anchor >= 0)
                {
                    _selected.Clear();
                    int a = Math.Min(_anchor, index), b = Math.Max(_anchor, index);
                    for (int i = a; i <= b; i++) if (IsSelectable(i)) _selected.Add(i);
                    Invalidate();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SetSelection(index, true);
                }
                OnRowClicked(index, e);
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (SelectsOnRightClick && IsSelectable(index) && !_selected.Contains(index)) SetSelection(index, true);
            }
        }

        protected virtual void OnRowClicked(int index, MouseEventArgs e)
        {
            RowClick?.Invoke(this, new RowMouseEventArgs(index, e.Location, e.Button));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right)
            {
                int index = RowIndexAt(e.Location);
                RowRightClick?.Invoke(this, new RowMouseEventArgs(index, e.Location, e.Button));
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int index = RowIndexAt(e.Location);
            if (index >= 0) RowDoubleClick?.Invoke(this, new RowMouseEventArgs(index, e.Location, e.Button));
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up: case Keys.Down: case Keys.PageUp: case Keys.PageDown:
                case Keys.Home: case Keys.End: case Keys.Space:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int count = RowCount;
            if (count == 0) return;
            int current = SelectedIndex;
            int target = current;
            switch (e.KeyCode)
            {
                case Keys.Up: target = PreviousSelectable(current); break;
                case Keys.Down: target = NextSelectable(current); break;
                case Keys.PageUp: target = Math.Max(0, current - Math.Max(1, ViewportHeight / Math.Max(1, GetRowHeight(0)))); break;
                case Keys.PageDown: target = Math.Min(count - 1, current + Math.Max(1, ViewportHeight / Math.Max(1, GetRowHeight(0)))); break;
                case Keys.Home: target = NextSelectable(-1); break;
                case Keys.End: target = PreviousSelectable(count); break;
                default: return;
            }
            e.Handled = true;
            if (target < 0 || target >= count) return;
            SetSelection(target, true);
            EnsureVisible(target);
        }

        private int NextSelectable(int from)
        {
            for (int i = from + 1; i < RowCount; i++) if (IsSelectable(i)) return i;
            return from;
        }

        private int PreviousSelectable(int from)
        {
            for (int i = from - 1; i >= 0; i--) if (IsSelectable(i)) return i;
            return from;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ScrollOffset = _scroll;
            SyncScrollBar();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyScrollBarColors();
            SyncScrollBar();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            ApplyScrollBarColors();
        }

        /// <summary>Rows changed, so the bar's range has to be recomputed.</summary>
        protected void ContentChanged()
        {
            SyncScrollBar();
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

        /// <summary>Keeps the selection and scroll position across a data refresh.</summary>
        protected void RestoreState(int selectedIndex, int scroll)
        {
            _selected.Clear();
            if (selectedIndex >= 0 && selectedIndex < RowCount) _selected.Add(selectedIndex);
            _anchor = selectedIndex;
            _scroll = Math.Max(0, Math.Min(scroll, MaxScroll));
            SyncScrollBar();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }

    public sealed class RowMouseEventArgs : EventArgs
    {
        public RowMouseEventArgs(int index, Point location, MouseButtons button)
        {
            Index = index;
            Location = location;
            Button = button;
        }

        public int Index { get; }
        public Point Location { get; }
        public MouseButtons Button { get; }
    }
}
