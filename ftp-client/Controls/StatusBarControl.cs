using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The 28 px status bar: a state dot with "Connected — SFTP", the endpoint, the padlock and
    /// security line, the pairing in --lane2, and idle or deploy state on the right.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("PairingClick")]
    public sealed class StatusBarControl : ModernControl
    {
        private const int SidePadding = 14;
        private const int SegmentGap = 18;

        private SessionState _state = SessionState.None;
        private string _statusText = "Not connected";
        private string _endpoint;
        private string _security;
        private string _pairingText;
        private string _rightText;
        private Rectangle _pairingBounds;
        private bool _hotPairing;

        /// <summary>Raised when the pairing segment is clicked.</summary>
        public event EventHandler PairingClick;

        public StatusBarControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(800, 28);
            Theme.Changed += OnThemeChanged;
        }

        [Category("Appearance"), DefaultValue(SessionState.None)]
        public SessionState State
        {
            get => _state;
            set { _state = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue("Not connected")]
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        public string Endpoint
        {
            get => _endpoint;
            set { _endpoint = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        public string Security
        {
            get => _security;
            set { _security = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        public string PairingText
        {
            get => _pairingText;
            set { _pairingText = value; Invalidate(); }
        }

        [Category("Appearance"), DefaultValue(null)]
        public string RightText
        {
            get => _rightText;
            set { if (_rightText == value) return; _rightText = value; Invalidate(); }
        }

        /// <summary>Draws the hairline across the top edge.</summary>
        [Category("Appearance"), DefaultValue(true)]
        public bool TopDivider { get; set; } = true;

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

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
            if (TopDivider) Draw.HLine(g, 0, 0, Width, p.DividerOn(surface));

            var font = Fonts.Ui(12f);
            int rightEdge = Width - SidePadding;
            if (!string.IsNullOrEmpty(_rightText))
            {
                int w = Draw.MeasureWidth(_rightText, font);
                Draw.Text(g, _rightText, font, new Rectangle(rightEdge - w, 0, w + 2, Height), p.Foreground3, Draw.LeftMiddle);
                rightEdge -= w + SegmentGap;
            }

            int x = SidePadding;
            float centreY = Height / 2f;
            if (_state != SessionState.None)
            {
                Draw.Dot(g, x, centreY, 7, SessionStates.StateColor(_state));
                x += 7 + 7;
            }
            x = Segment(g, x, rightEdge, null, _statusText, font, p.Foreground2, p.Foreground2);
            x = Segment(g, x, rightEdge, null, _endpoint, font, p.Foreground3, p.Foreground3);
            x = Segment(g, x, rightEdge, Icons.Padlock, _security, font, p.Foreground3, p.Foreground3);

            int pairingStart = x;
            x = Segment(g, x, rightEdge, Icons.Link, _pairingText, font, p.Lane2, p.Lane2);
            _pairingBounds = string.IsNullOrEmpty(_pairingText) ? Rectangle.Empty : new Rectangle(pairingStart, 0, Math.Max(0, x - SegmentGap - pairingStart), Height);
            if (_hotPairing && !_pairingBounds.IsEmpty)
            {
                int textLeft = pairingStart + 12 + 5;
                Draw.HLine(g, textLeft, (int)centreY + 8, Math.Max(0, _pairingBounds.Right - textLeft), p.Lane2);
            }
        }

        /// <summary>Paints an optional glyph and a text, returning where the next segment starts.</summary>
        private int Segment(Graphics g, int x, int limit, string icon, string text, Font font, Color iconColor, Color textColor)
        {
            if (string.IsNullOrEmpty(text) || x >= limit) return x;
            if (icon != null)
            {
                IconCache.DrawLeft(g, icon, 12, iconColor, new Rectangle(x, 0, 12, Height));
                x += 12 + 5;
            }
            int w = Math.Min(Draw.MeasureWidth(text, font), Math.Max(0, limit - x));
            Draw.Text(g, text, font, new Rectangle(x, 0, w + 1, Height), textColor, Draw.LeftMiddle);
            return x + w + SegmentGap;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool hot = _pairingBounds.Contains(e.Location);
            if (hot == _hotPairing) return;
            _hotPairing = hot;
            Cursor = hot ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!_hotPairing) return;
            _hotPairing = false;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _pairingBounds.Contains(e.Location)) PairingClick?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
