using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed class ProjectPromoter(
    SolutionGenerator solutionGenerator,
    EntityGenerator entityGenerator,
    UiGenerator uiGenerator)
{
    public async Task<PromoteResult> PromoteAsync(
        string projectDirectory,
        bool force = false,
        CancellationToken ct = default)
    {
        var spec = await SpecLoader.LoadAsync(projectDirectory, ct);
        var srcDir = Path.Combine(projectDirectory, "src");
        var slnPath = Path.Combine(projectDirectory, $"{spec.ApplicationName}.sln");

        if (File.Exists(slnPath) || Directory.Exists(srcDir))
        {
            if (!force)
                return PromoteResult.Fail($"Solution already exists at {projectDirectory}. Use force to regenerate src/.");
        }

        if (force)
        {
            if (Directory.Exists(srcDir))
                Directory.Delete(srcDir, recursive: true);
            if (File.Exists(slnPath))
                File.Delete(slnPath);
            foreach (var file in Directory.EnumerateFiles(projectDirectory, $"{spec.ApplicationName}.slnLaunch"))
                File.Delete(file);
        }

        var entitiesToGenerate = spec.Entities.ToList();
        var solutionSpec = new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
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

        await solutionGenerator.GenerateAsync(solutionSpec, projectDirectory, ct);
        await AppSettingsGenerator.WriteAsync(solutionSpec, projectDirectory, ct);
        await ProjectSpecWriter.WriteAsync(solutionSpec, projectDirectory, ct);

        var loaded = await SpecLoader.LoadAsync(projectDirectory, ct);
        foreach (var entity in entitiesToGenerate)
        {
            ct.ThrowIfCancellationRequested();
            await entityGenerator.GenerateAsync(loaded, entity, projectDirectory, ct);
            loaded = await SpecLoader.LoadAsync(projectDirectory, ct);
            if (loaded.UiTargets.HasFlag(UiTarget.MvcWeb))
                await uiGenerator.GenerateAsync(loaded, entity, projectDirectory, ct);
        }

        loaded = await SpecLoader.LoadAsync(projectDirectory, ct);
        await DatabaseScriptGenerator.WriteAsync(loaded, projectDirectory, ct);
        await ReadmeGenerator.WriteAsync(loaded, projectDirectory, ct);

        var updated = new SolutionSpec
        {
            SchemaVersion = loaded.SchemaVersion,
            ApplicationName = loaded.ApplicationName,
            RootNamespace = loaded.RootNamespace,
            Phase = spec.Portal is not null ? ProjectPhase.Both : ProjectPhase.Solution,
            Portal = loaded.Portal,
            EntitySketches = loaded.EntitySketches,
            Targets = loaded.Targets ?? spec.Targets,
            Generation = loaded.Generation ?? spec.Generation,
            Database = loaded.Database,
            UiTargets = loaded.UiTargets,
            Setup = loaded.Setup,
            Entities = loaded.Entities
        };
        await ProjectSpecWriter.WriteAsync(updated, projectDirectory, ct);

        return PromoteResult.Ok(projectDirectory, $"Promoted to API solution with {loaded.Entities.Count} entities.");
    }
}

public sealed record PromoteResult(bool Success, string Message, string? ProjectDirectory)
{
    public static PromoteResult Ok(string projectDirectory, string? message = null) =>
        new(true, message ?? "Promoted successfully.", projectDirectory);

    public static PromoteResult Fail(string message) =>
        new(false, message, null);
}
