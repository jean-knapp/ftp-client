using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The redesign's toggle: a 40x20 pill outlined in --fg3 with a 12 px --fg2 knob when off, and
    /// filled with --accfill carrying an --accfg knob when on. ModernToggleSwitch clears itself with
    /// the skin's panel colour, which stamps a --bg rectangle onto a card, so this draws the switch
    /// against whatever surface actually sits behind it.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultProperty("Checked")]
    [DefaultEvent("CheckedChanged")]
    public sealed class ToggleSwitchControl : ModernControl
    {
        private const int TrackWidth = 40;
        private const int TrackHeight = 20;
        private const float KnobSize = 12f;

        private bool _checked;
        private bool _hot;

        public event EventHandler CheckedChanged;

        public ToggleSwitchControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = true;
            BackColor = Color.Transparent;
            Size = new Size(TrackWidth, TrackHeight);
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

        /// <summary>Sets the state without raising <see cref="CheckedChanged"/>.</summary>
        public void SetCheckedQuiet(bool value)
        {
            _checked = value;
            Invalidate();
        }

        public override Size GetPreferredSize(Size proposedSize) => new Size(TrackWidth, TrackHeight);

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
            var surface = ParentSurface();
            Draw.Fill(g, ClientRectangle, surface);

            var track = new Rectangle(0, (Height - TrackHeight) / 2, Math.Min(TrackWidth, Width), TrackHeight);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            using (var path = Pill(new RectangleF(track.X + 0.5f, track.Y + 0.5f, track.Width - 1f, track.Height - 1f)))
            {
                if (_checked)
                {
                    using (var fill = new SolidBrush(p.AccentFill)) g.FillPath(fill, path);
                }
                else if (_hot)
                {
                    using (var fill = new SolidBrush(p.HoverOn(surface))) g.FillPath(fill, path);
                }

                using (var pen = new Pen(_checked ? p.AccentFill : (_hot ? p.Foreground2 : p.Foreground3)))
                {
                    g.DrawPath(pen, path);
                }
            }

            float centreY = track.Y + TrackHeight / 2f;
            float centreX = _checked ? track.Right - TrackHeight / 2f : track.X + TrackHeight / 2f;
            using (var knob = new SolidBrush(_checked ? p.AccentForeground : p.Foreground2))
            {
                g.FillEllipse(knob, centreX - KnobSize / 2f, centreY - KnobSize / 2f, KnobSize, KnobSize);
            }

            g.SmoothingMode = SmoothingMode.None;
            g.PixelOffsetMode = PixelOffsetMode.Default;

            if (Focused)
            {
                var focus = new Rectangle(track.X - 2, track.Y - 2, track.Width + 3, track.Height + 3);
                Draw.DrawRounded(g, focus, (focus.Height / 2f), p.Accent, 1.5f);
            }
        }

        private static GraphicsPath Pill(RectangleF r)
        {
            var path = new GraphicsPath();
            float d = r.Height;
            path.AddArc(r.Left, r.Top, d, d, 90, 180);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 180);
            path.CloseFigure();
            return path;
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
