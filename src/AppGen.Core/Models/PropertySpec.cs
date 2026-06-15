namespace AppGen.Core.Models;

public sealed class PropertySpec
{
    public required string Name { get; init; }
    public required string ClrType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsKey { get; init; }
    public string? ColumnName { get; init; }
    /// <summary>Name of the entity this property references (FK).</summary>
    public string? ForeignKeyEntity { get; init; }
}
