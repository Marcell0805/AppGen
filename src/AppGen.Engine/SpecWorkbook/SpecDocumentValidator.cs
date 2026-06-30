using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

public static class SpecDocumentValidator
{
    public static IReadOnlyList<SpecValidationIssue> Validate(AppGenSpecDocument document)
    {
        var issues = new List<SpecValidationIssue>();
        var database = ParseDatabase(document.Application.Database);

        if (string.IsNullOrWhiteSpace(document.Application.ApplicationName))
        {
            issues.Add(Error(SpecWorkbookSheets.Application, null,
                "ApplicationName is required on the Application sheet."));
        }

        var entityNames = document.Entities
            .Select(e => e.EntityName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entity in document.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.EntityName))
            {
                issues.Add(Error(SpecWorkbookSheets.Entities, entity.RowNumber, "EntityName is required."));
                continue;
            }

            if (document.Entities.Count(e =>
                    e.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                issues.Add(Error(SpecWorkbookSheets.Entities, entity.RowNumber,
                    $"Duplicate entity name '{entity.EntityName}'."));
            }
        }

        foreach (var property in document.Properties)
        {
            if (string.IsNullOrWhiteSpace(property.EntityName) || string.IsNullOrWhiteSpace(property.PropertyName))
            {
                issues.Add(Error(SpecWorkbookSheets.Properties, property.RowNumber,
                    "EntityName and PropertyName are required."));
                continue;
            }

            if (!entityNames.Contains(property.EntityName))
            {
                issues.Add(Error(SpecWorkbookSheets.Properties, property.RowNumber,
                    $"Entity '{property.EntityName}' is not defined on the Entities sheet."));
            }

            if (!ClrTypeCatalog.IsAllowed(property.ClrType, database))
            {
                issues.Add(Error(SpecWorkbookSheets.Properties, property.RowNumber,
                    $"ClrType '{property.ClrType}' is not supported for {database}."));
            }

            if (!string.IsNullOrWhiteSpace(property.ForeignKeyEntity) &&
                !entityNames.Contains(property.ForeignKeyEntity))
            {
                issues.Add(Error(SpecWorkbookSheets.Properties, property.RowNumber,
                    $"ForeignKeyEntity '{property.ForeignKeyEntity}' is not defined."));
            }
        }

        foreach (var entity in document.Entities)
        {
            var props = document.Properties
                .Where(p => p.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (props.Count == 0)
            {
                issues.Add(Warning(SpecWorkbookSheets.Entities, entity.RowNumber,
                    $"Entity '{entity.EntityName}' has no properties; a key property will be added automatically."));
                continue;
            }

            var keys = props.Where(p => p.IsKey).ToList();
            if (keys.Count > 1)
            {
                issues.Add(Error(SpecWorkbookSheets.Properties, keys[1].RowNumber,
                    $"Entity '{entity.EntityName}' has more than one key property."));
            }
        }

        foreach (var (entityName, rows) in document.EntityData)
        {
            if (!entityNames.Contains(entityName))
            {
                issues.Add(Warning($"{SpecWorkbookSheets.DataPrefix}{entityName}", null,
                    $"Data sheet references unknown entity '{entityName}'."));
                continue;
            }

            var props = document.Properties
                .Where(p => p.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (props.Count == 0)
                props = BuildDefaultProperties(entityName);

            var sheetName = $"{SpecWorkbookSheets.DataPrefix}{entityName}";
            var keyProps = props.Where(p => p.IsKey).ToList();
            var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNum = i + 2;
                var row = rows[i];
                if (row.Values.All(string.IsNullOrWhiteSpace))
                    continue;

                foreach (var prop in props)
                {
                    if (!row.TryGetValue(prop.PropertyName, out var raw) || string.IsNullOrWhiteSpace(raw))
                    {
                        if (!prop.IsNullable && !prop.IsKey)
                        {
                            issues.Add(Error(sheetName, rowNum,
                                $"Missing value for required property '{prop.PropertyName}'."));
                        }

                        continue;
                    }

                    if (!TryCoerce(raw, prop.ClrType, out _))
                    {
                        issues.Add(Error(sheetName, rowNum,
                            $"Value '{raw}' is not valid for property '{prop.PropertyName}' ({prop.ClrType})."));
                    }
                }

                foreach (var extra in row.Keys.Where(k => props.All(p => !p.PropertyName.Equals(k, StringComparison.OrdinalIgnoreCase))))
                {
                    issues.Add(Warning(sheetName, rowNum,
                        $"Column '{extra}' does not match any property on entity '{entityName}'."));
                }

                if (keyProps.Count == 1)
                {
                    var keyName = keyProps[0].PropertyName;
                    if (row.TryGetValue(keyName, out var keyValue) && !string.IsNullOrWhiteSpace(keyValue))
                    {
                        if (!seenKeys.Add(keyValue))
                        {
                            issues.Add(Error(sheetName, rowNum,
                                $"Duplicate key value '{keyValue}' for property '{keyName}'."));
                        }
                    }
                }
            }
        }

        foreach (var section in document.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.SectionId))
                issues.Add(Error(SpecWorkbookSheets.Sections, section.RowNumber, "SectionId is required."));
            if (string.IsNullOrWhiteSpace(section.Title))
                issues.Add(Error(SpecWorkbookSheets.Sections, section.RowNumber, "Title is required."));
        }

        return issues;
    }

    public static bool HasErrors(IReadOnlyList<SpecValidationIssue> issues) =>
        issues.Any(i => i.IsError);

    internal static bool TryCoerce(string raw, string clrType, out object? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(raw))
            return true;

        try
        {
            value = clrType.ToLowerInvariant() switch
            {
                "string" => raw,
                "int" => int.Parse(raw),
                "long" => long.Parse(raw),
                "decimal" => decimal.Parse(raw),
                "double" => double.Parse(raw),
                "bool" => raw is "1" or "yes" or "y" or "true",
                "datetime" => DateTime.Parse(raw),
                "dateonly" => DateOnly.Parse(raw),
                "guid" => Guid.Parse(raw),
                _ => raw
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<PropertySheetRow> BuildDefaultProperties(string entityName)
    {
        var key = NamingHelper.ToKeyPropertyName(entityName);
        return
        [
            new PropertySheetRow
            {
                EntityName = entityName,
                PropertyName = key,
                ClrType = "long",
                IsKey = true
            }
        ];
    }

    private static DatabaseProvider ParseDatabase(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DatabaseProvider.SqlServer;

        if (raw.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.PostgreSql;

        return Enum.TryParse<DatabaseProvider>(raw, ignoreCase: true, out var db)
            ? db
            : DatabaseProvider.SqlServer;
    }

    private static SpecValidationIssue Error(string sheet, int? row, string message) => new()
    {
        Sheet = sheet,
        Row = row,
        Message = message,
        IsError = true
    };

    private static SpecValidationIssue Warning(string sheet, int? row, string message) => new()
    {
        Sheet = sheet,
        Row = row,
        Message = message,
        IsError = false
    };
}
