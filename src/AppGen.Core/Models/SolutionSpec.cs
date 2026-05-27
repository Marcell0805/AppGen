using System.Text.Json.Serialization;

namespace AppGen.Core.Models;

public sealed class SolutionSpec
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public required string ApplicationName { get; init; }
    public required string RootNamespace { get; init; }
    public DatabaseProvider Database { get; init; } = DatabaseProvider.SqlServer;
    public List<EntitySpec> Entities { get; init; } = [];

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
}
