using System.Globalization;
using System.Text;
using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

internal static class ImportedSeedScriptBuilder
{
    public static bool HasImportedData(IReadOnlyDictionary<string, List<Dictionary<string, string>>>? entityData) =>
        entityData is not null && entityData.Values.Any(rows => rows.Any(WorkbookSeedDataStore.HasMeaningfulValues));

    public static string BuildSqlServer(
        SolutionSpec spec,
        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Seed data from AppGen spec workbook (imported rows + defaults for empty sheets).");
        sb.AppendLine();

        var entitySeedIds = SeedScriptHelpers.AssignEntitySeedIds(spec);

        foreach (var entity in spec.Entities)
        {
            var rows = GetMeaningfulRows(entityData, entity.Name);
            if (rows.Count > 0)
                AppendImportedSqlServerRows(sb, spec, entity, rows);
            else
                AppendSqlServerSampleRow(sb, spec, entity, entitySeedIds);
        }

        return sb.ToString();
    }

    public static string BuildPostgreSql(
        SolutionSpec spec,
        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Seed data from AppGen spec workbook (imported rows + defaults for empty sheets).");
        sb.AppendLine();

        var entitySeedIds = SeedScriptHelpers.AssignEntitySeedIds(spec);

        foreach (var entity in spec.Entities)
        {
            var rows = GetMeaningfulRows(entityData, entity.Name);
            if (rows.Count > 0)
                AppendImportedPostgreSqlRows(sb, spec, entity, rows);
            else
                AppendPostgreSqlSampleRow(sb, spec, entity, entitySeedIds);
        }

        return sb.ToString();
    }

    public static string BuildOracle(
        SolutionSpec spec,
        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData,
        string schemaPrefix)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- Seed data from AppGen spec workbook (imported rows + defaults for empty sheets).");
        sb.AppendLine();

        var entitySeedIds = SeedScriptHelpers.AssignEntitySeedIds(spec);

        foreach (var entity in spec.Entities)
        {
            var rows = GetMeaningfulRows(entityData, entity.Name);
            if (rows.Count > 0)
                AppendImportedOracleRows(sb, spec, entity, rows, schemaPrefix);
            else
                AppendOracleSampleRow(sb, spec, entity, entitySeedIds, schemaPrefix);
        }

