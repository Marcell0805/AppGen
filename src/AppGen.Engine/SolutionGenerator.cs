using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class SolutionGenerator(TemplateRenderer renderer)
{
    private static readonly (string Template, Func<SolutionSpec, string> Output)[] Files =
    [
        ("Solution/solution.sln.scriban", s => $"{s.ApplicationName}.sln"),
        ("Solution/gitignore.scriban", _ => ".gitignore"),
        ("Solution/api/Program.scriban", s => $"src/{s.ApiProject}/Program.cs"),
        ("Solution/api/csproj.scriban", s => $"src/{s.ApiProject}/{s.ApiProject}.csproj"),
        ("Solution/api/appsettings.json.scriban", s => $"src/{s.ApiProject}/appsettings.json"),
        ("Solution/api/appsettings.Development.json.scriban", s => $"src/{s.ApiProject}/appsettings.Development.json"),
        ("Solution/api/Properties/launchSettings.scriban", s => $"src/{s.ApiProject}/Properties/launchSettings.json"),
        ("Solution/api/Controllers/HealthController.scriban", s => $"src/{s.ApiProject}/Controllers/HealthController.cs"),
        ("Solution/application/csproj.scriban", s => $"src/{s.ApplicationProject}/{s.ApplicationProject}.csproj"),
        ("Solution/application/DependencyInjection.scriban", s => $"src/{s.ApplicationProject}/DependencyInjection.cs"),
        ("Solution/domain/csproj.scriban", s => $"src/{s.DomainProject}/{s.DomainProject}.csproj"),
        ("Solution/persistence/csproj.scriban", s => $"src/{s.PersistenceProject}/{s.PersistenceProject}.csproj"),
        ("Solution/persistence/ApplicationDbContext.scriban", s => $"src/{s.PersistenceProject}/Contexts/ApplicationDbContext.cs"),
        ("Solution/persistence/DependencyInjection.scriban", s => $"src/{s.PersistenceProject}/DependencyInjection.cs"),
        ("Solution/shared/csproj.scriban", s => $"src/{s.SharedProject}/{s.SharedProject}.csproj"),
        ("Solution/shared/Response.scriban", s => $"src/{s.SharedProject}/Wrappers/Response.cs"),
        ("Solution/shared/ApiException.scriban", s => $"src/{s.SharedProject}/Exceptions/ApiException.cs"),
        ("Solution/shared/ValidationException.scriban", s => $"src/{s.SharedProject}/Exceptions/ValidationException.cs"),
        ("Solution/tests/csproj.scriban", s => $"src/{s.TestsProject}/{s.TestsProject}.csproj"),
        ("Solution/tests/SmokeTests.scriban", s => $"src/{s.TestsProject}/SmokeTests.cs"),
        ("Solution/appgen.json.scriban", _ => "appgen.json"),
    ];

    public async Task GenerateAsync(SolutionSpec spec, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        var model = BuildModel(spec);

        foreach (var (templatePath, outputPathFunc) in Files)
        {
            ct.ThrowIfCancellationRequested();
            var content = renderer.Render(TemplateProvider.Load(templatePath), model);
            var relativePath = outputPathFunc(spec);
            var fullPath = Path.Combine(outputDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content, ct);
        }
    }

    internal static object BuildModel(SolutionSpec spec) => new
    {
        app_name = spec.ApplicationName,
        root_namespace = spec.RootNamespace,
        database = spec.Database.ToString(),
        use_oracle = spec.Database == DatabaseProvider.Oracle,
        use_sqlserver = spec.Database == DatabaseProvider.SqlServer,
        oracle_package = "Oracle.EntityFrameworkCore",
        oracle_version = "8.23.60",
        sqlserver_package = "Microsoft.EntityFrameworkCore.SqlServer",
        ef_version = "8.0.11",
        swagger_version = "6.9.0",
        versioning_version = "8.1.0",
    };
}
