namespace AppGen.Core.Capabilities;

public sealed class MobileCapabilityDefinition
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public bool IsImplemented { get; init; }
    public IReadOnlyList<string> DependsOn { get; init; } = [];
    public IReadOnlyList<PubspecPackage> PubspecPackages { get; init; } = [];
    public IReadOnlyList<string> AndroidPermissions { get; init; } = [];
    public IReadOnlyList<string> IosPlistKeys { get; init; } = [];
    public string? ServiceTemplate { get; init; }
    public string? ServiceFileName { get; init; }

    public sealed class PubspecPackage
    {
        public required string Name { get; init; }
        public required string Version { get; init; }
    }
}
