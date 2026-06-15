namespace AppGen.Core.Models;

public sealed class ProjectSetupSpec
{
    public List<ConfigEntrySpec> ConfigEntries { get; init; } = [];
    public string ActiveConnectionName { get; init; } = "Dev";
    public bool EnsureCreatedInDevelopment { get; init; } = true;
    /// <summary>Oracle schema prefix for SE.SE_LOT style tables (schema = prefix, table = PREFIX_ENTITY).</summary>
    public string? OracleSchemaPrefix { get; init; }
}
