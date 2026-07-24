using AppGen.Core.Branding;
using AppGen.Core.Models;
using AppGen.Engine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AppGen.Tests;

public class ProjectBrandingEmitterTests
{
    [Fact]
    public async Task EmitAllAsync_writes_documentation_mvc_and_mobile_assets()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var hubDir = Path.Combine(tempRoot, "BrandApp");
        var docDir = Path.Combine(tempRoot, "BrandApp Doc");
        var webDir = Path.Combine(tempRoot, "BrandApp Web");
        var mobileDir = Path.Combine(tempRoot, "BrandApp Mobile");

        try
        {
            Directory.CreateDirectory(hubDir);
            Directory.CreateDirectory(docDir);
            Directory.CreateDirectory(Path.Combine(docDir, "portal", "assets"));
            Directory.CreateDirectory(Path.Combine(webDir, "src", "BrandApp.MVC", "wwwroot"));
            Directory.CreateDirectory(Path.Combine(mobileDir, "android", "app", "src", "main", "res", "mipmap-mdpi"));
            Directory.CreateDirectory(Path.Combine(mobileDir, "web", "icons"));

            var iconPath = Path.Combine(hubDir, ProjectBrandingConstants.DefaultIconRelativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(iconPath)!);
            await CreateSquarePngAsync(iconPath, 512);

            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "BrandApp",
                RootNamespace = "BrandApp",
                Project = new ProjectInfoSpec
                {
                    Branding = new ProjectBrandingSpec
                    {
                        IconPath = ProjectBrandingConstants.DefaultIconRelativePath,
                        OriginalFileName = "icon.png"
                    }
                },
                Entities = []
            };

            var result = await ProjectBrandingEmitter.EmitAllAsync(
                spec,
                hubDir,
                new ProjectBrandingTargets
                {
                    DocumentationDirectory = docDir,
                    WebDirectory = webDir,
                    MobileDirectory = mobileDir
                });

            Assert.True(result.Emitted);
            Assert.True(File.Exists(Path.Combine(docDir, "portal", "assets", "logo.png")));
            Assert.True(File.Exists(Path.Combine(webDir, "src", "BrandApp.MVC", "wwwroot", "branding", "logo.png")));
            Assert.True(File.Exists(Path.Combine(webDir, "src", "BrandApp.MVC", "wwwroot", "favicon.png")));
            Assert.True(File.Exists(Path.Combine(mobileDir, "android", "app", "src", "main", "res", "mipmap-mdpi", "ic_launcher.png")));
            Assert.True(File.Exists(Path.Combine(mobileDir, "assets", "branding", "icon.png")));
            Assert.True(File.Exists(Path.Combine(mobileDir, "web", "icons", "Icon-192.png")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Branding_round_trips_in_manifest()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "BrandRoundTrip",
                RootNamespace = "BrandRoundTrip",
                Project = new ProjectInfoSpec
                {
                    Branding = new ProjectBrandingSpec
                    {
                        IconPath = ProjectBrandingConstants.DefaultIconRelativePath,
                        OriginalFileName = "logo.png"
                    }
                },
                Entities = []
            };

            await ProjectSpecWriter.WriteAsync(spec, tempRoot);
            var loaded = await SpecLoader.LoadAsync(tempRoot);

            Assert.Equal(SolutionSpec.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.NotNull(loaded.Project?.Branding);
            Assert.Equal(ProjectBrandingConstants.DefaultIconRelativePath, loaded.Project!.Branding!.IconPath);
            Assert.Equal("logo.png", loaded.Project.Branding.OriginalFileName);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static async Task CreateSquarePngAsync(string path, int size)
    {
        using var image = new Image<Rgba32>(size, size);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new Rgba32(37, 99, 235, 255);
            }
        });
        await image.SaveAsPngAsync(path);
    }
}
