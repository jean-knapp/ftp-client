using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using FtpClient.Remote;
using FtpClient.Services;
using ModernWinForms;

namespace FtpClient.Controls
{
    public sealed class PathEventArgs : EventArgs
    {
        public PathEventArgs(string path) { Path = path; }
        public string Path { get; }
    }

    /// <summary>
    /// The path field: the server glyph, the connection URI, then one segment per directory with
    /// the current one set semibold on --fill2, and the item count on the right. Segments navigate;
    /// double-clicking the empty part of the field turns it into a text box for typing a path.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("PathRequested")]
    public sealed class BreadcrumbBar : ModernControl
    {
        private const int IconLeft = 10;
        private const int IconSize = 14;
        private const int CrumbPadding = 6;
        private const int CrumbHeight = 22;
        private const string Separator = "/";

        private sealed class Crumb
        {
            public string Text;
            public string Path;
            public bool Current;
            public bool Muted;
            public Rectangle Bounds;
        }

        private readonly List<Crumb> _crumbs = new List<Crumb>();
        private readonly List<Rectangle> _separators = new List<Rectangle>();
        private string _uri = string.Empty;
        private string _path = RemotePath.Root;
        private string _info;
        private int _hot = -1;
        private bool _layoutValid;
        private ModernTextBox _editor;

        public event EventHandler<PathEventArgs> PathRequested;

        public BreadcrumbBar()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            TabStop = false;
            BackColor = Color.Transparent;
            Size = new Size(600, 32);
            Theme.Changed += OnThemeChanged;
        }

        /// <summary>The connection URI shown before the first segment, e.g. <c>sftp://deploy@web-prod</c>.</summary>
        [Category("Appearance"), DefaultValue("")]
        public string Uri
        {
            get => _uri;
            set { _uri = value ?? string.Empty; InvalidateLayout(); }
        }

        [Category("Appearance"), DefaultValue("/")]
        public string Path
        {
            get => _path;
            set { _path = RemotePath.Normalize(value); InvalidateLayout(); }
        }

        /// <summary>Right-aligned summary, e.g. <c>11 items · 2.4 GB</c>.</summary>
        [Category("Appearance"), DefaultValue(null)]
        public string InfoText
        {
            get => _info;
            set { if (_info == value) return; _info = value; InvalidateLayout(); }
        }

        [Browsable(false)]
        public bool IsEditing => _editor != null;

        private void OnThemeChanged(object sender, EventArgs e) => InvalidateLayout();

        private void InvalidateLayout()
        {
            _layoutValid = false;
            _hot = -1;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            InvalidateLayout();
        }

        // ------------------------------------------------------------------ layout

        private static Font CrumbFont(bool current) => Fonts.Ui(13f, current);

        private int InfoWidth => string.IsNullOrEmpty(_info) ? 0 : Draw.MeasureWidth(_info, Fonts.Ui(12.5f)) + 16;

        private static int CrumbWidth(Crumb crumb) => Draw.MeasureWidth(crumb.Text, CrumbFont(crumb.Current)) + CrumbPadding * 2;

        private void EnsureLayout()
        {
            if (_layoutValid) return;
            _layoutValid = true;
            _crumbs.Clear();
            _separators.Clear();

            var full = new List<Crumb>();
            var segments = RemotePath.Segments(_path);
            if (!string.IsNullOrEmpty(_uri)) full.Add(new Crumb { Text = _uri, Path = RemotePath.Root, Muted = true });
            if (segments.Length == 0)
            {
                full.Add(new Crumb { Text = Separator, Path = RemotePath.Root, Current = true });
            }
            else
            {
                var path = string.Empty;
                for (int i = 0; i < segments.Length; i++)
                {
                    path += "/" + segments[i];
                    full.Add(new Crumb { Text = segments[i], Path = path, Current = i == segments.Length - 1 });
                }
            }

            int left = IconLeft + IconSize + 8;
            int right = Width - 10 - InfoWidth;
            int separatorWidth = Draw.MeasureWidth(Separator, CrumbFont(false)) + 8;

            // Leading crumbs give way to an ellipsis, which still leads to the directory it hides.
            var chosen = full;
            for (int start = 0; start < full.Count; start++)
            {
                var candidate = new List<Crumb>();
                if (start > 0) candidate.Add(new Crumb { Text = "…", Path = full[start - 1].Path, Muted = true });
                for (int i = start; i < full.Count; i++) candidate.Add(full[i]);
                chosen = candidate;

                int width = 0;
                for (int i = 0; i < candidate.Count; i++) width += CrumbWidth(candidate[i]) + (i > 0 ? separatorWidth : 0);
                if (width <= right - left) break;
            }

            int x = left;
            int y = (Height - CrumbHeight) / 2;
            for (int i = 0; i < chosen.Count; i++)
            {
                if (i > 0)
                {
                    _separators.Add(new Rectangle(x, 0, separatorWidth, Height));
                    x += separatorWidth;
                }
                var crumb = chosen[i];
                int w = Math.Min(CrumbWidth(crumb), Math.Max(0, right - x));
                crumb.Bounds = new Rectangle(x, y, w, CrumbHeight);
                _crumbs.Add(crumb);
                x += w;
            }
        }

