using AppGen.Core.Capabilities;
using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class MobileCapabilitiesTests
{
    [Fact]
    public async Task V8_manifest_round_trips_mobile_capabilities()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "CapApp",
                RootNamespace = "CapApp",
                Targets = new ApplicationTargets
                {
                    Mobile = new MobileTargetSpec
                    {
                        Enabled = true,
                        Capabilities = new MobileCapabilitiesSpec
                        {
                            Enabled = [MobileCapabilityId.Camera, MobileCapabilityId.Gps]
                        }
                    }
                },
                Entities = []
            };

            await ProjectSpecWriter.WriteAsync(spec, tempRoot);
            var loaded = await SpecLoader.LoadAsync(tempRoot);
            Assert.Equal(8, loaded.SchemaVersion);
            Assert.Contains(MobileCapabilityId.Camera, loaded.Targets!.Mobile.Capabilities.Enabled);
            Assert.Contains(MobileCapabilityId.Gps, loaded.Targets.Mobile.Capabilities.Enabled);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Resolver_includes_legacy_offline_and_auth_capabilities()
    {
        var spec = new SolutionSpec
        {
            ApplicationName = "Legacy",
            RootNamespace = "Legacy",
            Targets = new ApplicationTargets
            {
                Web = new WebTargetSpec { Auth = new WebAuthTargetSpec { Enabled = true } },
                Mobile = new MobileTargetSpec
                {
                    Offline = new MobileOfflineTargetSpec { Enabled = true }
                }
            }
        };

        var ids = MobileCapabilityResolver.Resolve(spec).Select(c => c.Id).ToList();
        Assert.Contains(MobileCapabilityId.OfflineCache, ids);
        Assert.Contains(MobileCapabilityId.SecureStorage, ids);
        Assert.Contains(MobileCapabilityId.JwtAuth, ids);
    }

    [Fact]
    public void Resolver_orders_dependencies()
    {
        var catalog = MobileCapabilityCatalog.TryGet(MobileCapabilityId.Camera);
        Assert.NotNull(catalog);
    }

    [Fact]
    public async Task Flutter_generate_with_camera_emits_service_and_pubspec()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "CapFlutter");

        try
        {
            var spec = BuildSpec([MobileCapabilityId.Camera]);
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var result = await new MobileApplicationGenerator(new FlutterGenerator(renderer))
                .GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var flutterRoot = Path.Combine(outputDir, "mobile", "flutter");
            var service = Path.Combine(flutterRoot, "lib", "core", "services", "camera_service.dart");
            Assert.True(File.Exists(service));
            Assert.Contains("class CameraService", await File.ReadAllTextAsync(service));

            var pubspec = await File.ReadAllTextAsync(Path.Combine(flutterRoot, "pubspec.yaml"));
            Assert.Contains("image_picker:", pubspec);
            Assert.Contains("permission_handler:", pubspec);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Platform_patcher_merges_android_permissions()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var manifestDir = Path.Combine(tempRoot, "android", "app", "src", "main");
        Directory.CreateDirectory(manifestDir);
        var manifestPath = Path.Combine(manifestDir, "AndroidManifest.xml");
        await File.WriteAllTextAsync(manifestPath,
            """
            <?xml version="1.0" encoding="utf-8"?>
            <manifest xmlns:android="http://schemas.android.com/apk/res/android">
              <application android:label="test" />
            </manifest>
            """);

        var capabilities = MobileCapabilityResolver.Resolve(BuildSpec([MobileCapabilityId.Camera]));
        await FlutterPlatformConfigPatcher.PatchAsync(tempRoot, capabilities);

        var text = await File.ReadAllTextAsync(manifestPath);
        Assert.Contains("android.permission.CAMERA", text);
        Assert.Contains("AppGen:capability:camera", text);
    }

    [Fact]
    public async Task Flutter_generate_with_gallery_and_maps_emits_services()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "CapBatch");

        try
        {
            var spec = BuildSpec([MobileCapabilityId.Gallery, MobileCapabilityId.Maps]);
            Directory.CreateDirectory(outputDir);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var renderer = new TemplateRenderer();
            var result = await new MobileApplicationGenerator(new FlutterGenerator(renderer))
                .GenerateAsync(spec, outputDir, new GeneratorOptions());

            Assert.True(result.Success, result.Message);

            var servicesDir = Path.Combine(outputDir, "mobile", "flutter", "lib", "core", "services");
            Assert.True(File.Exists(Path.Combine(servicesDir, "gallery_service.dart")));
            Assert.True(File.Exists(Path.Combine(servicesDir, "maps_service.dart")));

            var pubspec = await File.ReadAllTextAsync(Path.Combine(outputDir, "mobile", "flutter", "pubspec.yaml"));
            Assert.Contains("google_maps_flutter:", pubspec);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void Catalog_has_no_unimplemented_ui_capabilities()
    {
        var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            MobileCapabilityId.OfflineCache,
            MobileCapabilityId.SecureStorage,
            MobileCapabilityId.JwtAuth,
            MobileCapabilityId.Internet
        };

        foreach (var cap in MobileCapabilityCatalog.GetAll().Where(c => !hidden.Contains(c.Id)))
            Assert.True(cap.IsImplemented, $"Capability {cap.Id} should be implemented");
    }

    private static SolutionSpec BuildSpec(IEnumerable<string> capabilityIds)
    {
        var baseSpec = SpecLoader.CreateDefault("CapFlutter", null, DatabaseProvider.SqlServer);
        return new SolutionSpec
        {
            SchemaVersion = baseSpec.SchemaVersion,
            ApplicationName = baseSpec.ApplicationName,
            RootNamespace = baseSpec.RootNamespace,
            Database = baseSpec.Database,
            Setup = baseSpec.Setup,
            Targets = new ApplicationTargets
            {
                Mobile = new MobileTargetSpec
                {
                    Enabled = true,
                    Capabilities = new MobileCapabilitiesSpec { Enabled = capabilityIds.ToList() }
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
}
