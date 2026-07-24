using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Core.Themes;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class ThemeEmitterTests
{
    [Fact]
    public async Task ApplyAsync_updates_theme_files_and_leaves_unrelated_and_branding_alone()
    {
        var renderer = new TemplateRenderer();
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenThemeEmit_" + Guid.NewGuid().ToString("N"));
        var hub = Path.Combine(tempRoot, "ThemeApp");
        var doc = Path.Combine(tempRoot, "ThemeApp Doc");
        var web = Path.Combine(tempRoot, "ThemeApp Web");
        var mobile = Path.Combine(tempRoot, "ThemeApp Mobile");

        try
        {
            Directory.CreateDirectory(hub);
            var portalCss = Path.Combine(doc, "portal", "css");
            var portalData = Path.Combine(doc, "portal", "data");
            Directory.CreateDirectory(portalCss);
            Directory.CreateDirectory(portalData);
            await File.WriteAllTextAsync(Path.Combine(portalCss, "theme-overrides.css"), ":root { --navy: #000000; }\n");
            await File.WriteAllTextAsync(
                Path.Combine(portalData, "portal-settings.json"),
                """{"portalName":"ThemeApp","theme":{"primaryColor":"#000000","accentColor":"#111111","highlightColor":"#222222","backgroundColor":"#333333"}}""");

            var brandingLogo = Path.Combine(doc, "portal", "assets");
            Directory.CreateDirectory(brandingLogo);
            var logoPath = Path.Combine(brandingLogo, "logo.png");
            await File.WriteAllBytesAsync(logoPath, [1, 2, 3, 4, 5]);
            var logoBefore = await File.ReadAllBytesAsync(logoPath);

            var mvc = Path.Combine(web, "src", "ThemeApp.MVC");
            Directory.CreateDirectory(Path.Combine(mvc, "wwwroot", "css"));
            Directory.CreateDirectory(Path.Combine(mvc, "Views", "Shared"));
            await File.WriteAllTextAsync(Path.Combine(mvc, "wwwroot", "css", "site.css"), "/* old */\n");
            await File.WriteAllTextAsync(Path.Combine(mvc, "Views", "Shared", "_Layout.cshtml"), "<!-- old -->\n");
            var unrelated = Path.Combine(mvc, "Controllers", "HomeController.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(unrelated)!);
            await File.WriteAllTextAsync(unrelated, "// keep me\n");

            var themeConfig = Path.Combine(mobile, "lib", "app");
            Directory.CreateDirectory(themeConfig);
            await File.WriteAllTextAsync(Path.Combine(themeConfig, "app_theme_config.dart"), "// old theme\n");
            var otherDart = Path.Combine(mobile, "lib", "main.dart");
            await File.WriteAllTextAsync(otherDart, "// main stays\n");

            var spec = MobileTargetMerger.ApplyAppThemePreset(
                new SolutionSpec
                {
                    ApplicationName = "ThemeApp",
                    RootNamespace = "ThemeApp",
                    Database = DatabaseProvider.SqlServer,
                    UiTargets = UiTarget.MvcWeb,
                    Setup = NamingHelper.DefaultSetup(DatabaseProvider.SqlServer),
                    Entities =
                    [
                        new EntitySpec
                        {
                            Name = "Widget",
                            Properties =
                            [
                                new PropertySpec { Name = "Id", ClrType = "int", IsKey = true },
                                new PropertySpec { Name = "Name", ClrType = "string" }
                            ]
                        }
                    ]
                },
                "nature");

            var result = await ThemeEmitter.ApplyAsync(
                spec,
                new ThemeEmitTargets(doc, web, mobile),
                renderer);

            Assert.True(result.Success, result.Message);
            Assert.Contains("Documentation", result.UpdatedLayers);
            Assert.Contains("Web", result.UpdatedLayers);
            Assert.Contains("Mobile", result.UpdatedLayers);

            var overrides = await File.ReadAllTextAsync(Path.Combine(portalCss, "theme-overrides.css"));
            Assert.Contains("--bg: #F5F1E8", overrides, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("--border: #C5BBA8", overrides, StringComparison.OrdinalIgnoreCase);

            var settings = await File.ReadAllTextAsync(Path.Combine(portalData, "portal-settings.json"));
            Assert.Contains("#2D4A3E", settings, StringComparison.OrdinalIgnoreCase);

            var siteCss = await File.ReadAllTextAsync(Path.Combine(mvc, "wwwroot", "css", "site.css"));
            Assert.Contains("#2D4A3E", siteCss, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/* old */", siteCss);

            var flutterTheme = await File.ReadAllTextAsync(Path.Combine(themeConfig, "app_theme_config.dart"));
            Assert.Contains("nature", flutterTheme);
            Assert.Contains("0xFF2D4A3E", flutterTheme);

            Assert.Equal("// keep me\n", await File.ReadAllTextAsync(unrelated));
            Assert.Equal("// main stays\n", await File.ReadAllTextAsync(otherDart));
            Assert.Equal(logoBefore, await File.ReadAllBytesAsync(logoPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAsync_soft_skips_missing_layers()
    {
        var result = await ThemeEmitter.ApplyAsync(
            MobileTargetMerger.ApplyAppThemePreset(
                new SolutionSpec { ApplicationName = "Missing", RootNamespace = "Missing" },
                "appgen"),
            new ThemeEmitTargets(
                Path.Combine(Path.GetTempPath(), "no-doc-" + Guid.NewGuid().ToString("N")),
                null,
                null));

        Assert.False(result.Success);
        Assert.Empty(result.UpdatedLayers);
        Assert.Contains("not generated", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
