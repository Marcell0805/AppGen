using AppGen.Core.Models;
using AppGen.Engine;
using Xunit.Abstractions;

namespace AppGen.Tests;

public class ProjectInfoAndReadmeTests
{
    [Fact]
    public async Task ProjectInfo_round_trips_in_manifest()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "DescApp",
                RootNamespace = "DescApp",
                Project = new ProjectInfoSpec
                {
                    Tagline = "Fleet management for contractors",
                    Description = "This app tracks equipment and work orders across sites."
                },
                Entities = []
            };

            await ProjectSpecWriter.WriteAsync(spec, tempRoot);
            var loaded = await SpecLoader.LoadAsync(tempRoot);

            Assert.Equal(SolutionSpec.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.NotNull(loaded.Project);
            Assert.Equal("Fleet management for contractors", loaded.Project!.Tagline);
            Assert.Contains("equipment", loaded.Project.Description);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ReadmeGenerator_writes_mobile_readme_with_api_prerequisite()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "MobileReadmeApp",
                RootNamespace = "MobileReadmeApp",
                Project = new ProjectInfoSpec { Tagline = "Mobile test app" },
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

            await ReadmeGenerator.WriteMobileAsync(new ReadmeContext(
                spec,
                tempRoot,
                ApiBaseUrl: "https://localhost:5001",
                EnableMobile: true));

            var readme = await File.ReadAllTextAsync(Path.Combine(tempRoot, "README.md"));
            Assert.Contains("Flutter", readme);
            Assert.Contains("Web API must be running", readme);
            Assert.Contains("flutter pub get", readme);
            Assert.Contains("https://localhost:5001", readme);
            Assert.Contains("Mobile test app", readme);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ReadmeGenerator_hub_links_enabled_layers()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = "HubApp",
                RootNamespace = "HubApp",
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

            await ReadmeGenerator.WriteHubAsync(new ReadmeContext(
                spec,
                tempRoot,
                OutputRoot: tempRoot,
                EnableDocumentation: true,
                EnableWeb: true,
                EnableMobile: true));

            var readme = await File.ReadAllTextAsync(Path.Combine(tempRoot, "README.md"));
            Assert.Contains("HubApp Doc/README.md", readme);
            Assert.Contains("HubApp Web/README.md", readme);
            Assert.Contains("HubApp Mobile/README.md", readme);
            Assert.Contains("**Item**", readme);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ProjectInfoSeeder_seeds_vision_when_placeholder()
    {
        var spec = SpecLoader.CreatePortalDefault("SeedApp", null);
        spec = new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = new ProjectInfoSpec
            {
                Tagline = "Built for field teams",
                Description = "Coordinates crews and assets in real time."
            },
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };

        var seeded = ProjectInfoSeeder.ApplyToPortalSpec(spec);
        var vision = seeded.Portal!.Sections.First(s => s.Id == "vision");
        var block = vision.Blocks.First(b => b.Id == "vision-statement");
        Assert.Contains("Coordinates crews", block.Content);
        Assert.Equal("Built for field teams", seeded.Portal.Settings.Tagline);
    }
}
