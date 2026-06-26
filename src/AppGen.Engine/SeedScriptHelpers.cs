using AppGen.Core.Models;

namespace AppGen.Engine;

internal static class SeedScriptHelpers
{
    /// <summary>
    /// Assigns one sample primary-key value per entity (1, 2, 3, …) in definition order.
    /// </summary>
    public static Dictionary<string, long> AssignEntitySeedIds(SolutionSpec spec)
    {
        var ids = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long next = 1;
        foreach (var entity in spec.Entities)
            ids[entity.Name] = next++;
        return ids;
    }

    public static string KeySampleValue(EntitySpec entity, IReadOnlyDictionary<string, long> entitySeedIds) =>
        entitySeedIds[entity.Name].ToString();

    public static string? ForeignKeySampleValue(
        SolutionSpec spec,
        PropertySpec property,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        var refEntity = SqlScriptHelpers.ResolveForeignKeyEntity(spec, property);
        if (refEntity is null)
            return null;

        return entitySeedIds[refEntity.Name].ToString();
    }
}
