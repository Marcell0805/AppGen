using AppGen.Core.Models;
using AppGen.Core.Themes;

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
    string Error,
    string OnAccent,
    string OnSidebar,
    string SidebarSelectedBackground,
    string HeadingFont,
    string BodyFont,
    string ButtonFont,
    double CornerRadius,
    bool IsDark,
    double MinButtonHeight,
    bool UsesDashboardNav);

public static class FlutterThemeResolver
{
    public static FlutterThemeTokens Resolve(SolutionSpec spec, MobileTargetSpec mobile)
    {
        var preset = ResolvePreset(spec, mobile);
        var definition = MobileThemeCatalog.Get(preset);

        var sidebar = ApplyOverride(mobile.Theme?.PrimaryColor, definition.Sidebar);
        var sidebarAccent = definition.SidebarAccent;
        var accent = ApplyOverride(mobile.Theme?.AccentColor, definition.Accent);
        var background = ApplyOverride(mobile.Theme?.BackgroundColor, definition.Background);
        var highlight = ApplyOverride(mobile.Theme?.HighlightColor, definition.Highlight);

        if (preset == "portal")
        {
            var portalTheme = spec.Portal?.Settings.Theme;
            var primary = mobile.Theme?.PrimaryColor ?? portalTheme?.PrimaryColor;
            sidebar = ApplyOverride(primary, definition.Sidebar);
            accent = ApplyOverride(mobile.Theme?.AccentColor ?? portalTheme?.AccentColor, definition.Accent);
            background = ApplyOverride(mobile.Theme?.BackgroundColor ?? portalTheme?.BackgroundColor, definition.Background);
            highlight = ApplyOverride(mobile.Theme?.HighlightColor ?? portalTheme?.HighlightColor, definition.Highlight);
            sidebarAccent = ThemeColorHelper.DarkenHex(primary, definition.SidebarAccent);
        }

        var onAccent = ThemeColorHelper.ResolveOnAccent(accent, definition.OnAccent);
        var onSidebar = ThemeColorHelper.ResolveOnSidebar(sidebar, definition.OnSidebar);
        var sidebarSelected = definition.SidebarSelectedBackground
            ?? $"0x33{accent.Replace("0xFF", "", StringComparison.OrdinalIgnoreCase)}";

        return new FlutterThemeTokens(
            Preset: preset,
            Sidebar: sidebar,
            SidebarAccent: sidebarAccent,
            Accent: accent,
            AccentMuted: definition.AccentMuted,
            Background: background,
            Surface: definition.Surface,
            Border: definition.Border,
            Text: definition.Text,
            TextMuted: definition.TextMuted,
            Highlight: highlight,
            Success: definition.Success,
            Error: definition.Error,
            OnAccent: onAccent,
            OnSidebar: onSidebar,
            SidebarSelectedBackground: sidebarSelected,
            HeadingFont: definition.HeadingFont,
            BodyFont: definition.BodyFont,
            ButtonFont: definition.ButtonFont,
            CornerRadius: definition.CornerRadius,
            IsDark: definition.IsDark,
            MinButtonHeight: definition.MinButtonHeight,
            UsesDashboardNav: definition.UsesDashboardNav || preset == "dashboard");
    }

    private static string ResolvePreset(SolutionSpec spec, MobileTargetSpec mobile)
    {
        var requested = mobile.Theme?.Preset?.Trim();
        if (!string.IsNullOrWhiteSpace(requested))
            return MobileThemeCatalog.NormalizePreset(requested);

        if (spec.Targets?.Documentation.Enabled == true && spec.Portal is not null)
            return "portal";

        return "appgen";
    }

    private static string ApplyOverride(string? hex, string fallback) =>
        string.IsNullOrWhiteSpace(hex) ? fallback : ThemeColorHelper.ToDartColor(hex, fallback);
}
