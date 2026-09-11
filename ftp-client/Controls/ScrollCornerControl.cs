using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// The square where a vertical and a horizontal scroll bar meet, and the piece that closes the
    /// gap under a lone vertical bar. It paints the same backdrop as the bars' gutters, so the edge
    /// of a panel reads as one continuous strip, and it draws the rounded corner of the card it
    /// sits in - which the bars themselves would square off.
    /// </summary>
    [ToolboxItem(false)]
    public sealed class ScrollCornerControl : ModernControl
    {
        public ScrollCornerControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(12, 12);
            Theme.Changed += OnThemeChanged;
        }

        /// <summary>Surface to paint; defaults to the nearest card's colour.</summary>
        [Browsable(false)]
        public Color Surface { get; set; } = Color.Empty;

        private Color EffectiveSurface
        {
            get
            {
                if (Surface != Color.Empty) return Surface;
                for (var c = Parent; c != null; c = c.Parent)
                {
                    var surface = c as SurfacePanel;
                    if (surface != null) return surface.SurfaceColor;
                }
                return Theme.Palette.Layer;
            }
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

        protected override void OnPaint(PaintEventArgs e)
        {
            Draw.Fill(e.Graphics, ClientRectangle, EffectiveSurface);
            SurfacePanel.PaintRoundedParentCorners(e.Graphics, this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
