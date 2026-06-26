using AppGen.UI.Models;

namespace AppGen.UI.Services;

public static class WizardEntityHelper
{
    public static List<EntityDraft> ToEntityDrafts(IEnumerable<WizardDraft.EntityDraftData> entities)
    {
        var list = new List<EntityDraft>();
        foreach (var entityData in entities)
        {
            var entity = new EntityDraft(entityData.Name)
            {
                IncludeInUi = entityData.IncludeInUi,
                IncludeAuditColumns = entityData.IncludeAuditColumns
            };
            entity.Properties.Clear();
            foreach (var property in entityData.Properties)
            {
                entity.Properties.Add(new PropertyRow
                {
                    Name = property.Name,
                    ClrType = property.ClrType,
                    IsKey = property.IsKey,
                    IsNullable = property.IsNullable,
                    ForeignKeyEntity = property.ForeignKeyEntity
                });
            }
            list.Add(entity);
        }
        return list;
    }
}
