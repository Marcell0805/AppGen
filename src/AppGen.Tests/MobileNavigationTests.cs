using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class MobileNavigationTests
{
    [Fact]
    public async Task Mobile_generate_emits_navigation_helpers_without_onExit_in_router()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "NavApp");

        try
        {
            var spec = SpecLoader.CreateDefault("NavApp", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
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
                    },
                    new EntitySpec
                    {
                        Name = "Gadget",
                        IncludeInUi = true,
                        Properties =
                        [
                            new PropertySpec { Name = "Gadget_Id", ClrType = "long", IsKey = true },
                            new PropertySpec { Name = "Name", ClrType = "string" }
                        ]
                    }
                ]
            };

            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
            var result = await generator.GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var flutterRoot = FlutterProjectPaths.GetFlutterRoot(outputDir);
            var router = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "lib", "app", "router.dart"));
            Assert.DoesNotContain("onExit:", router);

            var helpers = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "lib", "app", "navigation_helpers.dart"));
            Assert.Contains("const String appHomePath = '/widget';", helpers);
            Assert.Contains("'/widget'", helpers);
            Assert.Contains("'/gadget'", helpers);
            Assert.Contains("_isHandlingRouterBack", helpers);
            Assert.DoesNotContain("onExit:", helpers);

            var shell = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "lib", "app", "app_shell.dart"));
            Assert.Contains("BackButtonListener", shell);
            Assert.Contains("handleRouterBack", shell);

            var main = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "lib", "main.dart"));
            Assert.Contains("AndroidBackBridge.install", main);

            var detail = await File.ReadAllTextAsync(
                Path.Combine(flutterRoot, "lib", "features", "widget", "screens", "widget_detail_screen.dart"));
            Assert.Contains("navigateBackOrHome", detail);

            var form = await File.ReadAllTextAsync(
                Path.Combine(flutterRoot, "lib", "features", "widget", "screens", "widget_form_screen.dart"));
            Assert.Contains("navigateBackOrHome", form);

            var bridge = await File.ReadAllTextAsync(
                Path.Combine(flutterRoot, "lib", "core", "platform", "android_back_bridge.dart"));
            Assert.Contains("MethodChannel", bridge);
            Assert.Contains("/back", bridge);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task MainActivity_patcher_writes_back_channel_when_kotlin_folder_exists()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var flutterRoot = Path.Combine(tempRoot, "mobile", "flutter");
        var packageName = "com.example.navapp";
        var mainActivityPath = Path.Combine(
            flutterRoot,
            "android",
            "app",
            "src",
            "main",
            "kotlin",
            "com",
            "example",
            "navapp",
            "MainActivity.kt");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(mainActivityPath)!);
            await File.WriteAllTextAsync(mainActivityPath, "// placeholder");

            var renderer = new TemplateRenderer();
            var patch = await FlutterMainActivityPatcher.PatchAsync(renderer, flutterRoot, packageName);

            Assert.True(patch.Patched, patch.Message);
            var content = await File.ReadAllTextAsync(mainActivityPath);
            Assert.Contains("OnBackPressedCallback", content);
            Assert.Contains("com.example.navapp/back", content);
            Assert.Contains("FlutterFragmentActivity", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
