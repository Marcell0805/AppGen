using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class MobileUiShellTests
{
    [Fact]
    public async Task Flutter_generate_includes_app_drawer_and_theme_config()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "UiShellApp");

        try
        {
            var spec = BuildSpec("UiShellApp");
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
            var result = await generator.GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var flutterRoot = Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib");
            Assert.True(File.Exists(Path.Combine(flutterRoot, "app", "app_drawer.dart")));
            Assert.True(File.Exists(Path.Combine(flutterRoot, "app", "app_theme_config.dart")));
            Assert.True(File.Exists(Path.Combine(flutterRoot, "core", "widgets", "app_page_header.dart")));

            var drawer = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "app", "app_drawer.dart"));
            Assert.Contains("class AppDrawer", drawer);
            Assert.Contains("playfairDisplay", drawer);

            var shell = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "app", "app_shell.dart"));
            Assert.Contains("AppDrawer", shell);
            Assert.DoesNotContain("bottomNavigationBar", shell);

            var themeConfig = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "app", "app_theme_config.dart"));
            Assert.Contains("abstract final class AppThemeConfig", themeConfig);
            Assert.Contains("0xFF3B82F6", themeConfig);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_theme_uses_portal_colors_when_preset_portal()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "PortalThemeApp");

        try
        {
            var spec = BuildSpec("PortalThemeApp");
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                Project = new ProjectInfoSpec { Tagline = "Industrial intelligence" },
                Portal = new PortalSpec
                {
                    Settings = new PortalSettings
                    {
                        PortalName = "PortalThemeApp",
                        Tagline = "Portal tagline",
                        Theme = new PortalThemeSettings
                        {
                            PrimaryColor = "#1B3A5C",
                            AccentColor = "#2E75B6",
                            BackgroundColor = "#E8F1F8",
                            HighlightColor = "#F28C28"
                        }
                    }
                },
                Targets = new ApplicationTargets
                {
                    Documentation = new DocumentationTargetSpec { Enabled = true },
                    Web = new WebTargetSpec { Enabled = true },
                    Mobile = new MobileTargetSpec
                    {
                        Enabled = true,
                        Theme = new MobileThemeSpec { Preset = "portal" }
                    }
                },
                Entities = spec.Entities
            };

            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
            var result = await generator.GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var themeConfig = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "app", "app_theme_config.dart"));
            Assert.Contains("preset = 'portal'", themeConfig);
            Assert.Contains("0xFF1B3A5C", themeConfig);
            Assert.Contains("0xFF2E75B6", themeConfig);
            Assert.Contains("0xFFE8F1F8", themeConfig);

            var drawer = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "app", "app_drawer.dart"));
            Assert.Contains("Industrial intelligence", drawer);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_generate_list_screen_uses_page_header_not_nested_scaffold()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "HeaderApp");

        try
        {
            var spec = BuildSpec("HeaderApp");
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
            var result = await generator.GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var listScreen = await File.ReadAllTextAsync(
                Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "screens", "widget_list_screen.dart"));

            Assert.Contains("AppPageHeader", listScreen);
            Assert.DoesNotContain("Scaffold(", listScreen);
            Assert.DoesNotContain("floatingActionButton", listScreen);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static SolutionSpec BuildSpec(string appName)
    {
        var spec = SpecLoader.CreateDefault(appName, null, DatabaseProvider.SqlServer);
        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Database = spec.Database,
            Setup = spec.Setup,
            Project = new ProjectInfoSpec { Tagline = "Mobile shell test app" },
            Entities =
            [
                new EntitySpec
                {
                    Name = "Widget",
                    IncludeInUi = true,
                    Properties =
                    [
                        new PropertySpec { Name = "Widget_Id", ClrType = "long", IsKey = true },
                        new PropertySpec { Name = "Name", ClrType = "string" }
                    ]
                }
            ]
        };
    }
}
