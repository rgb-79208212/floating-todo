using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace FloatingTodo
{
    internal enum ThemeMode
    {
        Light,
        Dark
    }

    internal enum ThemeRole
    {
        CardTop,
        CardBottom,
        CardBorder,
        TextPrimary,
        TextSecondary,
        TextMuted,
        Accent,
        AccentInk,
        AccentSoft,
        AccentBorder,
        Danger,
        DangerSoft,
        DangerInk,
        TaskSurface,
        TaskSurfaceAlt,
        TaskHover,
        TaskBorder,
        TimeText,
        TimeSub,
        Input,
        InputBorder,
        Popup,
        PopupBorder,
        Control,
        ControlBorder,
        Selection,
        Track,
        Overlay,
        OverlayTint,
        UrgentMarker,
        UrgentMarkerInk,
        UrgentMarkerSub,
        RowShadow
    }

    internal sealed class ThemePalette
    {
        private readonly Dictionary<ThemeRole, Color> _colors;
        private readonly Dictionary<ThemeRole, SolidColorBrush> _brushes;

        public ThemeMode Mode { get; private set; }
        public Color CardTop { get { return GetColor(ThemeRole.CardTop); } }
        public Color CardBottom { get { return GetColor(ThemeRole.CardBottom); } }
        public Color CardBorder { get { return GetColor(ThemeRole.CardBorder); } }
        public Color TextPrimary { get { return GetColor(ThemeRole.TextPrimary); } }
        public Color TextSecondary { get { return GetColor(ThemeRole.TextSecondary); } }
        public Color TextMuted { get { return GetColor(ThemeRole.TextMuted); } }
        public Color Accent { get { return GetColor(ThemeRole.Accent); } }
        public Color AccentInk { get { return GetColor(ThemeRole.AccentInk); } }
        public Color AccentSoft { get { return GetColor(ThemeRole.AccentSoft); } }
        public Color AccentBorder { get { return GetColor(ThemeRole.AccentBorder); } }
        public Color Danger { get { return GetColor(ThemeRole.Danger); } }
        public Color DangerSoft { get { return GetColor(ThemeRole.DangerSoft); } }
        public Color DangerInk { get { return GetColor(ThemeRole.DangerInk); } }
        public Color TaskSurface { get { return GetColor(ThemeRole.TaskSurface); } }
        public Color TaskSurfaceAlt { get { return GetColor(ThemeRole.TaskSurfaceAlt); } }
        public Color TaskHover { get { return GetColor(ThemeRole.TaskHover); } }
        public Color TaskBorder { get { return GetColor(ThemeRole.TaskBorder); } }
        public Color TimeText { get { return GetColor(ThemeRole.TimeText); } }
        public Color TimeSub { get { return GetColor(ThemeRole.TimeSub); } }
        public Color Input { get { return GetColor(ThemeRole.Input); } }
        public Color InputBorder { get { return GetColor(ThemeRole.InputBorder); } }
        public Color Popup { get { return GetColor(ThemeRole.Popup); } }
        public Color PopupBorder { get { return GetColor(ThemeRole.PopupBorder); } }
        public Color Control { get { return GetColor(ThemeRole.Control); } }
        public Color ControlBorder { get { return GetColor(ThemeRole.ControlBorder); } }
        public Color Selection { get { return GetColor(ThemeRole.Selection); } }
        public Color Track { get { return GetColor(ThemeRole.Track); } }
        public Color Overlay { get { return GetColor(ThemeRole.Overlay); } }
        public Color OverlayTint { get { return GetColor(ThemeRole.OverlayTint); } }
        public Color UrgentMarker { get { return GetColor(ThemeRole.UrgentMarker); } }
        public Color UrgentMarkerInk { get { return GetColor(ThemeRole.UrgentMarkerInk); } }
        public Color UrgentMarkerSub { get { return GetColor(ThemeRole.UrgentMarkerSub); } }
        public Color RowShadow { get { return GetColor(ThemeRole.RowShadow); } }

        public DropShadowEffect TaskShadow { get; private set; }
        public RadialGradientBrush PageGlow { get; private set; }

        private ThemePalette(ThemeMode mode, Dictionary<ThemeRole, Color> colors)
        {
            Mode = mode;
            _colors = colors;
            _brushes = new Dictionary<ThemeRole, SolidColorBrush>();
            foreach (KeyValuePair<ThemeRole, Color> pair in colors)
            {
                SolidColorBrush brush = new SolidColorBrush(pair.Value);
                brush.Freeze();
                _brushes[pair.Key] = brush;
            }
            TaskShadow = new DropShadowEffect();
            TaskShadow.Color = Mode == ThemeMode.Light
                ? Color.FromArgb(255, 58, 42, 29)
                : Colors.Black;
            TaskShadow.BlurRadius = Mode == ThemeMode.Light ? 14 : 16;
            TaskShadow.ShadowDepth = Mode == ThemeMode.Light ? 3 : 4;
            TaskShadow.Opacity = Mode == ThemeMode.Light ? 0.26 : 0.52;
            TaskShadow.Direction = 270;
            TaskShadow.Freeze();
        }

        public Color GetColor(ThemeRole role)
        {
            Color color;
            return _colors.TryGetValue(role, out color) ? color : Colors.Transparent;
        }

        public SolidColorBrush GetBrush(ThemeRole role)
        {
            SolidColorBrush brush;
            if (_brushes.TryGetValue(role, out brush)) return brush;
            return new SolidColorBrush(Colors.Transparent);
        }

        public LinearGradientBrush CardGradient()
        {
            LinearGradientBrush brush = new LinearGradientBrush();
            brush.StartPoint = new System.Windows.Point(0.1, 0);
            brush.EndPoint = new System.Windows.Point(0.9, 1);
            brush.GradientStops.Add(new GradientStop(CardTop, 0));
            brush.GradientStops.Add(new GradientStop(CardBottom, 1));
            brush.Freeze();
            return brush;
        }

        public static ThemePalette Light()
        {
            Dictionary<ThemeRole, Color> c = new Dictionary<ThemeRole, Color>();
            c[ThemeRole.CardTop] = Hex("F2EADD");
            c[ThemeRole.CardBottom] = Hex("E2D3BF");
            c[ThemeRole.CardBorder] = Hex("CBB99F");
            c[ThemeRole.TextPrimary] = Hex("2A2B26");
            c[ThemeRole.TextSecondary] = Hex("786F61");
            c[ThemeRole.TextMuted] = Hex("9B8B75");
            c[ThemeRole.Accent] = Hex("B26A45");
            c[ThemeRole.AccentInk] = Hex("FFF8F0");
            c[ThemeRole.AccentSoft] = Hex("E6C9B8");
            c[ThemeRole.AccentBorder] = Hex("D7B59F");
            c[ThemeRole.Danger] = Hex("B8472C");
            c[ThemeRole.DangerSoft] = Hex("C9795F");
            c[ThemeRole.DangerInk] = Hex("FFF6EC");
            c[ThemeRole.TaskSurface] = Hex("FCF6EC");
            c[ThemeRole.TaskSurfaceAlt] = Hex("F5EADB");
            c[ThemeRole.TaskHover] = Hex("F7EDDF");
            c[ThemeRole.TaskBorder] = Hex("E7D9C7");
            c[ThemeRole.TimeText] = Hex("3C3026");
            c[ThemeRole.TimeSub] = Hex("705F4E");
            c[ThemeRole.Input] = Hex("F6EBDD");
            c[ThemeRole.InputBorder] = Hex("E1D1BD");
            c[ThemeRole.Popup] = Hex("FAF3E8");
            c[ThemeRole.PopupBorder] = Hex("D8C6B0");
            c[ThemeRole.Control] = Hex("EDDFCE");
            c[ThemeRole.ControlBorder] = Hex("D9C7B3");
            c[ThemeRole.Selection] = Color.FromArgb(38, 178, 106, 69);
            c[ThemeRole.Track] = Hex("E1D1BD");
            c[ThemeRole.Overlay] = Color.FromArgb(122, 20, 12, 8);
            c[ThemeRole.OverlayTint] = Hex("3A2A1F");
            c[ThemeRole.UrgentMarker] = Hex("B8472C");
            c[ThemeRole.UrgentMarkerInk] = Hex("FFF6EC");
            c[ThemeRole.UrgentMarkerSub] = Hex("F8DDCC");
            c[ThemeRole.RowShadow] = Color.FromArgb(255, 58, 42, 29);
            return new ThemePalette(ThemeMode.Light, c);
        }

        public static ThemePalette Dark()
        {
            Dictionary<ThemeRole, Color> c = new Dictionary<ThemeRole, Color>();
            c[ThemeRole.CardTop] = Hex("221316");
            c[ThemeRole.CardBottom] = Hex("150A0C");
            c[ThemeRole.CardBorder] = Hex("3B282A");
            c[ThemeRole.TextPrimary] = Hex("D3C8BD");
            c[ThemeRole.TextSecondary] = Hex("908079");
            c[ThemeRole.TextMuted] = Hex("7F6E67");
            c[ThemeRole.Accent] = Hex("BFB3A7");
            c[ThemeRole.AccentInk] = Hex("261614");
            c[ThemeRole.AccentSoft] = Hex("4A3537");
            c[ThemeRole.AccentBorder] = Hex("5A4243");
            c[ThemeRole.Danger] = Hex("B8806C");
            c[ThemeRole.DangerSoft] = Hex("C08A74");
            c[ThemeRole.DangerInk] = Hex("F4E9DF");
            c[ThemeRole.TaskSurface] = Hex("2E1B1E");
            c[ThemeRole.TaskSurfaceAlt] = Hex("352022");
            c[ThemeRole.TaskHover] = Hex("382326");
            c[ThemeRole.TaskBorder] = Hex("402B2D");
            c[ThemeRole.TimeText] = Hex("FFF9F2");
            c[ThemeRole.TimeSub] = Hex("D8C5B6");
            c[ThemeRole.Input] = Hex("170B0D");
            c[ThemeRole.InputBorder] = Hex("3E2628");
            c[ThemeRole.Popup] = Hex("2A181A");
            c[ThemeRole.PopupBorder] = Hex("4A3032");
            c[ThemeRole.Control] = Hex("352022");
            c[ThemeRole.ControlBorder] = Hex("4A3032");
            c[ThemeRole.Selection] = Color.FromArgb(45, 191, 179, 167);
            c[ThemeRole.Track] = Hex("3A2527");
            c[ThemeRole.Overlay] = Color.FromArgb(179, 5, 2, 3);
            c[ThemeRole.OverlayTint] = Hex("E6D4C4");
            c[ThemeRole.UrgentMarker] = Hex("CBBFB2");
            c[ThemeRole.UrgentMarkerInk] = Hex("261614");
            c[ThemeRole.UrgentMarkerSub] = Hex("4F332D");
            c[ThemeRole.RowShadow] = Color.FromArgb(255, 0, 0, 0);
            return new ThemePalette(ThemeMode.Dark, c);
        }

        private static Color Hex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString("#FF" + hex);
        }
    }

    internal static class ThemeManager
    {
        private static ThemePalette _palette = ThemePalette.Light();
        private static readonly Regex ColorRegex = new Regex("#[0-9A-Fa-f]{6,8}", RegexOptions.Compiled);
        private static readonly Dictionary<string, ThemeRole> LegacyRoles =
            new Dictionary<string, ThemeRole>(StringComparer.OrdinalIgnoreCase);

        public static ThemeMode Mode { get; private set; }
        public static ThemePalette Palette { get { return _palette; } }
        public static event Action Changed;

        static ThemeManager()
        {
            Mode = ThemeMode.Light;
            BuildLegacyMap();
        }

        public static void Initialize(string theme)
        {
            Mode = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
                ? ThemeMode.Dark : ThemeMode.Light;
            _palette = Mode == ThemeMode.Dark ? ThemePalette.Dark() : ThemePalette.Light();
        }

        public static void SetMode(ThemeMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            _palette = mode == ThemeMode.Dark ? ThemePalette.Dark() : ThemePalette.Light();
            Action handler = Changed;
            if (handler != null) handler();
        }

        public static string ModeName { get { return Mode == ThemeMode.Dark ? "dark" : "light"; } }
        public static bool IsLight { get { return Mode == ThemeMode.Light; } }

        public static string TransformXaml(string xaml)
        {
            if (string.IsNullOrEmpty(xaml)) return xaml;
            return ColorRegex.Replace(xaml, delegate(Match m)
            {
                Color mapped = MapHex(m.Value, Color.FromArgb(255, 255, 255, 255));
                return ToHex(mapped);
            });
        }

        public static Color MapHex(string hex, Color fallback)
        {
            Color color;
            try { color = (Color)ColorConverter.ConvertFromString(hex); }
            catch { return fallback; }
            return MapColor(color);
        }

        public static Color MapColor(Color color)
        {
            if (color.A == 0) return Colors.Transparent;

            string key = color.A.ToString("X2", CultureInfo.InvariantCulture) +
                         color.R.ToString("X2", CultureInfo.InvariantCulture) +
                         color.G.ToString("X2", CultureInfo.InvariantCulture) +
                         color.B.ToString("X2", CultureInfo.InvariantCulture);
            ThemeRole role;
            if (LegacyRoles.TryGetValue(key, out role))
            {
                Color mapped = _palette.GetColor(role);
                bool keepAlpha = role == ThemeRole.CardBorder || role == ThemeRole.AccentBorder ||
                                 role == ThemeRole.AccentSoft || role == ThemeRole.DangerSoft ||
                                 role == ThemeRole.Overlay;
                if (color.A < 250 && keepAlpha)
                    mapped = Color.FromArgb(color.A, mapped.R, mapped.G, mapped.B);
                return mapped;
            }

            double a = color.A / 255.0;
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double l = (max + min) / 2.0;
            double s = max == min ? 0 : (l > 0.5 ? (max - min) / (2 - max - min) : (max - min) / (max + min));
            double h = Hue(r, g, b, max, min);
            bool whiteish = min > 0.66;
            double spread = max - min;

            if (color.A < 250)
            {
                if (whiteish)
                {
                    Color tint = _palette.OverlayTint;
                    return Color.FromArgb(color.A, tint.R, tint.G, tint.B);
                }
                if (h >= 230 && h <= 290) return WithAlpha(_palette.Accent, color.A);
                if (h >= 330 || h <= 20) return WithAlpha(_palette.Danger, color.A);
                return WithAlpha(_palette.OverlayTint, color.A);
            }

            // Near-white lavender text should become ink in light mode and bone
            // in dark mode, not a saturated accent. HSL saturation is misleading
            // for these very light colours, so use absolute channel spread.
            if (spread < 0.10 && l > 0.60) return _palette.TextPrimary;
            if (spread < 0.15 && l > 0.40) return _palette.TextSecondary;
            if (s < 0.28 && l > 0.62) return _palette.TextPrimary;
            if (s < 0.22 && l > 0.44) return _palette.TextSecondary;
            if (h >= 235 && h <= 290 && s > 0.12)
                return l > 0.7 ? _palette.AccentSoft : _palette.Accent;
            if ((h >= 330 || h <= 20) && s > 0.2)
                return l > 0.72 ? _palette.DangerSoft : _palette.Danger;
            if (s < 0.18)
            {
                if (l > 0.68) return _palette.TextPrimary;
                if (l > 0.42) return _palette.TextSecondary;
                if (l > 0.26) return _palette.TextMuted;
                if (l > 0.13) return _palette.TaskSurfaceAlt;
                return _palette.CardBottom;
            }
            if (l > 0.70) return _palette.TextPrimary;
            if (l > 0.48) return _palette.TextSecondary;
            if (l > 0.30) return _palette.TextMuted;
            if (l > 0.14) return _palette.TaskSurfaceAlt;
            return _palette.CardBottom;
        }

        public static SolidColorBrush Brush(ThemeRole role)
        {
            return _palette.GetBrush(role);
        }

        public static SolidColorBrush Brush(Color color)
        {
            SolidColorBrush brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush BrushFromHex(string hex)
        {
            return Brush(MapHex(hex, _palette.TextPrimary));
        }

        public static Color WithAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static string ToHex(Color color)
        {
            return "#" + color.A.ToString("X2", CultureInfo.InvariantCulture) +
                   color.R.ToString("X2", CultureInfo.InvariantCulture) +
                   color.G.ToString("X2", CultureInfo.InvariantCulture) +
                   color.B.ToString("X2", CultureInfo.InvariantCulture);
        }

        private static double Hue(double r, double g, double b, double max, double min)
        {
            double d = max - min;
            if (d <= 0.00001) return 0;
            double h;
            if (max == r) h = ((g - b) / d) % 6;
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
            return h;
        }

        private static void BuildLegacyMap()
        {
            // Card shell and frame.
            Add("D9141620", ThemeRole.CardTop);
            Add("EE171922", ThemeRole.CardTop);
            Add("E20D0F17", ThemeRole.CardBottom);
            Add("F20D0F17", ThemeRole.CardBottom);
            Add("EA090B11", ThemeRole.CardBottom);
            Add("F5090B11", ThemeRole.CardBottom);
            Add("4DFFFFFF", ThemeRole.CardBorder);
            Add("22FFFFFF", ThemeRole.CardBorder);
            Add("1AFFFFFF", ThemeRole.CardBorder);

            // Primary typography.
            Add("FFF7F4FB", ThemeRole.TextPrimary);
            Add("FFF8F5FF", ThemeRole.TextPrimary);
            Add("FFF5F0FB", ThemeRole.TextPrimary);
            Add("FFF4F1FA", ThemeRole.TextPrimary);
            Add("FFDAD4E4", ThemeRole.TextPrimary);
            Add("FFDCD7E8", ThemeRole.TextSecondary);
            Add("FFDCD6E8", ThemeRole.TextSecondary);
            Add("FFD8D2E4", ThemeRole.TextSecondary);
            Add("FFC9C2D7", ThemeRole.TextSecondary);
            Add("FFA39DAF", ThemeRole.TextMuted);
            Add("FF9A93A8", ThemeRole.TextMuted);
            Add("FF8F889C", ThemeRole.TextMuted);
            Add("FF8B8498", ThemeRole.TextMuted);
            Add("FF8E889B", ThemeRole.TextMuted);
            Add("FFB2AABF", ThemeRole.TextMuted);
            Add("FF827B8E", ThemeRole.TextMuted);
            Add("FF7F798B", ThemeRole.TextMuted);
            Add("FF7A7386", ThemeRole.TextMuted);
            Add("FF817A8D", ThemeRole.TextMuted);
            Add("FFCFC8DA", ThemeRole.TextSecondary);
            Add("FFF0EBF7", ThemeRole.TextPrimary);
            Add("FFE5E0EE", ThemeRole.TextPrimary);
            Add("FFB7B1C3", ThemeRole.TextMuted);

            // Accent family.
            Add("FFBCA9FF", ThemeRole.Accent);
            Add("FFB7A5FF", ThemeRole.Accent);
            Add("FFB9A8FF", ThemeRole.Accent);
            Add("FFCEC2FF", ThemeRole.Accent);
            Add("FF9D86FF", ThemeRole.Accent);
            Add("FFA48EFF", ThemeRole.Accent);
            Add("E0BCA9FF", ThemeRole.Accent);
            Add("D9BCA9FF", ThemeRole.Accent);
            Add("2CBCA9FF", ThemeRole.Accent);
            Add("66BCA9FF", ThemeRole.AccentBorder);
            Add("56BCA9FF", ThemeRole.AccentBorder);

            // Urgent/danger family.
            Add("FFFF9CA5", ThemeRole.DangerSoft);
            Add("FFFF9AA3", ThemeRole.DangerSoft);
            Add("FFFFA0A8", ThemeRole.DangerSoft);
            Add("FFFF6E7E", ThemeRole.Danger);
            Add("FFFF7D88", ThemeRole.Danger);
            Add("FFFF6573", ThemeRole.Danger);
            Add("FFFF8A95", ThemeRole.Danger);
            Add("FFFF8792", ThemeRole.Danger);
            Add("35FF8995", ThemeRole.DangerSoft);
            Add("FF19141F", ThemeRole.AccentInk);

            // Common surfaces and controls.
            Add("FF090B11", ThemeRole.CardBottom);
            Add("FF0A0C12", ThemeRole.CardBottom);
            Add("FF0D0F17", ThemeRole.TaskSurfaceAlt);
            Add("FF111318", ThemeRole.TaskSurfaceAlt);
            Add("FF14161F", ThemeRole.TaskSurface);
            Add("FF171922", ThemeRole.TaskSurface);
            Add("FF191A23", ThemeRole.TaskSurface);
            Add("FF1A1B24", ThemeRole.Popup);
            Add("FF1A1C26", ThemeRole.TaskSurfaceAlt);
            Add("FF1B1D26", ThemeRole.TaskSurface);
            Add("FF20212A", ThemeRole.Popup);
            Add("FF20222D", ThemeRole.TaskSurfaceAlt);
            Add("FF23242D", ThemeRole.Input);
            Add("FF242630", ThemeRole.Input);
            Add("FF2A2C36", ThemeRole.InputBorder);

            // Opaque-ish popup and editor surfaces.
            Add("F4171821", ThemeRole.Popup);
            Add("F5191A22", ThemeRole.Popup);
            Add("FA191A23", ThemeRole.Popup);
            Add("FB191A23", ThemeRole.Popup);
            Add("AA8F79FF", ThemeRole.AccentSoft);
            Add("FFE1DCEC", ThemeRole.TextPrimary);
            Add("FFE3DDEC", ThemeRole.TextPrimary);
            Add("FFEADFFF", ThemeRole.TextPrimary);
            Add("FFB8B1C6", ThemeRole.TextMuted);
            Add("FFB9B2C4", ThemeRole.TextMuted);
            Add("FFBBB4C6", ThemeRole.TextMuted);
            Add("FFABA4B8", ThemeRole.TextMuted);
            Add("FFA9A2B4", ThemeRole.TextMuted);
            Add("FF898396", ThemeRole.TextMuted);
            Add("FF888195", ThemeRole.TextMuted);
            Add("FFE35F6D", ThemeRole.Danger);
            Add("FFD1C5FF", ThemeRole.Accent);
            Add("FFCFC1FF", ThemeRole.Accent);
            Add("FFCFC8DA", ThemeRole.TextSecondary);

            // Dark overlay and common white-alpha borders.
            Add("8805060B", ThemeRole.Overlay);
            Add("AA05060B", ThemeRole.Overlay);
            Add("9905060B", ThemeRole.Overlay);
        }

        private static void Add(string argb, ThemeRole role)
        {
            LegacyRoles[argb] = role;
        }
    }
}
