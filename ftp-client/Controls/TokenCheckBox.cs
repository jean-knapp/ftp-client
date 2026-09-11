using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The redesign's checkbox: an 18 px rounded box with a 1.5 px --fg3 outline, filled with the
    /// accent and a check glyph when set.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Checked")]
    [DefaultEvent("CheckedChanged")]
    public sealed class TokenCheckBox : ModernControl
    {
        private const int BoxSize = 18;

        private bool _checked;
        private float _textSizePx = 13f;
        private bool _hot;

        public event EventHandler CheckedChanged;

        public TokenCheckBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Size = new Size(200, 22);
            Cursor = Cursors.Hand;
            Theme.Changed += (s, e) => Invalidate();
        }

        [Category("Behavior"), DefaultValue(false)]
        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value) return;
                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        [Category("Appearance"), DefaultValue(13f)]
        public float TextSizePx
        {
            get => _textSizePx;
            set { _textSizePx = Math.Max(6f, value); Invalidate(); }
        }

        [Category("Appearance")]
        public override string Text
        {
            get => base.Text;
            set { base.Text = value; Invalidate(); }
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
            var box = new Rectangle(0, (Height - BoxSize) / 2, BoxSize, BoxSize);

            if (_checked)
            {
                Draw.FillRounded(g, box, 4f, p.AccentFill);
                IconCache.DrawCentered(g, Icons.Check, 13, p.AccentForeground, box.X + box.Width / 2, box.Y + box.Height / 2);
            }
            else
            {
                if (_hot) Draw.FillRounded(g, box, 4f, p.HoverOn(ParentSurface()));
                Draw.DrawRounded(g, new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1), 4f, p.Foreground3, 1.5f);
            }

            var textRect = new Rectangle(BoxSize + 10, 0, Math.Max(0, Width - BoxSize - 10), Height);
            Draw.Text(g, Text, Fonts.Ui(_textSizePx), textRect, p.Foreground2, Draw.LeftMiddle);

            if (Focused) Draw.DrawRounded(g, new Rectangle(0, 0, Width - 1, Height - 1), 4f, p.Accent, 2f);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            Focus();
            Checked = !Checked;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if ((keyData & Keys.KeyCode) == Keys.Space) return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space)
            {
                e.Handled = true;
                Checked = !Checked;
            }
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    }
}
