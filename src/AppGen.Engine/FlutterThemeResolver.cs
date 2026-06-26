using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed record FlutterThemeTokens(
    string Preset,
    string Sidebar,
    string SidebarAccent,
    string Accent,
    string AccentMuted,
    string Background,
    string Surface,
    string Border,
    string Text,
    string TextMuted,
    string Highlight,
    string Success,
    string Error);

public static class FlutterThemeResolver
{
    public static FlutterThemeTokens Resolve(SolutionSpec spec, MobileTargetSpec mobile)
    {
        var preset = ResolvePreset(spec, mobile);
        var tokens = preset switch
        {
            "portal" => PortalPreset(spec, mobile),
            "cookbook" => CookbookPreset(mobile),
            _ => AppGenPreset(mobile)
        };

        return tokens with { Preset = preset };
    }

    private static string ResolvePreset(SolutionSpec spec, MobileTargetSpec mobile)
    {
        var requested = mobile.Theme?.Preset?.Trim();
        if (!string.IsNullOrWhiteSpace(requested))
            return requested.ToLowerInvariant();

        if (spec.Targets?.Documentation.Enabled == true && spec.Portal is not null)
            return "portal";

        return "appgen";
    }

    private static FlutterThemeTokens AppGenPreset(MobileTargetSpec mobile) => new(
        Preset: "appgen",
        Sidebar: ToDartColor(mobile.Theme?.PrimaryColor, "0xFF0F172A"),
        SidebarAccent: "0xFF1E293B",
        Accent: ToDartColor(mobile.Theme?.AccentColor, "0xFF3B82F6"),
        AccentMuted: "0xFF60A5FA",
        Background: ToDartColor(mobile.Theme?.BackgroundColor, "0xFFF8FAFC"),
        Surface: "0xFFFFFFFF",
        Border: "0xFFE2E8F0",
        Text: "0xFF0F172A",
        TextMuted: "0xFF64748B",
        Highlight: ToDartColor(mobile.Theme?.HighlightColor, "0xFF60A5FA"),
        Success: "0xFF16A34A",
        Error: "0xFFDC2626");

    private static FlutterThemeTokens PortalPreset(SolutionSpec spec, MobileTargetSpec mobile)
    {
        var theme = spec.Portal?.Settings.Theme;
        return new FlutterThemeTokens(
            Preset: "portal",
            Sidebar: ToDartColor(mobile.Theme?.PrimaryColor ?? theme?.PrimaryColor, "0xFF1B3A5C"),
            SidebarAccent: DarkenHex(mobile.Theme?.PrimaryColor ?? theme?.PrimaryColor, "0xFF152E47"),
            Accent: ToDartColor(mobile.Theme?.AccentColor ?? theme?.AccentColor, "0xFF2E75B6"),
            AccentMuted: "0xFF5A9BD5",
            Background: ToDartColor(mobile.Theme?.BackgroundColor ?? theme?.BackgroundColor, "0xFFE8F1F8"),
            Surface: "0xFFFFFFFF",
            Border: "0xFFD4E3F0",
            Text: "0xFF1B3A5C",
            TextMuted: "0xFF5A6F82",
            Highlight: ToDartColor(mobile.Theme?.HighlightColor ?? theme?.HighlightColor, "0xFFF28C28"),
            Success: "0xFF16A34A",
            Error: "0xFFDC2626");
    }

    private static FlutterThemeTokens CookbookPreset(MobileTargetSpec mobile) => new(
        Preset: "cookbook",
        Sidebar: ToDartColor(mobile.Theme?.PrimaryColor, "0xFF1B3A5C"),
        SidebarAccent: "0xFF152E47",
        Accent: ToDartColor(mobile.Theme?.AccentColor, "0xFFC9A227"),
        AccentMuted: "0xFFE0BE5A",
        Background: ToDartColor(mobile.Theme?.BackgroundColor, "0xFFF5F0E6"),
        Surface: "0xFFFFFFFF",
        Border: "0xFFE8DFD0",
        Text: "0xFF1B3A5C",
        TextMuted: "0xFF6B5E4E",
        Highlight: ToDartColor(mobile.Theme?.HighlightColor, "0xFFF28C28"),
        Success: "0xFF16A34A",
        Error: "0xFFDC2626");

    private static string ToDartColor(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        var normalized = hex.Trim().TrimStart('#');
        if (normalized.Length is not (6 or 8))
            return fallback;

        if (normalized.Length == 6)
            return $"0xFF{normalized.ToUpperInvariant()}";

        return $"0x{normalized.ToUpperInvariant()}";
    }

    private static string DarkenHex(string? hex, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;

        var normalized = hex.Trim().TrimStart('#');
        if (normalized.Length != 6)
            return fallback;

        try
        {
            var r = Convert.ToInt32(normalized[..2], 16);
            var g = Convert.ToInt32(normalized.Substring(2, 2), 16);
            var b = Convert.ToInt32(normalized.Substring(4, 2), 16);
            r = (int)(r * 0.85);
            g = (int)(g * 0.85);
            b = (int)(b * 0.85);
            return $"0xFF{r:X2}{g:X2}{b:X2}";
        }
        catch
        {
            return fallback;
        }
    }
}
