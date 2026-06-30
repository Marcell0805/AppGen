using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class AuthAndOfflineTests
{
    [Fact]
    public async Task V7_manifest_round_trips_web_auth_and_mobile_offline()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "AuthOfflineApp",
                RootNamespace = "AuthOfflineApp",
                Targets = new ApplicationTargets
                {
                    Web = new WebTargetSpec
                    {
                        Enabled = true,
                        Auth = new WebAuthTargetSpec { Enabled = true, TokenLifetimeMinutes = 90 }
                    },
                    Mobile = new MobileTargetSpec
                    {
                        Enabled = true,
                        Offline = new MobileOfflineTargetSpec { Enabled = true }
                    }
                },
                Entities = []
            };

            await ProjectSpecWriter.WriteAsync(spec, tempRoot);
            var loaded = await SpecLoader.LoadAsync(tempRoot);
            Assert.Equal(8, loaded.SchemaVersion);
            Assert.True(loaded.Targets!.Web.Auth.Enabled);
            Assert.Equal(90, loaded.Targets.Web.Auth.TokenLifetimeMinutes);
            Assert.True(loaded.Targets.Mobile.Offline.Enabled);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Web_generate_with_auth_produces_AuthController_and_Authorize()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "WebAuth");

        try
        {
            var spec = SpecLoader.CreateDefault("WebAuth", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                Targets = new ApplicationTargets
                {
                    Web = new WebTargetSpec { Enabled = true, Auth = new WebAuthTargetSpec { Enabled = true } }
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
            var renderer = new TemplateRenderer();
            var entity = spec.Entities[0];
            await new SolutionGenerator(renderer).GenerateAsync(spec, outputDir);
            await AppSettingsGenerator.WriteAsync(spec, outputDir);
            await new EntityGenerator(renderer).GenerateAsync(EmptyEntities(spec), entity, outputDir);

            var authController = await File.ReadAllTextAsync(Path.Combine(outputDir, "src", "WebAuth.API", "Controllers", "V1", "AuthController.cs"));
            Assert.Contains("class AuthController", authController);

            var entityController = await File.ReadAllTextAsync(Path.Combine(outputDir, "src", "WebAuth.API", "Controllers", "V1", "WidgetController.cs"));
            Assert.Contains("[Authorize]", entityController);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_generate_with_offline_only_includes_cache_not_login()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "OfflineApp");

        try
        {
            var spec = BuildMobileSpec(offline: true, auth: false);
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var result = await new MobileApplicationGenerator(new FlutterGenerator(renderer)).GenerateAsync(spec, outputDir, new GeneratorOptions());
            Assert.True(result.Success, result.Message);

            Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "core", "offline", "offline_cache.dart")));
            Assert.False(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "auth", "screens", "login_screen.dart")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_generate_with_auth_includes_login_not_when_auth_off()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "AuthMobile");

        try
        {
            var spec = BuildMobileSpec(offline: false, auth: true);
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var result = await new MobileApplicationGenerator(new FlutterGenerator(renderer)).GenerateAsync(spec, outputDir, new GeneratorOptions());
            Assert.True(result.Success, result.Message);

            Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "auth", "screens", "login_screen.dart")));
            var pubspec = await File.ReadAllTextAsync(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "pubspec.yaml"));
            Assert.Contains("flutter_secure_storage", pubspec);
            Assert.DoesNotContain("sqflite", pubspec);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_generate_with_offline_emits_unique_pubspec_dependencies()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "OfflinePubspecApp");

        try
        {
            var spec = BuildMobileSpec(offline: true, auth: false);
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var result = await new MobileApplicationGenerator(new FlutterGenerator(renderer)).GenerateAsync(spec, outputDir, new GeneratorOptions());
            Assert.True(result.Success, result.Message);

            var pubspec = await File.ReadAllTextAsync(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "pubspec.yaml"));
            Assert.Equal(1, CountPackageOccurrences(pubspec, "sqflite:"));
            Assert.Equal(1, CountPackageOccurrences(pubspec, "path:"));
            Assert.Equal(1, CountPackageOccurrences(pubspec, "connectivity_plus:"));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static int CountPackageOccurrences(string pubspec, string packageLine) =>
        pubspec.Split('\n').Count(line => line.TrimStart().StartsWith(packageLine, StringComparison.Ordinal));

    private static SolutionSpec BuildMobileSpec(bool offline, bool auth)
    {
        var spec = SpecLoader.CreateDefault("MobileAuth", null, DatabaseProvider.SqlServer);
        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Database = spec.Database,
            Setup = spec.Setup,
            Targets = new ApplicationTargets
            {
                Web = new WebTargetSpec { Enabled = true, Auth = new WebAuthTargetSpec { Enabled = auth } },
                Mobile = new MobileTargetSpec
                {
                    Enabled = true,
                    Offline = new MobileOfflineTargetSpec { Enabled = offline }
                }
            },
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

    private static SolutionSpec EmptyEntities(SolutionSpec spec) => new()
    {
        SchemaVersion = spec.SchemaVersion,
        ApplicationName = spec.ApplicationName,
        RootNamespace = spec.RootNamespace,
        Project = spec.Project,
        Phase = spec.Phase,
        Portal = spec.Portal,
        EntitySketches = spec.EntitySketches,
        Targets = spec.Targets,
        Generation = spec.Generation,
        Database = spec.Database,
        UiTargets = spec.UiTargets,
        Setup = spec.Setup,
        Entities = []
    };
}
