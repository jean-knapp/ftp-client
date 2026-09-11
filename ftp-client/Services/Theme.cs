using System;
using System.Drawing;
using ModernWinForms;
using ModernWinForms.Enums;

namespace FtpClient.Services
{
    public enum ThemeMode
    {
        Dark,
        Light,
    }

    /// <summary>
    /// The design tokens from the Fluent handoff - the same set the Git Client uses, plus the
    /// transfer semantics (--up, --down, --err, --folder). One instance per theme; every
    /// custom-drawn control reads its colours from <see cref="Theme.Palette"/> so a theme switch
    /// repaints the whole application consistently.
    /// </summary>
    public sealed class ThemePalette
    {
        public ThemeMode Mode { get; private set; }

        // Surfaces
        public Color Background { get; private set; }      // --bg
        public Color Layer { get; private set; }           // --layer
        public Color Fill { get; private set; }            // --fill
        public Color Fill2 { get; private set; }           // --fill2
        public Color Stroke { get; private set; }          // --stroke
        public Color CardStroke { get; private set; }      // --cardstroke
        public Color Divider { get; private set; }         // --div
        public Color Hover { get; private set; }           // --hov
        public Color Selection { get; private set; }       // --sel

        // Text
        public Color Foreground { get; private set; }      // --fg
        public Color Foreground2 { get; private set; }     // --fg2
        public Color Foreground3 { get; private set; }     // --fg3

        // Accent
        public Color Accent { get; private set; }          // --acc
        public Color AccentFill { get; private set; }      // --accfill
        public Color AccentForeground { get; private set; }// --accfg

        // Semantics
        public Color Up { get; private set; }              // --up: uploads, connected, additions
        public Color Down { get; private set; }            // --down: downloads
        public Color Warning { get; private set; }         // --warn
        public Color Error { get; private set; }           // --err: failures, deletions
        public Color Folder { get; private set; }          // --folder: folder glyphs, modified files

        // Accents for protocol and pairing
        public Color Lane { get; private set; }            // --lane: servers, protocols
        public Color Lane2 { get; private set; }           // --lane2: pairing, git, avatars, symlinks

        public static readonly ThemePalette Dark = new ThemePalette
        {
            Mode = ThemeMode.Dark,
            Background = Rgb(0x202020),
            Layer = Rgb(0x272727),
            Fill = Argb(14, 255, 255, 255),      // rgba(255,255,255,.055)
            Fill2 = Argb(23, 255, 255, 255),     // rgba(255,255,255,.09)
            Stroke = Argb(23, 255, 255, 255),
            // Composited over a card's own --layer this is #383838, the same hairline as the
            // dividers inside the cards.
            CardStroke = Argb(20, 255, 255, 255),
            Divider = Argb(20, 255, 255, 255),
            Hover = Argb(11, 255, 255, 255),
            Selection = Argb(33, 96, 205, 255),  // rgba(96,205,255,.13)
            Foreground = Rgb(0xffffff),
            // --fg2 and --fg3 are translucent in the design, but GDI text rendering drops the alpha
            // channel, so they are stored pre-composited over --layer (#272727): white at .78 and
            // .54 respectively. The difference against --bg instead is under two levels.
            Foreground2 = Rgb(0xcfcfcf),
            Foreground3 = Rgb(0x9c9c9c),
            Accent = Rgb(0x60cdff),
            AccentFill = Rgb(0x4cc2ff),
            AccentForeground = Rgb(0x00344f),
            Up = Rgb(0x6ed282),
            Down = Rgb(0x60cdff),
            Warning = Rgb(0xfcd34d),
            Error = Rgb(0xf06e6e),
            Folder = Rgb(0xffc45c),
            Lane = Rgb(0x34c8eb),
            Lane2 = Rgb(0xb284ff),
        };

