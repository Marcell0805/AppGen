using System.Collections.ObjectModel;
using AppGen.Core.Models;

namespace AppGen.UI.Models;

public sealed class EntityDraft
{
    public EntityDraft(string name)
    {
        Name = name;
        Properties.Add(PropertyRow.FromDefaults("Id", "long", isKey: true));
        Properties.Add(PropertyRow.FromDefaults("Name", "string"));
    }

    public string Name { get; set; }
    public ObservableCollection<PropertyRow> Properties { get; } = new();

    public EntityDraft Duplicate(string newName)
    {
        var copy = new EntityDraft(newName);
        copy.Properties.Clear();
        foreach (var p in Properties)
        {
            copy.Properties.Add(new PropertyRow
            {
                Name = p.Name,
                ClrType = p.ClrType,
                IsKey = p.IsKey,
                IsNullable = p.IsNullable
            });
        }
        return copy;
    }

    public EntitySpec ToSpec() => new()
    {
        Name = Name,
        Properties = Properties
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new PropertySpec
            {
                Name = p.Name.Trim(),
                ClrType = p.ClrType.Trim(),
                IsKey = p.IsKey,
                IsNullable = p.IsNullable
            })
            .ToList()
    };
}
