using AppGen.Core.Models;

namespace AppGen.Engine;

public static class DatabaseScriptGenerator
{
    public static Task WriteAsync(SolutionSpec spec, string outputDirectory, CancellationToken ct = default) =>
        WriteAsync(spec, outputDirectory, entityData: null, ct);

    public static Task WriteAsync(
        SolutionSpec spec,
        string outputDirectory,
        IReadOnlyDictionary<string, List<Dictionary<string, string>>>? entityData,
        CancellationToken ct = default)
    {
        entityData ??= WorkbookSeedDataStore.TryLoad(
            WorkbookSeedDataStore.ResolveHubDirectory(outputDirectory, spec.ApplicationName));

        return spec.Database switch
        {
            DatabaseProvider.Oracle => OracleScriptGenerator.WriteAsync(spec, outputDirectory, entityData, ct),
            DatabaseProvider.SqlServer => SqlServerScriptGenerator.WriteAsync(spec, outputDirectory, entityData, ct),
            DatabaseProvider.PostgreSql => PostgreSqlScriptGenerator.WriteAsync(spec, outputDirectory, entityData, ct),
            _ => Task.CompletedTask
        };
    }

    public static string ScriptsFolder(DatabaseProvider database) => database switch
    {
        DatabaseProvider.Oracle => "scripts/oracle",
        DatabaseProvider.SqlServer => "scripts/sqlserver",
        DatabaseProvider.PostgreSql => "scripts/postgresql",
        _ => "scripts"
    };
}
