using System.Text.Json.Serialization;

namespace AppGen.Core.Models;

public sealed class SolutionSpec
{
    public const int CurrentSchemaVersion = 7;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string ApplicationName { get; init; }
    public required string RootNamespace { get; init; }
    public ProjectInfoSpec? Project { get; init; }
    public ProjectPhase Phase { get; init; } = ProjectPhase.Solution;
    public PortalSpec? Portal { get; init; }
    public List<EntitySketch> EntitySketches { get; init; } = [];
    public ApplicationTargets? Targets { get; init; }
    public GenerationMetadata? Generation { get; init; }
    public DatabaseProvider Database { get; init; } = DatabaseProvider.SqlServer;

    [JsonConverter(typeof(UiTargetJsonConverter))]
    public UiTarget UiTargets { get; init; } = UiTarget.None;

    public List<EntitySpec> Entities { get; init; } = [];

    public ProjectSetupSpec Setup { get; init; } = new();

    [JsonIgnore]
    public string ApiProject => $"{ApplicationName}.API";

    [JsonIgnore]
    public string ApplicationProject => $"{ApplicationName}.Application";

    [JsonIgnore]
    public string DomainProject => $"{ApplicationName}.Domain";

    [JsonIgnore]
    public string PersistenceProject => $"{ApplicationName}.Persistence";

    [JsonIgnore]
    public string SharedProject => $"{ApplicationName}.Shared";

    [JsonIgnore]
    public string TestsProject => $"{ApplicationName}.Tests";

    [JsonIgnore]
    public string MvcProject => $"{ApplicationName}.MVC";
}
