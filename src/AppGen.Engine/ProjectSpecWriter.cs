using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class ProjectSpecWriter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(), new UiTargetJsonConverter() }
    };

    public static async Task WriteAsync(SolutionSpec spec, string projectDirectory, CancellationToken ct = default)
    {
        var normalized = SpecNormalizer.Normalize(spec);
        if (normalized.Targets is null)
            normalized = SpecNormalizer.WithTargets(normalized, SpecNormalizer.BuildTargetsFromLegacy(normalized));

        var toWrite = new SolutionSpec
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = normalized.ApplicationName,
            RootNamespace = normalized.RootNamespace,
            Project = normalized.Project,
            Phase = normalized.Phase,
            Portal = normalized.Portal,
            EntitySketches = normalized.EntitySketches,
            Targets = normalized.Targets,
            Generation = normalized.Generation,
            Database = normalized.Database,
            UiTargets = normalized.UiTargets,
            Setup = normalized.Setup,
            Entities = normalized.Entities
        };

        var jsonPath = Path.Combine(projectDirectory, "appgen.json");
        var json = JsonSerializer.Serialize(toWrite, WriteOptions);
        await File.WriteAllTextAsync(jsonPath, json + Environment.NewLine, ct);
    }
}
