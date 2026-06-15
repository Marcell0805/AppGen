using AppGen.Core.Models;

namespace AppGen.UI.Models;

public sealed class ConfigEntryRow
{
    public string Name { get; set; } = string.Empty;
    public ConfigEntryKind Kind { get; set; } = ConfigEntryKind.ConnectionString;
    public string? Key { get; set; }
    public string? Value { get; set; }

    public ConfigEntrySpec ToSpec() => new()
    {
        Name = Name.Trim(),
        Kind = Kind,
        Key = string.IsNullOrWhiteSpace(Key) ? null : Key.Trim(),
        Value = Value
    };
}
