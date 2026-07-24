using AppGen.Core.Models;
using AppGen.Engine;

namespace AppGen.UI.Services;

public sealed class ManifestSaveService
{
    public async Task<ManifestSaveResult> SaveAsync(
        SolutionSpec wizardSpec,
        string projectDirectory,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return ManifestSaveResult.Fail("Output project folder is invalid.");

        Directory.CreateDirectory(projectDirectory);

        SolutionSpec spec;
        var manifestPath = Path.Combine(projectDirectory, "appgen.json");
        if (File.Exists(manifestPath))
        {
            var existing = await SpecLoader.LoadAsync(projectDirectory, ct);
            spec = new SolutionSpec
            {
                SchemaVersion = SolutionSpec.CurrentSchemaVersion,
                ApplicationName = wizardSpec.ApplicationName,
                RootNamespace = wizardSpec.RootNamespace,
                Project = wizardSpec.Project ?? existing.Project,
                Phase = existing.Phase,
                Portal = existing.Portal,
                EntitySketches = existing.EntitySketches,
                Targets = existing.Targets ?? wizardSpec.Targets,
                Generation = existing.Generation ?? wizardSpec.Generation,
                Database = wizardSpec.Database,
                UiTargets = wizardSpec.UiTargets,
                Setup = wizardSpec.Setup,
                Entities = wizardSpec.Entities
            };
        }
        else
        {
            spec = SpecNormalizer.Normalize(wizardSpec);
        }

        await ProjectSpecWriter.WriteAsync(spec, projectDirectory, ct);
        return ManifestSaveResult.Ok(projectDirectory, spec.Entities.Count);
    }
}

public sealed record ManifestSaveResult(bool Success, string Message, string? ProjectDirectory)
{
    public static ManifestSaveResult Ok(string projectDirectory, int entityCount) =>
        new(true, $"Saved appgen.json with {entityCount} entities.", projectDirectory);

    public static ManifestSaveResult Fail(string message) =>
        new(false, message, null);
}
