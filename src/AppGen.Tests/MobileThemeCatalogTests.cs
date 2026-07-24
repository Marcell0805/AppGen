using AppGen.Core.Themes;

namespace AppGen.Tests;

public class MobileThemeCatalogTests
{
    [Fact]
    public void Catalog_has_sixteen_presets_with_unique_ids()
    {
        var all = MobileThemeCatalog.GetAll();
        Assert.Equal(16, all.Count);
        Assert.Equal(all.Count, all.Select(d => d.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Theory]
    [InlineData("corporate")]
    [InlineData("cookbook")]
    [InlineData("dark-pro")]
    [InlineData("terminal")]
    [InlineData("dashboard")]
    public void NormalizePreset_returns_known_ids(string preset)
    {
        Assert.Equal(preset, MobileThemeCatalog.NormalizePreset(preset));
    }

    [Fact]
    public void NormalizePreset_unknown_falls_back_to_appgen()
    {
        Assert.Equal("appgen", MobileThemeCatalog.NormalizePreset("not-a-theme"));
    }

    [Fact]
    public void GetSelectable_hides_portal_when_documentation_disabled()
    {
        var selectable = MobileThemeCatalog.GetSelectable(documentationEnabled: false);
        Assert.DoesNotContain(selectable, t => t.Id == "portal");
        Assert.Contains(selectable, t => t.Id == "appgen");
    }

    [Fact]
    public void Cookbook_uses_dark_on_accent_for_gold_button()
    {
        var cookbook = MobileThemeCatalog.Get("cookbook");
        var onAccent = ThemeColorHelper.ResolveOnAccent(cookbook.Accent, cookbook.OnAccent);
        Assert.Equal("0xFF1A3D2E", onAccent);
        Assert.Equal("0xFF1A3D2E", cookbook.Sidebar);
        Assert.Equal("0xFFF5F0E8", cookbook.Background);
    }

    [Fact]
    public void ToPortalThemeSettings_maps_cookbook_to_documentation_colors()
    {
        var theme = MobileThemeCatalog.ToPortalThemeSettings("cookbook");
        Assert.Equal("#1A3D2E", theme.PrimaryColor);
        Assert.Equal("#C9A227", theme.AccentColor);
        Assert.Equal("#C45C5C", theme.HighlightColor);
        Assert.Equal("#F5F0E8", theme.BackgroundColor);
    }

    [Fact]
    public void PortalThemeCss_uses_readable_text_on_dark_presets()
    {
        var theme = PortalThemeCss.Resolve("dark-pro");
        var css = PortalThemeCss.ToCss(theme);

        Assert.True(theme.IsDark);
        Assert.Contains("--text: #F8FAFC", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--heading: #F8FAFC", css, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--muted: #94A3B8", css, StringComparison.OrdinalIgnoreCase);
    }
}
