using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class MobileGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_preserves_cookbook_theme_from_manifest()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "CookbookThemeApp Mobile");

        try
        {
            var spec = SpecLoader.CreateDefault("CookbookThemeApp", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                Project = new ProjectInfoSpec { Tagline = "Mobile tester" },
                Targets = new ApplicationTargets
                {
                    Web = new WebTargetSpec { Enabled = false },
                    Mobile = new MobileTargetSpec
                    {
                        Enabled = true,
                        Theme = new MobileThemeSpec { Preset = "cookbook" }
                    }
                },
                Entities =
                [
                    new EntitySpec
                    {
                        Name = "Widget",
                        Properties =
                        [
                            new PropertySpec { Name = "Widget_Id", ClrType = "long", IsKey = true },
                            new PropertySpec { Name = "Name", ClrType = "string" }
                        ]
                    }
                ]
            };

            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var service = new MobileGenerationService(new MobileApplicationGenerator(new FlutterGenerator(renderer)));
            var result = await service.GenerateAsync(
                spec,
                tempRoot,
                ["Widget"],
                "com.cookbook.app",
                "http://localhost:5000");

            Assert.True(result.Success, result.Message);

            var themeConfig = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "app", "app_theme_config.dart"));
            Assert.Contains("preset = 'cookbook'", themeConfig);
            Assert.Contains("0xFFC9A227", themeConfig);
            Assert.DoesNotContain("0xFF3B82F6", themeConfig);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ApplyWizardMobileSettings_overrides_stale_manifest_theme()
    {
        var spec = new SolutionSpec
        {
            ApplicationName = "Demo",
            RootNamespace = "Demo",
            Targets = new ApplicationTargets
            {
                Mobile = new MobileTargetSpec
                {
                    Theme = new MobileThemeSpec { Preset = "appgen" }
                }
            }
        };

        var merged = MobileTargetMerger.ApplyWizardMobileSettings(spec, new MobileTargetSpec
        {
            Theme = new MobileThemeSpec { Preset = "cookbook" }
        });

        Assert.Equal("cookbook", merged.Targets!.Mobile.Theme.Preset);
    }

    [Fact]
    public async Task GenerateAsync_emits_publish_mobile_script_with_defaults()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "PublishScriptApp Mobile");

        try
        {
            var spec = SpecLoader.CreateDefault("PublishScriptApp", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                Targets = new ApplicationTargets
                {
                    Web = new WebTargetSpec { Enabled = false },
                    Mobile = new MobileTargetSpec
                    {
                        Enabled = true,
                        Publish = new MobilePublishTargetSpec
                        {
                            BaseUrl = "https://example.github.io/publish-script-app",
                            ApkFileName = "custom-app.apk"
                        }
                    }
                },
                Entities =
                [
                    new EntitySpec
                    {
                        Name = "Item",
                        Properties =
                        [
                            new PropertySpec { Name = "Item_Id", ClrType = "long", IsKey = true },
                            new PropertySpec { Name = "Name", ClrType = "string" }
                        ]
                    }
                ]
            };

            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var service = new MobileGenerationService(new MobileApplicationGenerator(new FlutterGenerator(renderer)));
            var result = await service.GenerateAsync(
                spec,
                tempRoot,
                ["Item"],
                "com.publish.app",
                "http://localhost:5000");

            Assert.True(result.Success, result.Message);

            var scriptPath = Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "scripts", "publish-mobile.ps1");
            Assert.True(File.Exists(scriptPath));

            var script = await File.ReadAllTextAsync(scriptPath);
            Assert.Contains("custom-app.apk", script);
            Assert.Contains("https://example.github.io/publish-script-app", script);
            Assert.Contains("Join-Path $MobileRoot \"dist\"", script);
            Assert.Contains("mobile-version.json", script);
            Assert.Contains("Generated by AppGen for PublishScriptApp", script);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
