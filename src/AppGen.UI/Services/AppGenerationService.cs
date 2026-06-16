using AppGen.Core.Models;
using AppGen.Engine;

namespace AppGen.UI.Services;

public sealed class AppGenerationService(
    SolutionGenerator solutionGenerator,
    EntityGenerator entityGenerator,
    UiGenerator uiGenerator)
{
    public async Task<GenerationResult> GenerateAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        string outputRootDirectory,
        ProjectSetupSpec setup,
        IReadOnlyList<EntitySpec> entities,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return GenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return GenerationResult.Fail("Output folder is required.");

        var spec = SpecLoader.CreateDefault(applicationName, rootNamespace, database, uiTargets, setup);
        var outputDir = Path.GetFullPath(Path.Combine(outputRootDirectory, spec.ApplicationName));
        Directory.CreateDirectory(outputDir);

        if (Directory.EnumerateFileSystemEntries(outputDir).Any())
            return GenerationResult.Fail($"Output directory is not empty: {outputDir}");

        await solutionGenerator.GenerateAsync(spec, outputDir, ct);
        await AppSettingsGenerator.WriteAsync(spec, outputDir, ct);

        var loadedSpec = await SpecLoader.LoadAsync(outputDir, ct);
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            await entityGenerator.GenerateAsync(loadedSpec, entity, outputDir, ct);
            loadedSpec = await SpecLoader.LoadAsync(outputDir, ct);
            await uiGenerator.GenerateAsync(loadedSpec, entity, outputDir, ct);
        }

        await DatabaseScriptGenerator.WriteAsync(loadedSpec, outputDir, ct);
        await ReadmeGenerator.WriteAsync(loadedSpec, outputDir, ct);

        var uiNote = uiTargets.HasFlag(UiTarget.MvcWeb)
            ? " MVC Web UI included — run the API and MVC projects."
            : string.Empty;

        var scriptNote = loadedSpec.Entities.Count > 0
            ? $" SQL scripts in {DatabaseScriptGenerator.ScriptsFolder(database)}."
            : string.Empty;

        return GenerationResult.Ok(outputDir, $"Generated successfully.{uiNote}{scriptNote}");
    }
}

public sealed record GenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static GenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Generated successfully.", outputDirectory);

    public static GenerationResult Fail(string message) =>
        new(false, message, null);
}
