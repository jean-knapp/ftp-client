using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ModernWinForms;

namespace FtpClient.Controls
{
    /// <summary>
    /// Font cache for the redesign. The design was authored in CSS pixels at 96 DPI, so fonts are
    /// created in pixel units and the stated sizes carry over unchanged.
    /// </summary>
    public static class Fonts
    {
        private const string Primary = "Segoe UI Variable Text";
        private const string Fallback = "Segoe UI";
        private const string Mono = "Cascadia Mono";
        private const string MonoFallback = "Consolas";

        private static readonly Dictionary<string, Font> Cache = new Dictionary<string, Font>(StringComparer.Ordinal);
        private static string _uiFamily;
        private static string _monoFamily;

        private static string UiFamily => _uiFamily ?? (_uiFamily = Resolve(Primary, Fallback));
        private static string MonoFamily => _monoFamily ?? (_monoFamily = Resolve(Mono, MonoFallback));

        private static string Resolve(string preferred, string fallback)
        {
            try
            {
                using (var probe = new Font(preferred, 10f))
                {
                    if (string.Equals(probe.Name, preferred, StringComparison.OrdinalIgnoreCase)) return preferred;
                }
            }
            catch { }
            return fallback;
        }

        /// <summary>A UI font of the given size in CSS pixels.</summary>
        public static Font Ui(float pixels, bool semibold = false)
        {
            return Get(UiFamily, pixels, semibold ? FontStyle.Bold : FontStyle.Regular);
        }

        /// <summary>A monospace font of the given size in CSS pixels.</summary>
        public static Font Code(float pixels, bool bold = false)
        {
            return Get(MonoFamily, pixels, bold ? FontStyle.Bold : FontStyle.Regular);
        }

        private static Font Get(string family, float pixels, FontStyle style)
        {
            var key = family + "|" + pixels.ToString("0.##") + "|" + (int)style;
            Font font;
            if (Cache.TryGetValue(key, out font)) return font;
            font = new Font(family, pixels, style, GraphicsUnit.Pixel);
            Cache[key] = font;
            return font;
        }
    }

    /// <summary>
    /// Rasterises the SVG glyphs in <see cref="Icons"/>. SvgIconRenderer is internal to
    /// ModernWinForms, so rendering goes through a shared ModernImageList, which caches each
    /// bitmap per size and colour.
    /// </summary>
    public static class IconCache
    {
        private static readonly ModernImageList Images = new ModernImageList();
        private static readonly Dictionary<string, string> Keys = new Dictionary<string, string>(StringComparer.Ordinal);
        private static int _next;

        public static Image Get(string svg, int size, Color color)
        {
            if (string.IsNullOrEmpty(svg) || size <= 0) return null;
            string key;
            if (!Keys.TryGetValue(svg, out key))
            {
                key = "i" + (_next++).ToString();
                Keys[svg] = key;
                Images.Images.Add(new ModernImageListItem { Key = key, Svg = svg });
            }
            return Images.GetImage(key, size, color);
        }

        /// <summary>Draws a glyph centred on the given point.</summary>
        public static void DrawCentered(Graphics g, string svg, int size, Color color, int centerX, int centerY)
        {
            var image = Get(svg, size, color);
            if (image == null) return;
            g.DrawImage(image, centerX - size / 2, centerY - size / 2, size, size);
        }

        /// <summary>Draws a glyph vertically centred in the rectangle, at its left edge.</summary>
        public static void DrawLeft(Graphics g, string svg, int size, Color color, Rectangle bounds)
        {
            var image = Get(svg, size, color);
            if (image == null) return;
            g.DrawImage(image, bounds.X, bounds.Y + (bounds.Height - size) / 2, size, size);
        }
    }

    /// <summary>Painting helpers shared by the custom-drawn controls.</summary>
    public static class Draw
    {
        public const TextFormatFlags LeftMiddle =
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        public const TextFormatFlags RightMiddle =
            TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;

        public const TextFormatFlags CenterMiddle =
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;

        public static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            var path = new GraphicsPath();
            if (radius <= 0.01f)
            {
                path.AddRectangle(r);
                return path;
            }
            float d = Math.Min(radius, Math.Min(r.Width, r.Height) / 2f) * 2f;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void FillRounded(Graphics g, Rectangle bounds, float radius, Color color)
        {
            if (color.A == 0) return;
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRect(bounds, radius))
            using (var brush = new SolidBrush(color))
            {
                g.FillPath(brush, path);
            }
            g.SmoothingMode = old;
        }

