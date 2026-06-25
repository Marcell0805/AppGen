using AppGen.Core.Models;
using AppGen.Engine;
using Xunit.Abstractions;

namespace AppGen.Tests;

public class PortalIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PortalIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Portal_generate_produces_expected_folder_structure()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = GenerationOutputHelper.ResolveLayerDirectory(tempRoot, "PortalTestApp", ProjectOutputLayer.Documentation);

        try
        {
            var spec = SpecLoader.CreatePortalDefault("PortalTestApp", null);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Phase = spec.Phase,
                Portal = spec.Portal,
                EntitySketches =
                [
                    new EntitySketch { Name = "Company", Description = "Tenant root", Phase = "1" },
                    new EntitySketch { Name = "Equipment", Description = "Fleet asset", Phase = "1" }
                ],
                Database = spec.Database,
                UiTargets = spec.UiTargets,
                Setup = spec.Setup,
                Entities = spec.Entities
            };

            var renderer = new TemplateRenderer();
            var generator = new PortalGenerator(renderer);
            var service = new PortalGenerationService(generator);
            var result = await service.GenerateAsync(spec, tempRoot);

            Assert.True(result.Success, result.Message);
            Assert.True(File.Exists(Path.Combine(outputDir, "appgen.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "portal", "index.html")));
            Assert.True(File.Exists(Path.Combine(outputDir, "portal", "data", "vision.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "portal", "data", "entities.json")));
            Assert.True(File.Exists(Path.Combine(outputDir, "portal", "js", "portal-data.js")));
            Assert.True(File.Exists(Path.Combine(outputDir, "portal", "sections", "vision.html")));
            Assert.False(Directory.Exists(Path.Combine(outputDir, "src")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Portal_import_round_trips_section_edits()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            var spec = SpecLoader.CreatePortalDefault("ImportTestApp", null);
            var renderer = new TemplateRenderer();
            var service = new PortalGenerationService(new PortalGenerator(renderer));
            var outputDir = GenerationOutputHelper.ResolveLayerDirectory(tempRoot, "ImportTestApp", ProjectOutputLayer.Documentation);
            await service.GenerateAsync(spec, tempRoot);

            var visionPath = Path.Combine(outputDir, "portal", "data", "vision.json");
            var visionJson = await File.ReadAllTextAsync(visionPath);
            var updatedVision = visionJson.Replace("Describe what", "UPDATED vision for", StringComparison.Ordinal);
            await File.WriteAllTextAsync(visionPath, updatedVision);

            var imported = await PortalSpecImporter.ImportAsync(outputDir);
            var visionSection = imported.Portal!.Sections.First(s => s.Id == "vision");
            Assert.Contains("UPDATED vision", visionSection.Blocks[0].Content ?? string.Empty);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Promote_generates_buildable_solution_from_manifest()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = GenerationOutputHelper.ResolveLayerDirectory(tempRoot, "PromoteTestApp", ProjectOutputLayer.Documentation);

        try
        {
            var spec = SpecLoader.CreatePortalDefault("PromoteTestApp", null, DatabaseProvider.SqlServer, UiTarget.None);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Phase = spec.Phase,
                Portal = spec.Portal,
                EntitySketches = spec.EntitySketches,
                Database = spec.Database,
                UiTargets = spec.UiTargets,
                Setup = spec.Setup,
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

            var renderer = new TemplateRenderer();
            await new PortalGenerationService(new PortalGenerator(renderer)).GenerateAsync(spec, tempRoot);
            await ProjectSpecWriter.WriteAsync(spec, outputDir);

            var promoter = new ProjectPromoter(
                new SolutionGenerator(renderer),
                new EntityGenerator(renderer),
                new UiGenerator(renderer));

            var result = await promoter.PromoteAsync(outputDir);
            Assert.True(result.Success, result.Message);

            var slnPath = Path.Combine(outputDir, "PromoteTestApp.sln");
            Assert.True(File.Exists(slnPath));
            Assert.True(Directory.Exists(Path.Combine(outputDir, "portal")));
            Assert.True(File.Exists(Path.Combine(outputDir, "src", "PromoteTestApp.API", "Controllers", "V1", "WidgetController.cs")));

            var exitCode = await RunDotNetBuildAsync(slnPath);
            Assert.Equal(0, exitCode);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task V3_appgen_json_loads_for_api_only_projects()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var v3Json = """
            {
              "schemaVersion": 3,
              "applicationName": "LegacyApp",
              "rootNamespace": "LegacyApp",
              "database": "SqlServer",
              "uiTargets": [],
              "setup": {
                "activeConnectionName": "Dev",
                "ensureCreatedInDevelopment": true,
                "configEntries": []
              },
              "entities": []
            }
            """;
            await File.WriteAllTextAsync(Path.Combine(tempRoot, "appgen.json"), v3Json);

            var spec = await SpecLoader.LoadAsync(tempRoot);
            Assert.Equal(SolutionSpec.CurrentSchemaVersion, spec.SchemaVersion);
            Assert.Equal("LegacyApp", spec.ApplicationName);
            Assert.Null(spec.Portal);
            Assert.Empty(spec.EntitySketches);
            Assert.NotNull(spec.Targets);
            Assert.True(spec.Targets!.Web.Enabled);
            Assert.False(spec.Targets.Documentation.Enabled);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private async Task<int> RunDotNetBuildAsync(string slnPath)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"build \"{slnPath}\" --nologo",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet build.");

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            _output.WriteLine(stdout);
            _output.WriteLine(stderr);
        }

        return process.ExitCode;
    }
}
