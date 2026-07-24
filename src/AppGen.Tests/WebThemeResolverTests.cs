using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class WebThemeResolverTests
{
    [Fact]
    public void Resolve_uses_cookbook_tokens_for_mvc_css()
    {
        var spec = new SolutionSpec
        {
            ApplicationName = "CookbookWeb",
            RootNamespace = "CookbookWeb",
            UiTargets = UiTarget.MvcWeb,
            Targets = new ApplicationTargets
            {
                Mobile = new MobileTargetSpec
                {
                    Theme = new MobileThemeSpec { Preset = "cookbook" }
                }
            }
        };

        var theme = WebThemeResolver.Resolve(spec);

        Assert.Equal("cookbook", theme.Preset);
        Assert.Equal("#1A3D2E", theme.Sidebar);
        Assert.Equal("#C9A227", theme.Accent);
        Assert.Equal("#F5F0E8", theme.Background);
        Assert.Equal("cormorantGaramond", theme.HeadingFont);
    }

    [Fact]
    public async Task Generated_mvc_site_css_contains_cookbook_variables()
    {
        var renderer = new TemplateRenderer();
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenWebTheme_" + Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "CookbookMvc");
        try
        {
            var spec = new SolutionSpec
            {
                ApplicationName = "CookbookMvc",
                RootNamespace = "CookbookMvc",
                Database = DatabaseProvider.SqlServer,
                UiTargets = UiTarget.MvcWeb,
                Setup = NamingHelper.DefaultSetup(DatabaseProvider.SqlServer),
                Entities = [new EntitySpec { Name = "Widget" }],
                Targets = new ApplicationTargets
                {
                    Mobile = new MobileTargetSpec
                    {
                        Theme = new MobileThemeSpec { Preset = "cookbook" }
                    }
                }
            };

            await new SolutionGenerator(renderer).GenerateAsync(spec, outputDir);

            var siteCss = await File.ReadAllTextAsync(
                Path.Combine(outputDir, "src", "CookbookMvc.MVC", "wwwroot", "css", "site.css"));

            Assert.Contains("--theme-sidebar: #1A3D2E", siteCss, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--theme-accent: #C9A227", siteCss, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".entity-card", siteCss);
            Assert.Contains("Cormorant Garamond", siteCss);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
