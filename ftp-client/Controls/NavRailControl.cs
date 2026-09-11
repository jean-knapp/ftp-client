using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using FtpClient.Services;

namespace FtpClient.Controls
{
    /// <summary>
    /// The settings dialog's navigation rail: 36 px items, the selected one filled with --fill and
    /// carrying a 3 × 18 px accent pip inset 4 px from its left edge.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SelectionChanged")]
    public sealed class NavRailControl : VirtualListControl
    {
        private const int ItemHeight = 36;
        private const int ItemGap = 2;
        private const int RailPadding = 8;

        private readonly List<string> _items = new List<string>();

        public NavRailControl()
        {
            EmptyText = null;
        }

        [Category("Data")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [Editor("System.Windows.Forms.Design.StringCollectionEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public List<string> Items => _items;

        protected override int RowCount => _items.Count;
        protected override int GetRowHeight(int index) => ItemHeight + ItemGap;
        protected override int HeaderHeight => RailPadding;

        protected override void PaintHeader(Graphics g, Rectangle bounds)
        {
            Draw.Fill(g, bounds, RowSurface);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var item = new Rectangle(RailPadding, bounds.Y, Math.Max(0, bounds.Width - RailPadding * 2), ItemHeight);
            bool selected = (state & RowState.Selected) != 0;

            if (selected) Draw.FillRounded(g, item, 5f, p.FillOn(surface));
            else if ((state & RowState.Hot) != 0) Draw.FillRounded(g, item, 5f, p.HoverOn(surface));

            if (selected)
            {
                var pip = new Rectangle(item.X + 4, item.Y + 9, 3, ItemHeight - 18);
                Draw.FillRounded(g, pip, 2f, p.AccentFill);
            }

            Draw.Text(g, _items[index], Fonts.Ui(13.5f, selected),
                new Rectangle(item.X + 12, item.Y, item.Width - 20, item.Height),
                selected ? p.Foreground : p.Foreground2, Draw.LeftMiddle);
        }
    }
}
