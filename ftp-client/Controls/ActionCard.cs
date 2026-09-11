using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// A welcome-screen action card: a 40 px tinted icon tile, a semibold title and a muted
    /// description on a card surface that lightens on hover.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Title")]
    [DefaultEvent("Click")]
    public sealed class ActionCard : ModernControl
    {
        private string _title = "Action";
        private string _description = string.Empty;
        private string _iconSvg;
        private Color _tileFill = Color.Transparent;
        private Color _tileForeground = Color.White;
        private bool _hot;
        private bool _pressed;

        public ActionCard()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Size = new Size(280, 96);
            Cursor = Cursors.Hand;
            Theme.Changed += (s, e) => Invalidate();
        }

        [Category("Appearance"), DefaultValue("Action")]
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

        [Category("Appearance"), DefaultValue(null)]
        [Editor("System.ComponentModel.Design.MultilineStringEditor, System.Design", typeof(System.Drawing.Design.UITypeEditor))]
        public string IconSvg
        {
            get => _iconSvg;
            set { _iconSvg = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color TileFill
        {
            get => _tileFill;
            set { _tileFill = value; Invalidate(); }
        }

        [Category("Appearance")]
        public Color TileForeground
        {
            get => _tileForeground;
            set { _tileForeground = value; Invalidate(); }
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
            var parent = ParentSurface();

            var face = p.Layer;
            if (_pressed) face = p.Fill2On(p.Layer);
            else if (_hot) face = p.Mode == ThemeMode.Light ? p.HoverOn(p.Layer) : Color.FromArgb(0x2d, 0x2d, 0x2d);

            Draw.Card(g, new Rectangle(0, 0, Width - 1, Height - 1), 8f, face, p.CardStrokeOn(face));

            int x = 20;
            var tile = new Rectangle(x, (Height - 40) / 2, 40, 40);
            Draw.FillRounded(g, tile, 8f, ThemePalette.Flatten(_tileFill, face));
            if (!string.IsNullOrEmpty(_iconSvg))
            {
                IconCache.DrawCentered(g, _iconSvg, 20, _tileForeground, tile.X + tile.Width / 2, tile.Y + tile.Height / 2);
            }
            x += 40 + 14;

            int available = Math.Max(0, Width - x - 16);
            Draw.Text(g, _title, Fonts.Ui(15f, true), new Rectangle(x, Height / 2 - 20, available, 20), p.Foreground, Draw.LeftMiddle);
            Draw.Text(g, _description, Fonts.Ui(13f), new Rectangle(x, Height / 2 + 1, available, 18), p.Foreground3, Draw.LeftMiddle);

            if (Focused) Draw.DrawRounded(g, new Rectangle(1, 1, Width - 3, Height - 3), 8f, p.Accent, 2f);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; _pressed = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); if (e.Button == MouseButtons.Left) { _pressed = true; Focus(); Invalidate(); } }
        protected override void OnMouseUp(MouseEventArgs e) { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

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
                OnClick(EventArgs.Empty);
            }
        }
    }
}