        public static void DrawRounded(Graphics g, Rectangle bounds, float radius, Color color, float width = 1f)
        {
            if (color.A == 0) return;
            var old = g.SmoothingMode;
            var offset = g.PixelOffsetMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // Pixel centres at .5 keep a hairline on exactly one row of pixels; with the default
            // mode a 1 px border straddles two rows and renders at about half its colour.
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            var r = new RectangleF(bounds.X + width / 2f, bounds.Y + width / 2f, bounds.Width - width, bounds.Height - width);
            using (var path = RoundedRect(r, radius))
            using (var pen = new Pen(color, width))
            {
                g.DrawPath(pen, path);
            }
            g.PixelOffsetMode = offset;
            g.SmoothingMode = old;
        }

        /// <summary>Card surface: fill plus a hairline border.</summary>
        public static void Card(Graphics g, Rectangle bounds, float radius, Color fill, Color border)
        {
            FillRounded(g, bounds, radius, fill);
            DrawRounded(g, bounds, radius, border);
        }

        public static void Fill(Graphics g, Rectangle bounds, Color color)
        {
            if (color.A == 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
            using (var brush = new SolidBrush(color)) g.FillRectangle(brush, bounds);
        }

        /// <summary>One-pixel horizontal rule.</summary>
        public static void HLine(Graphics g, int x, int y, int width, Color color)
        {
            Fill(g, new Rectangle(x, y, width, 1), color);
        }

        /// <summary>One-pixel vertical rule.</summary>
        public static void VLine(Graphics g, int x, int y, int height, Color color)
        {
            Fill(g, new Rectangle(x, y, 1, height), color);
        }

        public static void Text(Graphics g, string text, Font font, Rectangle bounds, Color color, TextFormatFlags flags)
        {
            if (string.IsNullOrEmpty(text)) return;
            TextRenderer.DrawText(g, text, font, bounds, color, flags);
        }

        public static int MeasureWidth(string text, Font font)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            return TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        }

        /// <summary>A rounded pill with centred text, returning the width it used.</summary>
        public static int Pill(Graphics g, int x, int y, int height, string text, Font font, Color fill, Color fore, Color border, int paddingX = 8)
        {
            int width = MeasureWidth(text, font) + paddingX * 2;
            var bounds = new Rectangle(x, y, width, height);
            FillRounded(g, bounds, height / 2f, fill);
            if (border.A > 0) DrawRounded(g, bounds, height / 2f, border);
            Text(g, text, font, bounds, fore, CenterMiddle);
            return width;
        }

        /// <summary>A filled status dot whose left edge is at <paramref name="x"/>.</summary>
        public static void Dot(Graphics g, float x, float centerY, float size, Color color)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(color)) g.FillEllipse(brush, x, centerY - size / 2f, size, size);
            g.SmoothingMode = old;
        }

        /// <summary>A small rounded chip with centred text, returning the width it used.</summary>
        public static int Tag(Graphics g, int x, int y, int height, string text, Font font, Color fill, Color fore, int paddingX = 6, float radius = 4f)
        {
            int width = MeasureWidth(text, font) + paddingX * 2;
            var bounds = new Rectangle(x, y, width, height);
            FillRounded(g, bounds, radius, fill);
            Text(g, text, font, bounds, fore, CenterMiddle);
            return width;
        }

        /// <summary>A circular avatar carrying one or two initials.</summary>
        public static void Avatar(Graphics g, Rectangle bounds, string initials, Font font, Color fill, Color fore)
        {
            var old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var brush = new SolidBrush(fill)) g.FillEllipse(brush, bounds);
            g.SmoothingMode = old;
            Text(g, initials, font, bounds, fore, CenterMiddle);
        }

        public static string Initials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpperInvariant();
            return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpperInvariant();
        }

        /// <summary>Splits a repository-relative path into its directory and file name parts.</summary>
        public static void SplitPath(string path, out string directory, out string fileName)
        {
            if (string.IsNullOrEmpty(path)) { directory = string.Empty; fileName = string.Empty; return; }
            int slash = path.LastIndexOf('/');
            if (slash < 0) { directory = string.Empty; fileName = path; return; }
            directory = path.Substring(0, slash + 1);
            fileName = path.Substring(slash + 1);
        }
    }
}
