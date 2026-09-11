using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FtpClient.Remote;
using FtpClient.Services;

namespace FtpClient.Controls
{
    public sealed class SiteEventArgs : EventArgs
    {
        public SiteEventArgs(Site site) { Site = site; }
        public Site Site { get; }
    }

    public sealed class SiteMenuEventArgs : EventArgs
    {
        public SiteMenuEventArgs(Site site, Point screenLocation)
        {
            Site = site;
            ScreenLocation = screenLocation;
        }

        public Site Site { get; }

        /// <summary>Where the menu opens: the pointer, or under the row's name from the keyboard.</summary>
        public Point ScreenLocation { get; }
    }

    /// <summary>
    /// SAVED SITES on the connect screen: 52 px rows with a protocol tile, the site name over its
    /// endpoint in monospace, and the sign-in method and when it was last used on the right.
    /// </summary>
    [ToolboxItem(true)]
    [DefaultEvent("SiteActivated")]
    public sealed class SavedSitesListControl : VirtualListControl
    {
        private const int RowHeight = 52;
        private const int SidePadding = 16;
        private const int TileSize = 30;

        private readonly List<Site> _sites = new List<Site>();

        public event EventHandler<SiteEventArgs> SiteActivated;

        /// <summary>A row was right-clicked, or the menu key pressed on the selected row.</summary>
        public event EventHandler<SiteMenuEventArgs> SiteMenuRequested;

        public SavedSitesListControl()
        {
            EmptyText = "No saved sites yet. Connect to a host and choose Save as site.";
        }

        /// <summary>Lets rows say "Connected now" for sites with an open tab.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<Site, bool> IsConnected { get; set; }

        protected override int RowCount => _sites.Count;
        protected override int GetRowHeight(int index) => RowHeight;

        public Site SiteAt(int index) => index >= 0 && index < _sites.Count ? _sites[index] : null;

        public void SetSites(IEnumerable<Site> sites)
        {
            _sites.Clear();
            if (sites != null) _sites.AddRange(sites);
            RestoreState(-1, 0);
            ContentChanged();
        }

        /// <summary>The colour a protocol's tile is tinted with.</summary>
        public static Color ProtocolColor(RemoteProtocol protocol)
        {
            var p = Theme.Palette;
            switch (protocol)
            {
                case RemoteProtocol.Sftp: return p.Up;
                case RemoteProtocol.Ftp: return p.Mode == ThemeMode.Light ? p.Down : Color.FromArgb(0x6f, 0xc9, 0xf5);
                case RemoteProtocol.Ftps: return p.Lane;
                default: return p.Folder;
            }
        }

        /// <summary>How a site signs in, in the words the list uses: <c>ed25519 key</c>, <c>Password</c>.</summary>
        public static string AuthDescription(Site site)
        {
            if (site == null) return string.Empty;
            if (site.Protocol == RemoteProtocol.Local) return "Local folder";
            switch (site.Auth)
            {
                case AuthMethod.Anonymous: return "Anonymous";
                case AuthMethod.KeyFile:
                    var name = Path.GetFileName(site.KeyFile ?? string.Empty).ToLowerInvariant();
                    if (name.Contains("ed25519")) return "ed25519 key";
                    if (name.Contains("ecdsa")) return "ECDSA key";
                    if (name.Contains("rsa")) return "RSA key";
                    return "Key file";
                default: return "Password";
            }
        }

