using AppGen.Core.Models;

namespace AppGen.Engine;

public static class DatabaseScriptGenerator
{
    public static Task WriteAsync(SolutionSpec spec, string outputDirectory, CancellationToken ct = default) =>
        spec.Database switch
        {
            DatabaseProvider.Oracle => OracleScriptGenerator.WriteAsync(spec, outputDirectory, ct),
            DatabaseProvider.SqlServer => SqlServerScriptGenerator.WriteAsync(spec, outputDirectory, ct),
            DatabaseProvider.PostgreSql => PostgreSqlScriptGenerator.WriteAsync(spec, outputDirectory, ct),
            _ => Task.CompletedTask
        };

    public static string ScriptsFolder(DatabaseProvider database) => database switch
    {
        DatabaseProvider.Oracle => "scripts/oracle",
        DatabaseProvider.SqlServer => "scripts/sqlserver",
        DatabaseProvider.PostgreSql => "scripts/postgresql",
        _ => "scripts"
    };
}
