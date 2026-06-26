using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class GeneratedWebTestsTests
{
    [Fact]
    public async Task Flutter_generate_includes_entity_api_tests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "WebTests");

        try
        {
            var spec = SpecLoader.CreateDefault("WebTests", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                UiTargets = UiTarget.MvcWeb,
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

            Directory.CreateDirectory(outputDir);
            var renderer = new TemplateRenderer();
            var entity = spec.Entities[0];
            await new SolutionGenerator(renderer).GenerateAsync(spec, outputDir);
            await AppSettingsGenerator.WriteAsync(spec, outputDir);
            await new EntityGenerator(renderer).GenerateAsync(EmptyEntities(spec), entity, outputDir);

            var apiTests = Path.Combine(outputDir, "src", "WebTests.Tests", "Api", "WidgetApiTests.cs");
            var mvcTests = Path.Combine(outputDir, "src", "WebTests.Tests", "Mvc", "WidgetMvcTests.cs");
            Assert.True(File.Exists(apiTests));
            Assert.True(File.Exists(mvcTests));

            var apiContent = await File.ReadAllTextAsync(apiTests);
            Assert.Contains("Crud_roundtrip_works", apiContent);
            Assert.DoesNotContain("SmokeTests", await File.ReadAllTextAsync(Path.Combine(outputDir, "src", "WebTests.Tests", "Infrastructure", "ApiWebApplicationFactory.cs")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Generated_solution_tests_pass_dotnet_test()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "RunnableTests");

        try
        {
            var spec = SpecLoader.CreateDefault("RunnableTests", null, DatabaseProvider.SqlServer);
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

            var sln = Path.Combine(outputDir, "RunnableTests.sln");
            Assert.True(File.Exists(sln));

            var restoreCode = await RunDotNetAsync(sln, "restore");
            Assert.Equal(0, restoreCode);

            var testCode = await RunDotNetAsync(sln, "test", "--no-restore");
            Assert.Equal(0, testCode);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
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

    private static async Task<int> RunDotNetAsync(string slnPath, string command, params string[] extraArgs)
    {
        var args = new List<string> { command, $"\"{slnPath}\"", "--nologo" };
        args.AddRange(extraArgs);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(' ', args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start dotnet {command}.");

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