        public static readonly ThemePalette Light = new ThemePalette
        {
            Mode = ThemeMode.Light,
            Background = Rgb(0xf3f3f3),
            Layer = Rgb(0xffffff),
            Fill = Argb(7, 0, 0, 0),             // rgba(0,0,0,.028)
            Fill2 = Argb(15, 0, 0, 0),           // rgba(0,0,0,.06)
            Stroke = Argb(28, 0, 0, 0),          // rgba(0,0,0,.11)
            CardStroke = Argb(20, 0, 0, 0),
            Divider = Argb(20, 0, 0, 0),
            Hover = Argb(10, 0, 0, 0),
            Selection = Argb(23, 0, 95, 184),    // rgba(0,95,184,.09)
            Foreground = Rgb(0x191919),
            // Pre-composited over --layer (#ffffff): black at .74 and .55.
            Foreground2 = Rgb(0x424242),
            Foreground3 = Rgb(0x737373),
            Accent = Rgb(0x005fb8),
            AccentFill = Rgb(0x005fb8),
            AccentForeground = Rgb(0xffffff),
            Up = Rgb(0x107c41),
            Down = Rgb(0x005fb8),
            Warning = Rgb(0x9a6b00),
            Error = Rgb(0xc42b1c),
            Folder = Rgb(0xc98a00),
            Lane = Rgb(0x0a8fb0),
            Lane2 = Rgb(0x6b3fc9),
        };

        private static Color Rgb(int value) => Color.FromArgb(255, (value >> 16) & 0xff, (value >> 8) & 0xff, value & 0xff);
        private static Color Argb(int alpha, int r, int g, int b) => Color.FromArgb(alpha, r, g, b);

        /// <summary>Flattens a translucent token onto a background so it can be used where alpha is not supported.</summary>
        public static Color Flatten(Color over, Color under)
        {
            if (over.A == 255) return over;
            float a = over.A / 255f;
            return Color.FromArgb(255,
                (int)Math.Round(over.R * a + under.R * (1 - a)),
                (int)Math.Round(over.G * a + under.G * (1 - a)),
                (int)Math.Round(over.B * a + under.B * (1 - a)));
        }

        /// <summary>
        /// A semantic colour at a fraction of its strength over a surface - the chip fills in the
        /// design ("--up at 16 %") are tints of the token rather than colours of their own, which
        /// is what keeps them right in both themes.
        /// </summary>
        public static Color Tint(Color semantic, double strength, Color surface)
        {
            int alpha = (int)Math.Round(255 * Math.Max(0, Math.Min(1, strength)));
            return Flatten(Color.FromArgb(alpha, semantic.R, semantic.G, semantic.B), surface);
        }

        public Color FillOn(Color surface) => Flatten(Fill, surface);
        public Color Fill2On(Color surface) => Flatten(Fill2, surface);
        public Color StrokeOn(Color surface) => Flatten(Stroke, surface);
        public Color CardStrokeOn(Color surface) => Flatten(CardStroke, surface);
        public Color DividerOn(Color surface) => Flatten(Divider, surface);
        public Color HoverOn(Color surface) => Flatten(Hover, surface);
        public Color SelectionOn(Color surface) => Flatten(Selection, surface);

        /// <summary>Colour of a changed file in a commit: modified, added or deleted.</summary>
        public Color StatusColor(Git.FileChangeKind kind)
        {
            switch (kind)
            {
                case Git.FileChangeKind.Added:
                case Git.FileChangeKind.Untracked: return Up;
                case Git.FileChangeKind.Deleted: return Error;
                case Git.FileChangeKind.Conflicted: return Warning;
                default: return Folder;
            }
        }
    }

    /// <summary>Application-wide theme. Controls read <see cref="Palette"/> and repaint on <see cref="Changed"/>.</summary>
    public static class Theme
    {
        private static ThemeMode _mode = ThemeMode.Dark;
        private static ModernSkin _skin;

        /// <summary>Raised after the theme changes, so custom-drawn controls can repaint.</summary>
        public static event EventHandler Changed;