        /// <summary>Paints a protocol tile: a rounded square with the protocol name in bold.</summary>
        public static void PaintTile(Graphics g, Rectangle tile, RemoteProtocol protocol, Color face)
        {
            var color = ProtocolColor(protocol);
            Draw.FillRounded(g, tile, 6f, ThemePalette.Tint(color, 0.16, face));
            var text = protocol == RemoteProtocol.Local ? "DIR" : FtpClient.Remote.Site.NameOf(protocol);
            Draw.Text(g, text, Fonts.Ui(text.Length > 3 ? 9f : 10f, true), tile, color, Draw.CenterMiddle);
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var site = _sites[index];

            var face = surface;
            if ((state & RowState.Selected) != 0 && (state & RowState.Focused) != 0) face = p.SelectionOn(surface);
            else if ((state & RowState.Hot) != 0) face = p.HoverOn(surface);
            if (face != surface) Draw.Fill(g, bounds, face);
            if (index < _sites.Count - 1) Draw.HLine(g, 0, bounds.Bottom - 1, bounds.Width, p.DividerOn(surface));

            int x = SidePadding;
            PaintTile(g, new Rectangle(x, bounds.Y + (RowHeight - TileSize) / 2, TileSize, TileSize), site.Protocol, face);
            x += TileSize + 12;

            var metaFont = Fonts.Ui(12.5f);
            bool connected = IsConnected != null && IsConnected(site);
            var when = connected ? "Connected now" : site.LastUsed.HasValue ? Format.Relative(site.LastUsed) : "Never used";
            var auth = AuthDescription(site);
            int right = bounds.Right - SidePadding;
            int whenWidth = Draw.MeasureWidth(when, metaFont);
            Draw.Text(g, when, metaFont, new Rectangle(right - whenWidth, bounds.Y, whenWidth + 1, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            right -= whenWidth + 14;
            int authWidth = Draw.MeasureWidth(auth, metaFont);
            Draw.Text(g, auth, metaFont, new Rectangle(right - authWidth, bounds.Y, authWidth + 1, bounds.Height), p.Foreground3, Draw.LeftMiddle);
            right -= authWidth + 16;

            int available = Math.Max(0, right - x);
            Draw.Text(g, site.Name, Fonts.Ui(14f, true), new Rectangle(x, bounds.Y + 8, available, 19), p.Foreground, Draw.LeftMiddle);
            Draw.Text(g, site.Endpoint, Fonts.Code(12f), new Rectangle(x, bounds.Y + 27, available, 17), p.Foreground3, Draw.LeftMiddle);
        }

        protected override void OnRowClicked(int index, MouseEventArgs e)
        {
            base.OnRowClicked(index, e);
            var site = SiteAt(index);
            if (site != null && e.Button == MouseButtons.Left) SiteActivated?.Invoke(this, new SiteEventArgs(site));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Right) return;
            var site = SiteAt(RowIndexAt(e.Location));
            if (site != null) SiteMenuRequested?.Invoke(this, new SiteMenuEventArgs(site, PointToScreen(e.Location)));
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var site = SiteAt(SelectedIndex);
                if (site != null) SiteActivated?.Invoke(this, new SiteEventArgs(site));
                e.Handled = true;
                return;
            }
            if (e.KeyCode == Keys.Apps || e.KeyData == (Keys.Shift | Keys.F10))
            {
                int index = SelectedIndex;
                var site = SiteAt(index);
                if (site != null)
                {
                    EnsureVisible(index);
                    var location = PointToScreen(new Point(SidePadding + TileSize + 12, RowTop(index) + RowHeight - 6));
                    SiteMenuRequested?.Invoke(this, new SiteMenuEventArgs(site, location));
                }
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
        }
    }

    /// <summary>
    /// The site manager's rail: collapsible group headers in small capitals over 38 px site rows
    /// with a connection dot, the name, and <c>protocol · user@host</c>.
    /// </summary>
    [ToolboxItem(true)]
    public sealed class SiteTreeListControl : VirtualListControl
    {
        private const int GroupHeight = 30;
        private const int SiteHeight = 40;
        private const int RowMargin = 8;

        private sealed class Row
        {
            public string Group;
            public Site Site;
        }

        private readonly List<Row> _rows = new List<Row>();
        private readonly HashSet<string> _collapsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private List<KeyValuePair<string, List<Site>>> _groups = new List<KeyValuePair<string, List<Site>>>();

