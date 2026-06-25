using AppGen.Core;
using AppGen.UI.Models;

namespace AppGen.UI.Services;

public static class EntityValidation
{
    public static string? ValidateEntities(IReadOnlyList<EntityDraft> entities)
    {
        var allNames = entities.Select(e => e.Name).ToList();

        foreach (var entity in entities)
        {
            if (!NamingHelper.IsValidClrIdentifier(entity.Name))
                return $"Entity '{entity.Name}' is not a valid C# name (use letters, digits, underscore; cannot start with a digit).";

            var names = entity.Properties
                .Select(p => p.Name.Trim())
                .Where(n => n.Length > 0)
                .ToList();

            if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
                return $"Duplicate property names on entity {entity.Name}.";

            var keyCount = entity.Properties.Count(p => p.IsKey);
            if (keyCount != 1)
                return $"Entity {entity.Name} must have exactly one key property.";

            if (names.Count == 0)
                return $"Entity {entity.Name} must have at least one property.";

            foreach (var propertyName in names)
            {
                if (!NamingHelper.IsValidClrIdentifier(propertyName))
                    return $"Property '{propertyName}' on entity {entity.Name} is not a valid C# name (use letters, digits, underscore; cannot start with a digit).";
            }

            var spec = entity.ToSpec(allNames);
            foreach (var property in spec.Properties.Where(p => p.ForeignKeyEntity is not null))
            {
                if (!allNames.Any(n => n.Equals(property.ForeignKeyEntity, StringComparison.OrdinalIgnoreCase)))
                    return $"Entity {entity.Name}: FK '{property.Name}' references unknown entity '{property.ForeignKeyEntity}'.";
            }
        }

        return null;
    }
}
