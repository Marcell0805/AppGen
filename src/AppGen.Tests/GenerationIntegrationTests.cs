using AppGen.Core.Models;
using AppGen.Engine;
using Xunit.Abstractions;

namespace AppGen.Tests;

public class GenerationIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public GenerationIntegrationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Generate_solution_with_entity_builds_successfully()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "CrudTestApp");

        try
        {
            var spec = SpecLoader.CreateDefault("CrudTestApp", null, DatabaseProvider.SqlServer);
            var product = new EntitySpec
            {
                Name = "Product",
                Properties =
                [
                    new PropertySpec { Name = "Product_Id", ClrType = "long", IsKey = true },
                    new PropertySpec { Name = "Name", ClrType = "string" },
                    new PropertySpec { Name = "Price", ClrType = "decimal" }
                ]
            };

            var renderer = new TemplateRenderer();
            var solutionGenerator = new SolutionGenerator(renderer);
            var entityGenerator = new EntityGenerator(renderer);

            await solutionGenerator.GenerateAsync(spec, outputDir);
            var loaded = await SpecLoader.LoadAsync(outputDir);
            await entityGenerator.GenerateAsync(loaded, product, outputDir);

            var slnPath = Path.Combine(outputDir, "CrudTestApp.sln");
            Assert.True(File.Exists(slnPath));

            var createRequestPath = Path.Combine(outputDir, "src/CrudTestApp.Shared/Requests/CreateProductRequest.cs");
            var createRequestContent = await File.ReadAllTextAsync(createRequestPath);
            Assert.DoesNotContain("public long Product_Id", createRequestContent);
            Assert.Contains("public string Name", createRequestContent);

            var repositoryPath = Path.Combine(outputDir, "src/CrudTestApp.Persistence/Repositories/ProductRepository.cs");
            var repositoryContent = await File.ReadAllTextAsync(repositoryPath);
            Assert.Contains("GetTrackedByIdAsync", repositoryContent);

            var servicePath = Path.Combine(outputDir, "src/CrudTestApp.Application/Services/ProductService.cs");
            var serviceContent = await File.ReadAllTextAsync(servicePath);
            Assert.Contains("GetTrackedByIdAsync", serviceContent);

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
    public void Rendered_repository_update_uses_tracked_entity_pattern()
    {
        var spec = new SolutionSpec
        {
            ApplicationName = "TestApp",
            RootNamespace = "TestApp",
            Database = DatabaseProvider.SqlServer,
            Entities = []
        };

        var entity = new EntitySpec
        {
            Name = "Item",
            Properties =
            [
                new PropertySpec { Name = "Item_Id", ClrType = "long", IsKey = true },
                new PropertySpec { Name = "Name", ClrType = "string" }
            ]
        };

        var model = EntityGenerator.BuildEntityModel(spec, entity);
        var renderer = new TemplateRenderer();
        var content = renderer.Render(AppGen.Templates.TemplateProvider.Load("Entity/Repository.scriban"), model);

        Assert.Contains("GetTrackedByIdAsync", content);
        Assert.Contains("AsNoTracking()", content);
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
