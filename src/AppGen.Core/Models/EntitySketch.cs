namespace AppGen.Core.Models;

public sealed class EntitySketch
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Phase { get; init; }
    public string Status { get; init; } = "planned";
}
