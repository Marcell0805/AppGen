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
            Assert.Contains("onAccent = Color(0xFF1A3D2E)", themeConfig);
            Assert.Contains("sidebar = Color(0xFF1A3D2E)", themeConfig);
            Assert.Contains("isDark = false", themeConfig);
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

    [Fact]
    public async Task GenerateAsync_always_emits_update_checker_with_empty_url_by_default()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "UpdateEmptyApp Mobile");

        try
        {
            var spec = SpecLoader.CreateDefault("UpdateEmptyApp", null, DatabaseProvider.SqlServer);
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
                    Mobile = new MobileTargetSpec { Enabled = true }
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
                "com.updateempty.app",
                "http://localhost:5000");

            Assert.True(result.Success, result.Message);
            var root = FlutterProjectPaths.GetFlutterRoot(outputDir);

            Assert.True(File.Exists(Path.Combine(root, "lib", "core", "services", "update_service.dart")));
            Assert.True(File.Exists(Path.Combine(root, "lib", "core", "widgets", "update_prompt_listener.dart")));
            Assert.True(File.Exists(Path.Combine(root, "assets", "mobile_config.json")));

            var config = await File.ReadAllTextAsync(Path.Combine(root, "assets", "mobile_config.json"));
            Assert.Contains("\"updateCheckUrl\": \"\"", config);

            var main = await File.ReadAllTextAsync(Path.Combine(root, "lib", "main.dart"));
            Assert.Contains("UpdatePromptListener", main);

            var pubspec = await File.ReadAllTextAsync(Path.Combine(root, "pubspec.yaml"));
            Assert.Contains("package_info_plus:", pubspec);
            Assert.Contains("assets/mobile_config.json", pubspec);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_bakes_update_check_url_from_publish_settings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "UpdateUrlApp Mobile");

        try
        {
            var spec = SpecLoader.CreateDefault("UpdateUrlApp", null, DatabaseProvider.SqlServer);
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
                            BaseUrl = "https://example.github.io/the-foxs-den-doc",
                            AppId = "update-url-app"
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
                "com.updateurl.app",
                "http://localhost:5000");

            Assert.True(result.Success, result.Message);
            var config = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "assets", "mobile_config.json"));
            Assert.Contains(
                "https://example.github.io/the-foxs-den-doc/downloads/update-url-app/mobile-version.json",
                config);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateAsync_prefers_explicit_update_check_url_override()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "UpdateOverrideApp Mobile");

        try
        {
            var spec = SpecLoader.CreateDefault("UpdateOverrideApp", null, DatabaseProvider.SqlServer);
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
                            BaseUrl = "https://example.github.io/the-foxs-den-doc",
                            AppId = "ignored-id",
                            UpdateCheckUrl = "https://cdn.example.com/custom/mobile-version.json"
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
                "com.updateoverride.app",
                "http://localhost:5000");

            Assert.True(result.Success, result.Message);
            var config = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "assets", "mobile_config.json"));
            Assert.Contains("https://cdn.example.com/custom/mobile-version.json", config);
            Assert.DoesNotContain("ignored-id", config);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
