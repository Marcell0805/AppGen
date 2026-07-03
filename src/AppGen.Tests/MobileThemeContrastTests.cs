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
        var theme = MobileThemeCatalog.Get(presetId);
        var tokens = FlutterThemeResolver.Resolve(
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
}
