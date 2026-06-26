using AppGen.Core.Capabilities;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class MobileCapabilityResolver
{
    public static IReadOnlyList<MobileCapabilityDefinition> Resolve(SolutionSpec spec)
    {
        var enabledIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var explicitIds = spec.Targets?.Mobile.Capabilities.Enabled ?? [];
        foreach (var id in explicitIds)
        {
            if (!string.IsNullOrWhiteSpace(id))
                enabledIds.Add(id.Trim());
        }

        if (TargetFlags.OfflineEnabled(spec))
            enabledIds.Add(MobileCapabilityId.OfflineCache);

        if (TargetFlags.AuthEnabled(spec))
        {
            enabledIds.Add(MobileCapabilityId.SecureStorage);
            enabledIds.Add(MobileCapabilityId.JwtAuth);
        }

        var resolved = new List<MobileCapabilityDefinition>();
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var id in enabledIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            Visit(id, resolved, visiting, visited);

        return resolved;
    }

    public static bool Has(SolutionSpec spec, string capabilityId) =>
        Resolve(spec).Any(c => c.Id.Equals(capabilityId, StringComparison.OrdinalIgnoreCase));

    private static void Visit(
        string id,
        List<MobileCapabilityDefinition> resolved,
        HashSet<string> visiting,
        HashSet<string> visited)
    {
        if (visited.Contains(id))
            return;

        if (!visiting.Add(id))
            return;

        var definition = MobileCapabilityCatalog.TryGet(id);
        if (definition is null)
        {
            visiting.Remove(id);
            return;
        }

        foreach (var dep in definition.DependsOn)
            Visit(dep, resolved, visiting, visited);

        if (!resolved.Any(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            resolved.Add(definition);

        visiting.Remove(id);
        visited.Add(id);
    }
}
