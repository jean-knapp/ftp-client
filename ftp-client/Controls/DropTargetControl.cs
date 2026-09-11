using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    public sealed class FilesDroppedEventArgs : EventArgs
    {
        public FilesDroppedEventArgs(string[] paths) { Paths = paths ?? new string[0]; }
        public string[] Paths { get; }
    }

    /// <summary>
    /// The dashed drop zone under the file list: an upload glyph and "Drop files here to upload
    /// to /var/www/html". It takes the accent and the selection fill while files are dragged over
    /// it, and a click opens the file picker instead.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("FilesDropped")]
    public sealed class DropTargetControl : ModernControl
    {
        private string _targetPath = "/";
        private bool _dragOver;
        private bool _hot;

        public event EventHandler<FilesDroppedEventArgs> FilesDropped;

        public DropTargetControl()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            AllowDrop = true;
            Cursor = Cursors.Hand;
            Size = new Size(600, 72);
            Theme.Changed += OnThemeChanged;
        }

        /// <summary>The remote directory named in the prompt.</summary>
        [Category("Appearance"), DefaultValue("/")]
        public string TargetPath
        {
            get => _targetPath;
            set { _targetPath = value; Invalidate(); }
        }

        /// <summary>Lit from outside while files are dragged over the list above the zone.</summary>
        [Browsable(false)]
        public bool Highlighted
        {
            get => _dragOver;
            set { if (_dragOver == value) return; _dragOver = value; Invalidate(); }
        }

        /// <summary>The local paths in a drag, or null when it carries none.</summary>
        public static string[] GetFiles(IDataObject data)
        {
            if (data == null || !data.GetDataPresent(DataFormats.FileDrop)) return null;
            return data.GetData(DataFormats.FileDrop) as string[];
        }

        private void OnThemeChanged(object sender, EventArgs e) => Invalidate();

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

            var bounds = new Rectangle(0, 0, Width, Height);
            var face = _dragOver ? p.SelectionOn(surface) : _hot ? p.HoverOn(surface) : surface;
            if (face != surface) Draw.FillRounded(g, bounds, 7f, face);

            var border = _dragOver ? p.Accent : ThemePalette.Flatten(Color.FromArgb(p.Mode == ThemeMode.Light ? 46 : 38, p.Foreground), surface);
            var oldSmoothing = g.SmoothingMode;
            var oldOffset = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (var path = Draw.RoundedRect(new RectangleF(0.75f, 0.75f, Width - 1.5f, Height - 1.5f), 6.25f))
            using (var pen = new Pen(border, 1.5f) { DashPattern = new[] { 4f, 3f } })
            {
                g.DrawPath(pen, path);
            }
            g.SmoothingMode = oldSmoothing;
            g.PixelOffsetMode = oldOffset;

            const string lead = "Drop files here to upload to ";
            const string hint = "or press Upload to pick them from disk";
            var leadFont = Fonts.Ui(13.5f);
            var pathFont = Fonts.Code(13f);
            var hintFont = Fonts.Ui(12f);
            int leadWidth = Draw.MeasureWidth(lead, leadFont);
            int pathWidth = Draw.MeasureWidth(_targetPath, pathFont);
            int hintWidth = Draw.MeasureWidth(hint, hintFont);
            int textWidth = Math.Max(leadWidth + pathWidth, hintWidth);
            int groupWidth = Math.Min(Width - 32, 22 + 14 + textWidth);
            int x = Math.Max(16, (Width - groupWidth) / 2);
            int centreY = Height / 2;

            IconCache.DrawCentered(g, Icons.UploadTray, 22, _dragOver ? p.Accent : p.Foreground3, x + 11, centreY);
            x += 22 + 14;
            int available = Math.Max(0, Width - 16 - x);
            Draw.Text(g, lead, leadFont, new Rectangle(x, centreY - 20, Math.Min(leadWidth, available), 20), p.Foreground, Draw.LeftMiddle);
            if (available > leadWidth)
            {
                Draw.Text(g, _targetPath, pathFont, new Rectangle(x + leadWidth, centreY - 20, available - leadWidth, 20), p.Foreground, Draw.LeftMiddle);
            }
            Draw.Text(g, hint, hintFont, new Rectangle(x, centreY + 1, available, 18), p.Foreground3, Draw.LeftMiddle);
        }

        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hot = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hot = false; Invalidate(); }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            bool files = GetFiles(e.Data) != null;
            e.Effect = files ? DragDropEffects.Copy : DragDropEffects.None;
            Highlighted = files;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);
            e.Effect = GetFiles(e.Data) != null ? DragDropEffects.Copy : DragDropEffects.None;
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            Highlighted = false;
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            Highlighted = false;
            var files = GetFiles(e.Data);
            if (files != null && files.Length > 0) FilesDropped?.Invoke(this, new FilesDroppedEventArgs(files));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) Theme.Changed -= OnThemeChanged;
            base.Dispose(disposing);
        }
    }
}
