namespace AppGen.Core.Models;

public sealed class ConfigEntrySpec
{
    public required string Name { get; init; }
    public ConfigEntryKind Kind { get; init; } = ConfigEntryKind.ConnectionString;
  /// <summary>Appsettings key for AppSetting kind (e.g. Paths:Report). Ignored for connection strings.</summary>
    public string? Key { get; init; }
    public string? Value { get; init; }
}
