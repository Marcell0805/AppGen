namespace AppGen.Core.Models;

public sealed class EntitySpec
{
    public required string Name { get; init; }
    public string? TableName { get; init; }
    public string? Schema { get; init; }
    public List<PropertySpec> Properties { get; init; } = [];
}
