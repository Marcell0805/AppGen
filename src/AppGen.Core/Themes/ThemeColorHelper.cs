namespace AppGen.Core.Themes;

public static class ThemeColorHelper
{
    public static string ToDartColor(string? hex, string fallback)
    {
        var normalized = NormalizeHexDigits(hex);
        if (normalized is null)
            return fallback;

        return normalized.Length == 6
            ? $"0xFF{normalized}"
            : $"0x{normalized}";
    }

    public static string DartToCssHex(string dartColor)
    {
        var normalized = NormalizeHexDigits(dartColor);
        return normalized is { Length: 6 } ? $"#{normalized}" : "#000000";
    }

    public static string DarkenHex(string? hex, string fallback, double factor = 0.85)
    {
        var normalized = NormalizeHexDigits(hex);
        if (normalized is null || normalized.Length != 6)
            return fallback;

        try
        {
            var r = (int)(Convert.ToInt32(normalized[..2], 16) * factor);
            var g = (int)(Convert.ToInt32(normalized.Substring(2, 2), 16) * factor);
            var b = (int)(Convert.ToInt32(normalized.Substring(4, 2), 16) * factor);
            return $"0xFF{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return fallback;
        }
    }

    public static string ResolveOnAccent(string accentDart, string? explicitOnAccent = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitOnAccent))
        {
            var explicitColor = ToDartColor(explicitOnAccent, string.Empty);
            if (!string.IsNullOrEmpty(explicitColor))
                return explicitColor;
        }

        return PickHigherContrastForeground(accentDart);
    }

    public static string ResolveOnSidebar(string sidebarDart, string? explicitOnSidebar = null) =>
        !string.IsNullOrWhiteSpace(explicitOnSidebar)
            ? ToDartColor(explicitOnSidebar, PickHigherContrastForeground(sidebarDart))
            : PickHigherContrastForeground(sidebarDart);

    public static double ContrastRatio(string foregroundDart, string backgroundDart)
    {
        var fg = RelativeLuminance(foregroundDart);
        var bg = RelativeLuminance(backgroundDart);
        var lighter = Math.Max(fg, bg);
        var darker = Math.Min(fg, bg);
        return (lighter + 0.05) / (darker + 0.05);
    }

    public static double RelativeLuminance(string dartColor)
    {
        var (r, g, b) = ParseRgb(dartColor);
        static double Channel(int c)
        {
            var s = c / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }

    private static string PickHigherContrastForeground(string backgroundDart)
    {
        const string white = "0xFFFFFFFF";
        const string dark = "0xFF1B1B1B";
        return ContrastRatio(white, backgroundDart) >= ContrastRatio(dark, backgroundDart) ? white : dark;
    }

    private static string? NormalizeHexDigits(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return null;

        var normalized = hex.Trim().TrimStart('#');
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[2..];

        if (normalized.Length is not (6 or 8))
            return null;

        if (normalized.Length == 8)
            normalized = normalized[2..];

        return normalized.ToUpperInvariant();
    }

    private static (int R, int G, int B) ParseRgb(string dartColor)
    {
        var normalized = NormalizeHexDigits(dartColor);
        if (normalized is null || normalized.Length != 6)
            return (0, 0, 0);

        return (
            Convert.ToInt32(normalized[..2], 16),
            Convert.ToInt32(normalized.Substring(2, 2), 16),
            Convert.ToInt32(normalized.Substring(4, 2), 16));
    }
}
