using AppGen.Core.Models;

namespace AppGen.Core;

public static class NamingHelper
{
    public static string NormalizeAppName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

        var name = string.Join("_", rawName.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var filtered = new string(name
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray());

        if (filtered.Length > 0 && char.IsDigit(filtered[0]))
            filtered = "_" + filtered;

        while (filtered.Contains("__", StringComparison.Ordinal))
            filtered = filtered.Replace("__", "_", StringComparison.Ordinal);

        return filtered;
    }

    public static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    public static string ToKeyPropertyName(string entityName) =>
        $"{entityName}_Id";

    public static string ToOracleTableName(string schemaPrefix, string entityName)
    {
        var schema = schemaPrefix.Trim().ToUpperInvariant();
        var table = $"{schema}_{entityName.ToUpperInvariant()}";
        return $"{schema}.{table}";
    }

    public static string ToOracleTableNameOnly(string schemaPrefix, string entityName) =>
        $"{schemaPrefix.Trim().ToUpperInvariant()}_{entityName.ToUpperInvariant()}";

    public static string ToOracleSchema(string schemaPrefix) =>
        schemaPrefix.Trim().ToUpperInvariant();

    /// <summary>Oracle column identifier (unquoted uppercase, matches generated SQL scripts).</summary>
    public static string ToOracleColumnName(string propertyName) =>
        propertyName.ToUpperInvariant();

    public static ConfigEntrySpec DefaultDevConnection(DatabaseProvider database) => new()
    {
        Name = "Dev",
        Kind = ConfigEntryKind.ConnectionString,
        Value = database switch
        {
            DatabaseProvider.Oracle => "User Id=app;Password=secret;Data Source=localhost:1521/XEPDB1",
            DatabaseProvider.PostgreSql => "Host=localhost;Port=5432;Database=AppGenDb;Username=postgres;Password=postgres",
            _ => "Server=localhost;Database=AppGenDb;Trusted_Connection=True;TrustServerCertificate=True"
        }
    };

    public static ProjectSetupSpec DefaultSetup(DatabaseProvider database) => new()
    {
        ConfigEntries = [DefaultDevConnection(database)],
        ActiveConnectionName = "Dev",
        EnsureCreatedInDevelopment = true,
        OracleSchemaPrefix = database == DatabaseProvider.Oracle ? "SE" : null
    };

    public static string? InferForeignKeyEntity(
        string propertyName,
        string currentEntityName,
        IEnumerable<string> entityNames)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return null;

        var ownKey = ToKeyPropertyName(currentEntityName);
        if (propertyName.Equals(ownKey, StringComparison.OrdinalIgnoreCase))
            return null;

        var names = entityNames
            .Where(n => !n.Equals(currentEntityName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var entity in names)
        {
            if (propertyName.Equals(ToKeyPropertyName(entity), StringComparison.OrdinalIgnoreCase))
                return entity;
        }

        if (!propertyName.EndsWith("_Id", StringComparison.Ordinal))
            return null;

        var stem = propertyName[..^3];
        return names.FirstOrDefault(e =>
            e.Equals(stem, StringComparison.OrdinalIgnoreCase) ||
            e.Equals(stem + "s", StringComparison.OrdinalIgnoreCase) ||
            (e.EndsWith('s') && e[..^1].Equals(stem, StringComparison.OrdinalIgnoreCase)));
    }
}
