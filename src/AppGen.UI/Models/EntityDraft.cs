using System.Collections.ObjectModel;
using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.UI.Models;

public sealed class EntityDraft
{
    public EntityDraft(string name)
    {
        Name = name;
        Properties.Add(PropertyRow.FromDefaults(NamingHelper.ToKeyPropertyName(name), "long", isKey: true));
        Properties.Add(PropertyRow.FromDefaults("Name", "string"));
    }

    public string Name { get; set; }
    public bool IncludeInUi { get; set; } = true;
    public bool IncludeAuditColumns { get; set; }
    public ObservableCollection<PropertyRow> Properties { get; } = new();

    public EntityDraft Duplicate(string newName)
    {
        var copy = new EntityDraft(newName);
        copy.Properties.Clear();
        copy.IncludeInUi = IncludeInUi;
        copy.IncludeAuditColumns = IncludeAuditColumns;

        var newKeyName = NamingHelper.ToKeyPropertyName(newName);
        foreach (var p in Properties)
        {
            copy.Properties.Add(new PropertyRow
            {
                Name = p.IsKey ? newKeyName : p.Name,
                ClrType = p.ClrType,
                IsKey = p.IsKey,
                IsNullable = p.IsNullable,
                ForeignKeyEntity = p.ForeignKeyEntity
            });
        }

        return copy;
    }

    public EntitySpec ToSpec(IReadOnlyList<string> allEntityNames) => new()
    {
        Name = Name,
        IncludeInUi = IncludeInUi,
        IncludeAuditColumns = IncludeAuditColumns,
        Properties = Properties
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p =>
            {
                var trimmedName = p.Name.Trim();
                var fk = string.IsNullOrWhiteSpace(p.ForeignKeyEntity)
                    ? NamingHelper.InferForeignKeyEntity(trimmedName, Name, allEntityNames)
                    : p.ForeignKeyEntity.Trim();

                return new PropertySpec
                {
                    Name = trimmedName,
                    ClrType = p.ClrType.Trim(),
                    IsKey = p.IsKey,
                    IsNullable = p.IsNullable,
                    ForeignKeyEntity = string.IsNullOrWhiteSpace(fk) ? null : fk
                };
            })
            .ToList()
    };
}