        public SiteTreeListControl()
        {
            EmptyText = "No sites";
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<Site, bool> IsConnected { get; set; }

        protected override int RowCount => _rows.Count;
        protected override int GetRowHeight(int index) => _rows[index].Site == null ? GroupHeight : SiteHeight;
        protected override bool IsSelectable(int index) => _rows[index].Site != null;

        [Browsable(false)]
        public Site SelectedSite
        {
            get
            {
                int index = SelectedIndex;
                return index >= 0 && index < _rows.Count ? _rows[index].Site : null;
            }
        }

        public void SetSites(IEnumerable<KeyValuePair<string, List<Site>>> groups, string selectId)
        {
            _groups = new List<KeyValuePair<string, List<Site>>>(groups ?? new KeyValuePair<string, List<Site>>[0]);
            Rebuild(selectId);
        }

        public void SelectSite(string id, bool raiseEvent)
        {
            int index = _rows.FindIndex(r => r.Site != null && r.Site.Id == id);
            if (index < 0 && id != null)
            {
                // A site inside a collapsed group opens it.
                foreach (var group in _groups)
                {
                    if (group.Value.Exists(s => s.Id == id)) _collapsed.Remove(group.Key);
                }
                Rebuild(id);
                index = _rows.FindIndex(r => r.Site != null && r.Site.Id == id);
            }
            SetSelection(index, raiseEvent);
            if (index >= 0) EnsureVisible(index);
        }

        private void Rebuild(string selectId)
        {
            _rows.Clear();
            foreach (var group in _groups)
            {
                _rows.Add(new Row { Group = group.Key });
                if (_collapsed.Contains(group.Key)) continue;
                foreach (var site in group.Value) _rows.Add(new Row { Site = site });
            }
            int index = selectId == null ? -1 : _rows.FindIndex(r => r.Site != null && r.Site.Id == selectId);
            RestoreState(index, ScrollOffset);
            ContentChanged();
        }

        protected override void PaintRow(Graphics g, int index, Rectangle bounds, RowState state)
        {
            var p = P;
            var surface = RowSurface;
            var row = _rows[index];

            if (row.Site == null)
            {
                bool collapsed = _collapsed.Contains(row.Group);
                IconCache.DrawCentered(g, collapsed ? Icons.ChevronRight : Icons.ChevronDown, 9, p.Foreground3, RowMargin + 8, bounds.Y + bounds.Height / 2 + 1);
                Draw.Text(g, row.Group.ToUpperInvariant(), Fonts.Ui(11f, true), new Rectangle(RowMargin + 20, bounds.Y + 2, bounds.Width - RowMargin * 2 - 20, bounds.Height), p.Foreground3, Draw.LeftMiddle);
                return;
            }

            var site = row.Site;
            var item = new Rectangle(RowMargin, bounds.Y + 1, Math.Max(0, bounds.Width - RowMargin * 2), SiteHeight - 2);
            bool selected = (state & RowState.Selected) != 0;
            if (selected)
            {
                Draw.FillRounded(g, item, 5f, p.SelectionOn(surface));
                Draw.FillRounded(g, new Rectangle(item.X, item.Y + 11, 3, item.Height - 22), 1.5f, p.AccentFill);
            }
            else if ((state & RowState.Hot) != 0)
            {
                Draw.FillRounded(g, item, 5f, p.HoverOn(surface));
            }

            bool connected = IsConnected != null && IsConnected(site);
            int x = RowMargin + 16;
            Draw.Dot(g, x, item.Y + item.Height / 2f, 7, connected ? p.Up : p.Foreground3);
            x += 7 + 10;
            int width = Math.Max(0, item.Right - 8 - x);
            Draw.Text(g, site.Name, Fonts.Ui(13.5f, selected), new Rectangle(x, item.Y + 2, width, 19), p.Foreground, Draw.LeftMiddle);
            var detail = site.Protocol == RemoteProtocol.Local
                ? "local · " + site.Host
                : site.ProtocolName.ToLowerInvariant() + " · " + (string.IsNullOrEmpty(site.User) ? string.Empty : site.User + "@") + site.Host;
            Draw.Text(g, detail, Fonts.Ui(11.5f), new Rectangle(x, item.Y + 20, width, 16), p.Foreground3, Draw.LeftMiddle);
        }

        protected override void OnRowClicked(int index, MouseEventArgs e)
        {
            base.OnRowClicked(index, e);
            if (index < 0 || index >= _rows.Count || _rows[index].Site != null) return;
            var group = _rows[index].Group;
            if (!_collapsed.Remove(group)) _collapsed.Add(group);
            Rebuild(SelectedSite?.Id);
        }
    }
}
