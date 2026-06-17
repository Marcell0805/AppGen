using AppGen.Core;

namespace AppGen.UI.Models;

public static class EntityRenameHelper
{
    public static string? TryRename(IList<EntityDraft> entities, EntityDraft entity, string newName)
    {
        var trimmed = newName.Trim();
        if (trimmed.Length == 0)
            return "Entity name cannot be empty.";

        if (trimmed.Equals(entity.Name, StringComparison.OrdinalIgnoreCase))
        {
            entity.Name = trimmed;
            return null;
        }

        if (entities.Any(e => !ReferenceEquals(e, entity) && e.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase)))
            return $"Entity already exists: {trimmed}";

        var oldName = entity.Name;
        var oldKeyName = NamingHelper.ToKeyPropertyName(oldName);
        var newKeyName = NamingHelper.ToKeyPropertyName(trimmed);

        entity.Name = trimmed;

        var keyProperty = entity.Properties.FirstOrDefault(p => p.IsKey);
        if (keyProperty is not null)
            keyProperty.Name = newKeyName;

        foreach (var other in entities)
        {
            if (ReferenceEquals(other, entity))
                continue;

            foreach (var property in other.Properties)
            {
                if (property.ForeignKeyEntity?.Equals(oldName, StringComparison.OrdinalIgnoreCase) != true)
                    continue;

                property.ForeignKeyEntity = trimmed;
                if (property.Name.Equals(oldKeyName, StringComparison.OrdinalIgnoreCase))
                    property.Name = newKeyName;
            }
        }

        return null;
    }
}