        public static ThemeMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                // The skin instance is kept and re-filled, so every control already bound to it
                // repaints through ModernSkin.Changed instead of needing a new reference.
                if (_skin != null) CopyInto(_skin, Palette);
                Changed?.Invoke(null, EventArgs.Empty);
            }
        }

        public static ThemePalette Palette => _mode == ThemeMode.Light ? ThemePalette.Light : ThemePalette.Dark;

        /// <summary>A ModernSkin built from the palette, so library controls match the custom-drawn ones.</summary>
        public static ModernSkin Skin => _skin ?? (_skin = BuildSkin(Palette));

        /// <summary>Applies the current theme to a skin instance that forms already reference.</summary>
        public static void Apply(ModernSkin target)
        {
            if (target == null) return;
            CopyInto(target, Palette);
        }

        private static ModernSkin BuildSkin(ThemePalette p)
        {
            var skin = new ModernSkin();
            CopyInto(skin, p);
            return skin;
        }

        private static void CopyInto(ModernSkin s, ThemePalette p)
        {
            var layer = p.Layer;
            var fillOnLayer = p.FillOn(layer);
            var fill2OnLayer = p.Fill2On(layer);
            var strokeOnLayer = p.StrokeOn(layer);

            s.Control.BackColor = p.Background;
            s.Control.ForeColor = p.Foreground;

            s.Form.TitleBar.BackColor = p.Background;
            s.Form.TitleBar.ButtonColor = p.Foreground2;
            s.Form.TitleBar.TitleColor = p.Foreground2;

            s.Label.BackColor = Color.Transparent;
            s.Label.ForeColor = p.Foreground;

            s.Panel.BackColor = p.Background;
            s.Panel.BorderColor = Color.Transparent;
            s.Panel.CornerStyle = CornerStyle.Square;

            s.GroupBox.BackColor = layer;
            s.GroupBox.BorderColor = p.CardStrokeOn(p.Background);
            s.GroupBox.CornerStyle = CornerStyle.SmallRound;
            s.GroupBox.Header.BackColor = fillOnLayer;
            s.GroupBox.Header.ForeColor = p.Foreground;

            s.TextBox.CornerStyle = CornerStyle.SmallRound;
            s.TextBox.Colors.BorderColor = strokeOnLayer;
            s.TextBox.Colors.Normal.BackColor = fillOnLayer;
            s.TextBox.Colors.Normal.ForeColor = p.Foreground;
            s.TextBox.Colors.Hover.BackColor = fill2OnLayer;
            s.TextBox.Colors.Hover.BorderColor = strokeOnLayer;
            s.TextBox.Colors.Active.BackColor = fillOnLayer;
            s.TextBox.Colors.Active.BorderColor = p.Accent;
            s.TextBox.Colors.Disabled.BackColor = fillOnLayer;
            s.TextBox.Colors.Disabled.ForeColor = p.Foreground3;
            s.TextBox.Colors.Disabled.BorderColor = strokeOnLayer;

            s.ComboBox.CornerStyle = CornerStyle.SmallRound;
            s.ComboBox.Colors.BorderColor = strokeOnLayer;
            s.ComboBox.Colors.Normal.BackColor = fillOnLayer;
            s.ComboBox.Colors.Normal.ForeColor = p.Foreground;
            s.ComboBox.Colors.Hover.BackColor = fill2OnLayer;
            s.ComboBox.Colors.Hover.BorderColor = strokeOnLayer;
            s.ComboBox.Colors.Active.BackColor = fillOnLayer;
            s.ComboBox.Colors.Active.BorderColor = p.Accent;
            s.ComboBox.Colors.Disabled.BackColor = fillOnLayer;
            s.ComboBox.Colors.Disabled.ForeColor = p.Foreground3;
            s.ComboBox.Colors.Disabled.BorderColor = strokeOnLayer;
            s.ComboBox.DropDown.BackColor = layer;
            s.ComboBox.DropDown.BorderColor = strokeOnLayer;
            s.ComboBox.DropDown.HoverColor = p.HoverOn(layer);
            s.ComboBox.DropDown.SelectedColor = p.SelectionOn(layer);
            s.ComboBox.DropDown.CornerStyle = CornerStyle.SmallRound;

            s.Button.CornerStyle = CornerStyle.SmallRound;
            s.Button.Colors.BorderColor = strokeOnLayer;
            s.Button.Colors.Normal.BackColor = fillOnLayer;
            s.Button.Colors.Normal.ForeColor = p.Foreground;
            s.Button.Colors.Hover.BackColor = fill2OnLayer;
            s.Button.Colors.Pressed.BackColor = p.FillOn(layer);
            s.Button.Colors.Disabled.BackColor = fillOnLayer;
            s.Button.Colors.Disabled.ForeColor = p.Foreground3;
            s.Button.Colors.Checked.BackColor = p.SelectionOn(layer);
            s.Button.Colors.Checked.HoverBackColor = p.SelectionOn(layer);
            s.Button.Colors.Checked.ForeColor = p.Foreground;
            s.Button.Colors.Checked.BorderColor = p.Accent;

            s.TreeView.BackColor = layer;
            s.TreeView.BorderColor = Color.Transparent;
            s.TreeView.CornerStyle = CornerStyle.Square;
            s.TreeView.Colors.Normal.BackColor = layer;
            s.TreeView.Colors.Normal.ForeColor = p.Foreground;
            s.TreeView.Colors.Hover.BackColor = p.HoverOn(layer);
            s.TreeView.Colors.Hover.ForeColor = p.Foreground;
            s.TreeView.Colors.Selected.BackColor = p.SelectionOn(layer);
            s.TreeView.Colors.Selected.ForeColor = p.Foreground;
            s.TreeView.Colors.SelectedUnfocused.BackColor = p.Fill2On(layer);
            s.TreeView.Colors.SelectedUnfocused.ForeColor = p.Foreground2;
            s.TreeView.Colors.Header.BackColor = fillOnLayer;
            s.TreeView.Colors.Header.ForeColor = p.Foreground3;

            s.ScrollBar.TrackColor = Color.Transparent;
            s.ScrollBar.ThumbColor = fill2OnLayer;
            s.ScrollBar.ThumbHoverColor = p.Foreground3;

            // The track matches the body the cards sit on; the grip dots are derived from it.
            s.SplitContainer.Colors.SplitterColor = p.Background;
            s.SplitContainer.Colors.PressedColor = p.Accent;

            s.ProgressBar.CornerStyle = CornerStyle.Round;
            s.ProgressBar.Colors.TrackColor = fillOnLayer;
            s.ProgressBar.Colors.BorderColor = Color.Transparent;
            s.ProgressBar.Colors.FillColor = p.AccentFill;

            s.ToggleSwitch.OffTrackColor = Color.Transparent;
            s.ToggleSwitch.OffBorderColor = p.Foreground3;
            s.ToggleSwitch.OffThumbColor = p.Foreground2;
            s.ToggleSwitch.OnTrackColor = p.AccentFill;
            s.ToggleSwitch.OnThumbColor = p.AccentForeground;
            s.ToggleSwitch.ForeColor = p.Foreground2;

            s.ToolTip.BackColor = layer;
            s.ToolTip.BorderColor = strokeOnLayer;
            s.ToolTip.TitleColor = p.Foreground;
            s.ToolTip.TextColor = p.Foreground2;
            s.ToolTip.CornerStyle = CornerStyle.SmallRound;

            s.MenuBar.BackColor = Color.Transparent;
            s.MenuBar.DropDown.BackColor = layer;
            s.MenuBar.DropDown.ForeColor = p.Foreground;
            s.MenuBar.DropDown.HoverColor = p.HoverOn(layer);
            s.MenuBar.DropDown.BorderColor = strokeOnLayer;
            s.MenuBar.DropDown.CornerStyle = CornerStyle.SmallRound;
            s.MenuBar.ItemColors.Normal.BackColor = Color.Transparent;
            s.MenuBar.ItemColors.Normal.ForeColor = p.Foreground2;
            s.MenuBar.ItemColors.Hover.BackColor = p.HoverOn(p.Background);
            s.MenuBar.ItemColors.Hover.ForeColor = p.Foreground;
            s.MenuBar.ItemColors.Pressed.BackColor = p.Fill2On(p.Background);
            s.MenuBar.ItemColors.Pressed.ForeColor = p.Foreground;
            s.MenuBar.ItemColors.Disabled.BackColor = Color.Transparent;
            s.MenuBar.ItemColors.Disabled.ForeColor = p.Foreground3;

            s.TabControl.Colors.BackColor = Color.Transparent;
            s.TabControl.Colors.TabForeColor = p.Foreground2;
            s.TabControl.Colors.HoverBackColor = p.HoverOn(p.Background);
            s.TabControl.Colors.HoverForeColor = p.Foreground;
            s.TabControl.Colors.SelectedForeColor = p.Foreground;
            s.TabControl.Colors.SelectedBackColor = p.FillOn(p.Background);
            s.TabControl.Colors.AccentColor = p.AccentFill;
            s.TabControl.Colors.SeparatorColor = Color.Transparent;
        }
    }
}
