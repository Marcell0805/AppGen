using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class SpecNormalizer
{
    public static SolutionSpec Normalize(SolutionSpec spec)
    {
        var targets = spec.Targets ?? BuildTargetsFromLegacy(spec);
        return new SolutionSpec
        {
            SchemaVersion = Math.Max(spec.SchemaVersion, SolutionSpec.CurrentSchemaVersion),
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }

    public static ApplicationTargets BuildTargetsFromLegacy(SolutionSpec spec) => new()
    {
        Documentation = new DocumentationTargetSpec
        {
            Enabled = spec.Portal is not null,
            Preset = spec.Portal?.Preset ?? "engineering-portal"
        },
        Web = new WebTargetSpec
        {
            Enabled = spec.Phase is ProjectPhase.Solution or ProjectPhase.Both
        },
        Mobile = new MobileTargetSpec
        {
            Enabled = false,
            PackageName = $"com.{NamingHelper.NormalizeAppName(spec.ApplicationName).ToLowerInvariant()}.app",
            ApiBaseUrl = "https://localhost:5001"
        }
    };

    public static SolutionSpec WithTargets(SolutionSpec spec, ApplicationTargets targets) =>
        new()
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
}
