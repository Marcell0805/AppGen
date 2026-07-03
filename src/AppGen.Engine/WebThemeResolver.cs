using AppGen.Core.Models;
using AppGen.Core.Themes;

namespace AppGen.Engine;

public sealed record WebThemeTokens(
    string Preset,
    string Sidebar,
    string SidebarAccent,
    string Accent,
    string OnAccent,
    string OnSidebar,
    string Background,
    string Surface,
    string Border,
    string Text,
    string TextMuted,
    string Highlight,
    string Error,
    double CornerRadius,
    bool IsDark,
    string HeadingFont);

public static class WebThemeResolver
{
    public static WebThemeTokens Resolve(SolutionSpec spec)
    {
        var mobile = spec.Targets?.Mobile ?? new MobileTargetSpec();
        var tokens = FlutterThemeResolver.Resolve(spec, mobile);
        return new WebThemeTokens(
            Preset: tokens.Preset,
            Sidebar: ThemeColorHelper.DartToCssHex(tokens.Sidebar),
            SidebarAccent: ThemeColorHelper.DartToCssHex(tokens.SidebarAccent),
            Accent: ThemeColorHelper.DartToCssHex(tokens.Accent),
            OnAccent: ThemeColorHelper.DartToCssHex(tokens.OnAccent),
            OnSidebar: ThemeColorHelper.DartToCssHex(tokens.OnSidebar),
            Background: ThemeColorHelper.DartToCssHex(tokens.Background),
            Surface: ThemeColorHelper.DartToCssHex(tokens.Surface),
            Border: ThemeColorHelper.DartToCssHex(tokens.Border),
            Text: ThemeColorHelper.DartToCssHex(tokens.Text),
            TextMuted: ThemeColorHelper.DartToCssHex(tokens.TextMuted),
            Highlight: ThemeColorHelper.DartToCssHex(tokens.Highlight),
            Error: ThemeColorHelper.DartToCssHex(tokens.Error),
            CornerRadius: tokens.CornerRadius,
            IsDark: tokens.IsDark,
            HeadingFont: tokens.HeadingFont);
    }
}
