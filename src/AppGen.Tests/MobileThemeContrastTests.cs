using AppGen.Core.Models;
using AppGen.Core.Themes;
using AppGen.Engine;

namespace AppGen.Tests;

public class MobileThemeContrastTests
{
    public static IEnumerable<object[]> PresetIds =>
        MobileThemeCatalog.PresetIds.Select(id => new object[] { id });

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Accent_and_onAccent_meet_wcag_aa(string presetId)
    {
        var tokens = Resolve(presetId);
        var ratio = ThemeColorHelper.ContrastRatio(tokens.OnAccent, tokens.Accent);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' accent/onAccent contrast {ratio:F2} is below 4.5:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Text_and_background_meet_readable_contrast(string presetId)
    {
        var theme = MobileThemeCatalog.Get(presetId);
        var ratio = ThemeColorHelper.ContrastRatio(theme.Text, theme.Background);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' text/background contrast {ratio:F2} is below 4.5:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void TextMuted_and_background_meet_readable_contrast(string presetId)
    {
        var theme = MobileThemeCatalog.Get(presetId);
        var ratio = ThemeColorHelper.ContrastRatio(theme.TextMuted, theme.Background);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' textMuted/background contrast {ratio:F2} is below 4.5:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Border_and_background_have_visible_separation(string presetId)
    {
        var theme = MobileThemeCatalog.Get(presetId);
        var ratio = ThemeColorHelper.BorderSeparationRatio(theme.Border, theme.Background);
        Assert.True(ratio >= 1.25, $"Preset '{presetId}' border/background separation {ratio:F2} is below 1.25:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Selected_nav_foreground_is_visible_on_sidebar(string presetId)
    {
        var tokens = Resolve(presetId);
        // Selected tile uses accent on sidebar (or selected chip). Accent vs sidebar OR accent vs selected bg must read.
        var onSidebar = ThemeColorHelper.ContrastRatio(tokens.Accent, tokens.Sidebar);
        var onSelectedBg = ThemeColorHelper.ContrastRatio(tokens.Accent, tokens.SidebarSelectedBackground);
        Assert.True(
            onSidebar >= 3.0 || onSelectedBg >= 3.0,
            $"Preset '{presetId}' selected nav contrast sidebar={onSidebar:F2}, selectedBg={onSelectedBg:F2} (need ≥3 on one)");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void AccentMuted_and_onAccentMuted_meet_wcag_aa(string presetId)
    {
        var tokens = Resolve(presetId);
        var ratio = ThemeColorHelper.ContrastRatio(tokens.OnAccentMuted, tokens.AccentMuted);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' accentMuted/onAccentMuted contrast {ratio:F2} is below 4.5:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Link_color_meets_aa_on_background(string presetId)
    {
        var tokens = Resolve(presetId);
        var ratio = ThemeColorHelper.ContrastRatio(tokens.Link, tokens.Background);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' link/background contrast {ratio:F2} is below 4.5:1");
    }

    [Theory]
    [MemberData(nameof(PresetIds))]
    public void Portal_onPrimary_meets_aa_on_primary(string presetId)
    {
        var portal = PortalThemeCss.Resolve(presetId);
        var primaryDart = ThemeColorHelper.ToDartColor(portal.Primary, "0xFF000000");
        var onPrimaryDart = ThemeColorHelper.ToDartColor(portal.OnPrimary, "0xFFFFFFFF");
        var ratio = ThemeColorHelper.ContrastRatio(onPrimaryDart, primaryDart);
        Assert.True(ratio >= 4.5, $"Preset '{presetId}' portal on-primary/primary contrast {ratio:F2} is below 4.5:1");
    }

    [Fact]
    public void DartToCssHex_preserves_translucent_alpha()
    {
        Assert.Equal("#FFFFFF33", ThemeColorHelper.DartToCssHex("0x33FFFFFF"));
        Assert.Equal("#FFFFFF", ThemeColorHelper.DartToCssHex("0xFFFFFFFF"));
        Assert.Equal("#C7D2FE", ThemeColorHelper.DartToCssHex("0xFFC7D2FE"));
    }

    private static FlutterThemeTokens Resolve(string presetId) =>
        FlutterThemeResolver.Resolve(
            new SolutionSpec
            {
                ApplicationName = "ContrastTest",
                RootNamespace = "ContrastTest",
                Targets = new ApplicationTargets
                {
                    Mobile = new MobileTargetSpec
                    {
                        Theme = new MobileThemeSpec { Preset = presetId }
                    }
                }
            },
            new MobileTargetSpec { Theme = new MobileThemeSpec { Preset = presetId } });
}
