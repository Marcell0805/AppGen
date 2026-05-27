using AppGen.Core.Models;
using AppGen.Engine;

namespace AppGen.UI.Services;

public sealed class AppGenerationService(
    SolutionGenerator solutionGenerator,
    EntityGenerator entityGenerator)
{
    public async Task<GenerationResult> GenerateAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        string outputRootDirectory,
        IReadOnlyList<EntitySpec> entities,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return GenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return GenerationResult.Fail("Output folder is required.");

        var spec = SpecLoader.CreateDefault(applicationName, rootNamespace, database);
        var outputDir = Path.GetFullPath(Path.Combine(outputRootDirectory, spec.ApplicationName));
        Directory.CreateDirectory(outputDir);

        if (Directory.EnumerateFileSystemEntries(outputDir).Any())
            return GenerationResult.Fail($"Output directory is not empty: {outputDir}");

        await solutionGenerator.GenerateAsync(spec, outputDir, ct);

        var loadedSpec = await SpecLoader.LoadAsync(outputDir, ct);
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            await entityGenerator.GenerateAsync(loadedSpec, entity, outputDir, ct);
            loadedSpec = await SpecLoader.LoadAsync(outputDir, ct);
        }

        return GenerationResult.Ok(outputDir);
    }
}

public sealed record GenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static GenerationResult Ok(string outputDirectory) =>
        new(true, "Generated successfully.", outputDirectory);

    public static GenerationResult Fail(string message) =>
        new(false, message, null);
}