        // ------------------------------------------------------------------ painting

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
            var face = p.FillOn(surface);
            Draw.Fill(g, ClientRectangle, surface);

            var field = new Rectangle(0, 0, Width, Height);
            Draw.FillRounded(g, field, 4f, face);
            Draw.DrawRounded(g, field, 4f, p.StrokeOn(surface));
            if (_editor != null) return;

            IconCache.DrawLeft(g, Icons.Server, IconSize, p.Lane, new Rectangle(IconLeft, 0, IconSize, Height));

            EnsureLayout();
            foreach (var separator in _separators)
            {
                Draw.Text(g, Separator, CrumbFont(false), separator, p.Foreground3, Draw.CenterMiddle);
            }
            for (int i = 0; i < _crumbs.Count; i++)
            {
                var crumb = _crumbs[i];
                bool hot = i == _hot && !crumb.Current;
                if (crumb.Current || hot) Draw.FillRounded(g, crumb.Bounds, 3f, p.Fill2On(face));
                var color = crumb.Current || hot ? p.Foreground : crumb.Muted ? p.Foreground3 : p.Foreground2;
                var textRect = new Rectangle(crumb.Bounds.X + CrumbPadding, 0, Math.Max(0, crumb.Bounds.Width - CrumbPadding * 2) + 2, Height);
                Draw.Text(g, crumb.Text, CrumbFont(crumb.Current), textRect, color, Draw.LeftMiddle);
            }

            if (!string.IsNullOrEmpty(_info))
            {
                var font = Fonts.Ui(12.5f);
                int w = Draw.MeasureWidth(_info, font);
                Draw.Text(g, _info, font, new Rectangle(Width - 10 - w, 0, w + 2, Height), p.Foreground3, Draw.LeftMiddle);
            }
        }

        // ------------------------------------------------------------------ input

        private int CrumbAt(Point point)
        {
            EnsureLayout();
            for (int i = 0; i < _crumbs.Count; i++)
            {
                if (_crumbs[i].Bounds.Contains(point)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hot = CrumbAt(e.Location);
            if (hot == _hot) return;
            _hot = hot;
            Cursor = hot >= 0 && !_crumbs[hot].Current ? Cursors.Hand : Cursors.IBeam;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hot == -1) return;
            _hot = -1;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;
            int index = CrumbAt(e.Location);
            if (index >= 0 && !_crumbs[index].Current) PathRequested?.Invoke(this, new PathEventArgs(_crumbs[index].Path));
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            int index = CrumbAt(e.Location);
            if (index < 0 || _crumbs[index].Current) BeginEdit();
        }

        /// <summary>Replaces the crumbs with a text box holding the current path.</summary>
        public void BeginEdit()
        {
            if (_editor != null) return;
            _editor = new ModernTextBox
            {
                Bounds = new Rectangle(0, 0, Width, Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                OverrideSkinFont = true,
                Font = Fonts.Code(13f),
            };
            _editor.KeyDown += Editor_KeyDown;
            _editor.Leave += Editor_Leave;
            Controls.Add(_editor);
            _editor.CreateControl();
            // ModernTextBox drops text assigned before its inner edit box exists.
            _editor.Text = _path;
            _editor.Focus();
            Invalidate();
        }

        public void EndEdit()
        {
            var editor = _editor;
            if (editor == null) return;
            _editor = null;
            editor.KeyDown -= Editor_KeyDown;
            editor.Leave -= Editor_Leave;
            Controls.Remove(editor);
            editor.Dispose();
            Invalidate();
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = e.SuppressKeyPress = true;
                var target = (_editor?.Text ?? string.Empty).Trim();
                EndEdit();
                if (target.Length > 0) PathRequested?.Invoke(this, new PathEventArgs(RemotePath.Normalize(target)));
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.Handled = e.SuppressKeyPress = true;
                EndEdit();
            }
        }

        private void Editor_Leave(object sender, EventArgs e)
        {
            // Disposing the box inside its own focus change upsets WinForms; finish afterwards.
            if (_editor != null && IsHandleCreated) BeginInvoke((Action)EndEdit);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Theme.Changed -= OnThemeChanged;
                EndEdit();
            }
            base.Dispose(disposing);
        }
    }
}
