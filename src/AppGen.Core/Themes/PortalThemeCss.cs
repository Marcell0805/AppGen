using AppGen.Core.Models;

namespace AppGen.Core.Themes;

public sealed record PortalThemeCssVariables(
    string Primary,
    string Accent,
    string Highlight,
    string Background,
    string Surface,
    string Text,
    string Muted,
    string Border,
    string Heading,
    string OnPrimary,
    bool IsDark);

public static class PortalThemeCss
{
    public static PortalThemeCssVariables Resolve(string? presetId, PortalThemeSettings? portalOverrides = null)
    {
        var preset = MobileThemeCatalog.NormalizePreset(presetId);
        var definition = MobileThemeCatalog.Get(preset);

        var sidebar = definition.Sidebar;
        var accent = definition.Accent;
        var highlight = definition.Highlight;
        var background = definition.Background;

        if (preset == "portal" && portalOverrides is not null)
        {
            sidebar = ThemeColorHelper.ToDartColor(portalOverrides.PrimaryColor, sidebar);
            accent = ThemeColorHelper.ToDartColor(portalOverrides.AccentColor, accent);
            highlight = ThemeColorHelper.ToDartColor(portalOverrides.HighlightColor, highlight);
            background = ThemeColorHelper.ToDartColor(portalOverrides.BackgroundColor, background);
        }

        var text = ThemeColorHelper.DartToCssHex(definition.Text);
        var muted = ThemeColorHelper.DartToCssHex(definition.TextMuted);
        // Always use body text for headings — light sidebars (e.g. ios) are unreadable as heading color.
        var heading = text;

        return new PortalThemeCssVariables(
            Primary: ThemeColorHelper.DartToCssHex(sidebar),
            Accent: ThemeColorHelper.DartToCssHex(accent),
            Highlight: ThemeColorHelper.DartToCssHex(highlight),
            Background: ThemeColorHelper.DartToCssHex(background),
            Surface: ThemeColorHelper.DartToCssHex(definition.Surface),
            Text: text,
            Muted: muted,
            Border: ThemeColorHelper.DartToCssHex(definition.Border),
            Heading: heading,
            OnPrimary: ThemeColorHelper.DartToCssHex(
                ThemeColorHelper.ResolveOnSidebar(sidebar, definition.OnSidebar)),
            IsDark: definition.IsDark);
    }

    public static string ToCss(PortalThemeCssVariables theme) =>
        ":root {\n" +
        $"  --navy: {theme.Primary};\n" +
        $"  --blue: {theme.Accent};\n" +
        $"  --orange: {theme.Highlight};\n" +
        $"  --bg: {theme.Background};\n" +
        $"  --surface: {theme.Surface};\n" +
        $"  --text: {theme.Text};\n" +
        $"  --muted: {theme.Muted};\n" +
        $"  --border: {theme.Border};\n" +
        $"  --heading: {theme.Heading};\n" +
        $"  --on-primary: {theme.OnPrimary};\n" +
        "}\n";
}