        return sb.ToString();
    }

    private static List<Dictionary<string, string>> GetMeaningfulRows(
        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData,
        string entityName)
    {
        if (!entityData.TryGetValue(entityName, out var rows))
        {
            var match = entityData.FirstOrDefault(kvp =>
                kvp.Key.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            rows = string.IsNullOrEmpty(match.Key) ? [] : match.Value;
        }

        return rows.Where(WorkbookSeedDataStore.HasMeaningfulValues).ToList();
    }

    private static void AppendImportedSqlServerRows(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyList<Dictionary<string, string>> rows)
    {
        var table = $"[{SqlScriptHelpers.SchemaOrDefault(entity, spec.Database)}].[{SqlScriptHelpers.TableName(entity)}]";
        var props = EntityGenerator.ExpandEntityProperties(entity);
        var key = props.FirstOrDefault(p => p.IsKey);
        var hasIdentity = key is not null && SqlScriptHelpers.IsAutoIncrementKey(props, key);

        foreach (var row in rows)
        {
            var columns = string.Join(", ", props.Select(p => $"[{ColumnName(p)}]"));
            var values = string.Join(", ", props.Select(p => FormatSqlServerValue(entity, p, spec, row)));

            if (hasIdentity)
                sb.AppendLine($"SET IDENTITY_INSERT {table} ON;");

            sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");

            if (hasIdentity)
                sb.AppendLine($"SET IDENTITY_INSERT {table} OFF;");

            sb.AppendLine();
        }
    }

    private static void AppendImportedPostgreSqlRows(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyList<Dictionary<string, string>> rows)
    {
        var table = $"\"{SqlScriptHelpers.SchemaOrDefault(entity, spec.Database)}\".\"{SqlScriptHelpers.TableName(entity)}\"";
        var props = EntityGenerator.ExpandEntityProperties(entity);

        foreach (var row in rows)
        {
            var columns = string.Join(", ", props.Select(p => $"\"{ColumnName(p)}\""));
            var values = string.Join(", ", props.Select(p => FormatPostgreSqlValue(entity, p, spec, row)));
            sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");
            sb.AppendLine();
        }
    }

    private static void AppendImportedOracleRows(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyList<Dictionary<string, string>> rows,
        string schemaPrefix)
    {
        var table = $"{schemaPrefix}{SqlScriptHelpers.TableName(entity)}";
        var props = EntityGenerator.ExpandEntityProperties(entity);

        foreach (var row in rows)
        {
            var columns = string.Join(", ", props.Select(p => ColumnName(p)));
            var values = string.Join(", ", props.Select(p => FormatOracleValue(entity, p, spec, row)));
            sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");
            sb.AppendLine();
        }
    }

    private static void AppendSqlServerSampleRow(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        var table = $"[{SqlScriptHelpers.SchemaOrDefault(entity, spec.Database)}].[{SqlScriptHelpers.TableName(entity)}]";
        var props = EntityGenerator.ExpandEntityProperties(entity);
        var key = props.FirstOrDefault(p => p.IsKey);
        var hasIdentity = key is not null && SqlScriptHelpers.IsAutoIncrementKey(props, key);
        var columns = string.Join(", ", props.Select(p => $"[{ColumnName(p)}]"));
        var values = string.Join(", ", props.Select(p => FormatSqlServerSampleValue(entity, p, spec, entitySeedIds)));

        if (hasIdentity)
            sb.AppendLine($"SET IDENTITY_INSERT {table} ON;");

        sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");

        if (hasIdentity)
            sb.AppendLine($"SET IDENTITY_INSERT {table} OFF;");

        sb.AppendLine();
    }

    private static void AppendPostgreSqlSampleRow(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        var table = $"\"{SqlScriptHelpers.SchemaOrDefault(entity, spec.Database)}\".\"{SqlScriptHelpers.TableName(entity)}\"";
        var props = EntityGenerator.ExpandEntityProperties(entity);
        var columns = string.Join(", ", props.Select(p => $"\"{ColumnName(p)}\""));
        var values = string.Join(", ", props.Select(p => FormatPostgreSqlSampleValue(entity, p, spec, entitySeedIds)));
        sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");
        sb.AppendLine();
    }

    private static void AppendOracleSampleRow(
        StringBuilder sb,
        SolutionSpec spec,
        EntitySpec entity,
        IReadOnlyDictionary<string, long> entitySeedIds,
        string schemaPrefix)
    {
        var table = $"{schemaPrefix}{SqlScriptHelpers.TableName(entity)}";
        var props = EntityGenerator.ExpandEntityProperties(entity);
        var columns = string.Join(", ", props.Select(p => ColumnName(p)));
        var values = string.Join(", ", props.Select(p => FormatOracleSampleValue(entity, p, spec, entitySeedIds)));
        sb.AppendLine($"INSERT INTO {table} ({columns}) VALUES ({values});");
        sb.AppendLine();
    }

    private static string ColumnName(PropertySpec p) =>
        SqlScriptHelpers.ColumnName(p, DatabaseProvider.SqlServer);

    private static string FormatSqlServerSampleValue(
        EntitySpec entity,
        PropertySpec p,
        SolutionSpec spec,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        if (p.IsKey)
            return SeedScriptHelpers.KeySampleValue(entity, entitySeedIds);

        var fkValue = SeedScriptHelpers.ForeignKeySampleValue(spec, p, entitySeedIds);
        if (fkValue is not null)
            return fkValue;

        return p.ClrType switch
        {
            "string" => $"N'{p.Name.Replace("'", "''", StringComparison.Ordinal)} sample'",
            "int" or "long" => "1",
            "decimal" or "double" => "0",
            "bool" => "0",
            "DateTime" => "SYSUTCDATETIME()",
            "DateOnly" => "CAST(SYSUTCDATETIME() AS DATE)",
            "Guid" => "NEWID()",
            _ => "NULL"
        };
    }

    private static string FormatPostgreSqlSampleValue(
        EntitySpec entity,
        PropertySpec p,
        SolutionSpec spec,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        if (p.IsKey)
            return SeedScriptHelpers.KeySampleValue(entity, entitySeedIds);

        var fkValue = SeedScriptHelpers.ForeignKeySampleValue(spec, p, entitySeedIds);
        if (fkValue is not null)
            return fkValue;

        return p.ClrType switch
        {
            "string" => $"'{p.Name.Replace("'", "''", StringComparison.Ordinal)} sample'",
            "int" or "long" => "1",
            "decimal" or "double" => "0",
            "bool" => "FALSE",
            "DateTime" => "NOW()",
            "DateOnly" => "CURRENT_DATE",
            "Guid" => "gen_random_uuid()",
            _ => "NULL"
        };
    }

    private static string FormatOracleSampleValue(
        EntitySpec entity,
        PropertySpec p,
        SolutionSpec spec,
        IReadOnlyDictionary<string, long> entitySeedIds)
    {
        if (p.IsKey)
            return SeedScriptHelpers.KeySampleValue(entity, entitySeedIds);

        var fkValue = SeedScriptHelpers.ForeignKeySampleValue(spec, p, entitySeedIds);
        if (fkValue is not null)
            return fkValue;

        return p.ClrType switch
        {
            "string" => $"'{p.Name.Replace("'", "''", StringComparison.Ordinal)} sample'",
            "int" or "long" => "1",
            "decimal" or "double" => "0",
            "bool" => "0",
            "DateTime" => "SYSTIMESTAMP",
            "DateOnly" => "TRUNC(SYSDATE)",
            "Guid" => "SYS_GUID()",
            _ => "NULL"
        };
    }

    private static string FormatSqlServerValue(
        EntitySpec entity,
        PropertySpec property,
        SolutionSpec spec,
        IReadOnlyDictionary<string, string> row)
    {
        if (!TryGetRowValue(row, property.Name, out var raw) || string.IsNullOrWhiteSpace(raw))
            return property.IsNullable ? "NULL" : DefaultSqlServerLiteral(property.ClrType);

        if (property.IsKey && long.TryParse(raw, out _))
            return raw;

        var fk = SqlScriptHelpers.ResolveForeignKeyEntity(spec, property);
        if (fk is not null && long.TryParse(raw, out _))
            return raw;

        return property.ClrType switch
        {
            "string" => $"N'{raw.Replace("'", "''", StringComparison.Ordinal)}'",
            "int" or "long" => long.TryParse(raw, out _) ? raw : "0",
            "decimal" or "double" => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ? raw : "0",
            "bool" => raw is "1" or "true" or "yes" or "y" ? "1" : "0",
            "DateTime" => DateTime.TryParse(raw, out var dt) ? $"'{dt:yyyy-MM-dd HH:mm:ss}'" : "SYSUTCDATETIME()",
            "DateOnly" => DateOnly.TryParse(raw, out var d) ? $"'{d:yyyy-MM-dd}'" : "CAST(SYSUTCDATETIME() AS DATE)",
            "Guid" => Guid.TryParse(raw, out var g) ? $"'{g}'" : "NEWID()",
            _ => $"N'{raw.Replace("'", "''", StringComparison.Ordinal)}'"
        };
    }

    private static string FormatPostgreSqlValue(
        EntitySpec entity,
        PropertySpec property,
        SolutionSpec spec,
        IReadOnlyDictionary<string, string> row)
    {
        if (!TryGetRowValue(row, property.Name, out var raw) || string.IsNullOrWhiteSpace(raw))
            return property.IsNullable ? "NULL" : DefaultPostgreSqlLiteral(property.ClrType);

        return property.ClrType switch
        {
            "string" => $"'{raw.Replace("'", "''", StringComparison.Ordinal)}'",
            "int" or "long" => long.TryParse(raw, out _) ? raw : "0",
            "decimal" or "double" => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ? raw : "0",
            "bool" => raw is "1" or "true" or "yes" or "y" ? "TRUE" : "FALSE",
            "DateTime" => DateTime.TryParse(raw, out var dt) ? $"'{dt:yyyy-MM-dd HH:mm:ss}'" : "NOW()",
            "DateOnly" => DateOnly.TryParse(raw, out var d) ? $"'{d:yyyy-MM-dd}'" : "CURRENT_DATE",
            "Guid" => Guid.TryParse(raw, out var g) ? $"'{g}'" : "gen_random_uuid()",
            _ => $"'{raw.Replace("'", "''", StringComparison.Ordinal)}'"
        };
    }

    private static string FormatOracleValue(
        EntitySpec entity,
        PropertySpec property,
        SolutionSpec spec,
        IReadOnlyDictionary<string, string> row)
    {
        if (!TryGetRowValue(row, property.Name, out var raw) || string.IsNullOrWhiteSpace(raw))
            return property.IsNullable ? "NULL" : DefaultOracleLiteral(property.ClrType);

        return property.ClrType switch
        {
            "string" => $"'{raw.Replace("'", "''", StringComparison.Ordinal)}'",
            "int" or "long" => long.TryParse(raw, out _) ? raw : "0",
            "decimal" or "double" => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ? raw : "0",
            "bool" => raw is "1" or "true" or "yes" or "y" ? "1" : "0",
            "DateTime" => DateTime.TryParse(raw, out var dt) ? $"TO_TIMESTAMP('{dt:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')" : "SYSTIMESTAMP",
            "DateOnly" => DateOnly.TryParse(raw, out var d) ? $"DATE '{d:yyyy-MM-dd}'" : "TRUNC(SYSDATE)",
            "Guid" => Guid.TryParse(raw, out var g) ? $"'{g}'" : "SYS_GUID()",
            _ => $"'{raw.Replace("'", "''", StringComparison.Ordinal)}'"
        };
    }

    private static bool TryGetRowValue(IReadOnlyDictionary<string, string> row, string propertyName, out string value)
    {
        if (row.TryGetValue(propertyName, out value!))
            return true;

        var match = row.FirstOrDefault(kvp =>
            kvp.Key.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(match.Key))
        {
            value = match.Value;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static string DefaultSqlServerLiteral(string clrType) => clrType switch
    {
        "string" => "N''",
        "bool" => "0",
        "DateTime" => "SYSUTCDATETIME()",
        "DateOnly" => "CAST(SYSUTCDATETIME() AS DATE)",
        "Guid" => "NEWID()",
        _ => "0"
    };

    private static string DefaultPostgreSqlLiteral(string clrType) => clrType switch
    {
        "string" => "''",
        "bool" => "FALSE",
        "DateTime" => "NOW()",
        "DateOnly" => "CURRENT_DATE",
        "Guid" => "gen_random_uuid()",
        _ => "0"
    };

    private static string DefaultOracleLiteral(string clrType) => clrType switch
    {
        "string" => "''",
        "bool" => "0",
        "DateTime" => "SYSTIMESTAMP",
        "DateOnly" => "TRUNC(SYSDATE)",
        "Guid" => "SYS_GUID()",
        _ => "0"
    };
}
