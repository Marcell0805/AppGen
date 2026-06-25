using AppGen.Core.Models;
using AppGen.Engine;

namespace AppGen.UI.Services;

public sealed class AppGenerationService(
    SolutionGenerator solutionGenerator,
    EntityGenerator entityGenerator,
    UiGenerator uiGenerator)
{
    public Task<GenerationResult> GenerateAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        string outputRootDirectory,
        ProjectSetupSpec setup,
        IReadOnlyList<EntitySpec> entities,
        bool overwrite = false,
        CancellationToken ct = default) =>
        GenerateCoreAsync(
            applicationName,
            rootNamespace,
            database,
            uiTargets,
            outputRootDirectory,
            setup,
            entities,
            overwrite,
            ct);

    public Task<GenerationResult> RegenerateAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        string outputRootDirectory,
        ProjectSetupSpec setup,
        IReadOnlyList<EntitySpec> entities,
        CancellationToken ct = default) =>
        GenerateCoreAsync(
            applicationName,
            rootNamespace,
            database,
            uiTargets,
            outputRootDirectory,
            setup,
            entities,
            overwrite: true,
            ct);

    private async Task<GenerationResult> GenerateCoreAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        string outputRootDirectory,
        ProjectSetupSpec setup,
        IReadOnlyList<EntitySpec> entities,
        bool overwrite,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return GenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return GenerationResult.Fail("Output folder is required.");

        var spec = SpecLoader.CreateDefault(applicationName, rootNamespace, database, uiTargets, setup);
        var outputDir = GenerationOutputHelper.ResolveLayerDirectory(
            outputRootDirectory.Trim(),
            spec.ApplicationName,
            ProjectOutputLayer.Web);
        var exists = GenerationOutputHelper.OutputDirectoryExists(outputDir);

        if (exists && !overwrite)
            return GenerationResult.Fail($"Output directory is not empty: {outputDir}");

        if (overwrite && exists)
            GenerationOutputHelper.DeleteOutputDirectory(outputDir);

        Directory.CreateDirectory(outputDir);

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

        var prefix = overwrite && exists ? "Regenerated successfully." : "Generated successfully.";
        return GenerationResult.Ok(outputDir, $"{prefix}{uiNote}{scriptNote}");
    }
}

public sealed record GenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static GenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Generated successfully.", outputDirectory);

    public static GenerationResult Fail(string message) =>
        new(false, message, null);
}
