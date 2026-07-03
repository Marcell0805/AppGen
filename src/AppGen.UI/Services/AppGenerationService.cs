using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Engine;

namespace AppGen.UI.Services;

public sealed class AppGenerationService(
    SolutionGenerator solutionGenerator,
    EntityGenerator entityGenerator,
    UiGenerator uiGenerator)
{
    public Task<GenerationResult> GenerateFromSpecAsync(
        SolutionSpec spec,
        string outputRootDirectory,
        CancellationToken ct = default) =>
        GenerateCoreFromSpecAsync(spec, outputRootDirectory, overwrite: false, ct);

    public Task<GenerationResult> RegenerateFromSpecAsync(
        SolutionSpec spec,
        string outputRootDirectory,
        CancellationToken ct = default) =>
        GenerateCoreFromSpecAsync(spec, outputRootDirectory, overwrite: true, ct);

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
            mobileThemePreset: null,
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
            mobileThemePreset: null,
            overwrite: true,
            ct);

    private async Task<GenerationResult> GenerateCoreFromSpecAsync(
        SolutionSpec sourceSpec,
        string outputRootDirectory,
        bool overwrite,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sourceSpec.ApplicationName))
            return GenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return GenerationResult.Fail("Output folder is required.");

        var spec = new SolutionSpec
        {
            SchemaVersion = sourceSpec.SchemaVersion,
            ApplicationName = sourceSpec.ApplicationName,
            RootNamespace = sourceSpec.RootNamespace,
            Project = sourceSpec.Project,
            Phase = ProjectPhase.Solution,
            Portal = sourceSpec.Portal,
            EntitySketches = sourceSpec.EntitySketches,
            Targets = sourceSpec.Targets,
            Generation = sourceSpec.Generation,
            Database = sourceSpec.Database,
            UiTargets = sourceSpec.UiTargets,
            Setup = sourceSpec.Setup,
            Entities = sourceSpec.Entities.ToList()
        };

        return await GenerateOutputAsync(spec, outputRootDirectory, overwrite, ct);
    }

    private async Task<GenerationResult> GenerateCoreAsync(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        string outputRootDirectory,
        ProjectSetupSpec setup,
        IReadOnlyList<EntitySpec> entities,
        string? mobileThemePreset,
        bool overwrite,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            return GenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return GenerationResult.Fail("Output folder is required.");

        var spec = SpecLoader.CreateDefault(applicationName, rootNamespace, database, uiTargets, setup);
        spec = new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = ProjectPhase.Solution,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = entities.ToList()
        };

        if (!string.IsNullOrWhiteSpace(mobileThemePreset))
            spec = MobileTargetMerger.ApplyAppThemePreset(spec, mobileThemePreset);

        return await GenerateOutputAsync(spec, outputRootDirectory, overwrite, ct);
    }

    private async Task<GenerationResult> GenerateOutputAsync(
        SolutionSpec spec,
        string outputRootDirectory,
        bool overwrite,
        CancellationToken ct)
    {
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
        foreach (var entity in spec.Entities)
        {
            ct.ThrowIfCancellationRequested();
            await entityGenerator.GenerateAsync(loadedSpec, entity, outputDir, ct);
            loadedSpec = await SpecLoader.LoadAsync(outputDir, ct);
            await uiGenerator.GenerateAsync(loadedSpec, entity, outputDir, ct);
        }

        await DatabaseScriptGenerator.WriteAsync(loadedSpec, outputDir, ct);
        await ReadmeGenerator.WriteAsync(loadedSpec, outputDir, ct);

        var uiNote = spec.UiTargets.HasFlag(UiTarget.MvcWeb)
            ? " MVC Web UI included — run the API and MVC projects."
            : string.Empty;

        var scriptNote = loadedSpec.Entities.Count > 0
            ? $" SQL scripts in {DatabaseScriptGenerator.ScriptsFolder(spec.Database)}."
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
