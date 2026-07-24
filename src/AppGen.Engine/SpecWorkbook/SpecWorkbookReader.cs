using AppGen.Core.Models.SpecWorkbook;
using ClosedXML.Excel;

namespace AppGen.Engine;

public static class SpecWorkbookReader
{
    public static AppGenSpecDocument Read(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Workbook not found.", filePath);

        using var workbook = new XLWorkbook(filePath);
        var document = new AppGenSpecDocument
        {
            Application = ReadApplication(workbook)
        };

        document.Sections.AddRange(ReadSections(workbook));
        document.Entities.AddRange(ReadEntities(workbook));
        document.Properties.AddRange(ReadProperties(workbook));

        foreach (var worksheet in workbook.Worksheets)
        {
            if (!worksheet.Name.StartsWith(SpecWorkbookSheets.DataPrefix, StringComparison.OrdinalIgnoreCase))
                continue;

            var entityName = worksheet.Name[SpecWorkbookSheets.DataPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(entityName))
                continue;

            document.EntityData[entityName] = ReadDataSheet(worksheet);
        }

        NormalizeEntityDataKeys(document);
        return document;
    }

    private static void NormalizeEntityDataKeys(AppGenSpecDocument document)
    {
        if (document.EntityData.Count == 0)
            return;

        var entityNames = document.Entities
            .Select(e => e.EntityName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        var remapped = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (sheetEntity, rows) in document.EntityData)
        {
            var match = entityNames.FirstOrDefault(n =>
                n.Equals(sheetEntity, StringComparison.OrdinalIgnoreCase));
            var key = match ?? sheetEntity;
            if (remapped.TryGetValue(key, out var existing))
                existing.AddRange(rows);
            else
                remapped[key] = rows;
        }

        document.EntityData.Clear();
        foreach (var kvp in remapped)
            document.EntityData[kvp.Key] = kvp.Value;
    }

    private static ApplicationSheetData ReadApplication(XLWorkbook workbook)
    {
        var data = new ApplicationSheetData();
        if (!workbook.TryGetWorksheet(SpecWorkbookSheets.Application, out var sheet))
            return data;

        var map = ReadKeyValueSheet(sheet);
        data.ApplicationName = Get(map, "ApplicationName");
        data.RootNamespace = Get(map, "RootNamespace");
        data.Tagline = Get(map, "Tagline");
        data.Description = Get(map, "Description");
        data.Database = Get(map, "Database");
        data.EnableDocumentation = GetBool(map, "EnableDocumentation");
        data.EnableWeb = GetBool(map, "EnableWeb");
        data.EnableMobile = GetBool(map, "EnableMobile");
        data.EnableWebAuth = GetBool(map, "EnableWebAuth");
        data.EnableMobileOffline = GetBool(map, "EnableMobileOffline");
        data.MobileThemePreset = Get(map, "MobileThemePreset");
        data.MobileApiBaseUrl = Get(map, "MobileApiBaseUrl");
        data.DocumentationPreset = Get(map, "DocumentationPreset");
        return data;
    }

    private static Dictionary<string, string> ReadKeyValueSheet(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        for (var row = 2; row <= lastRow; row++)
        {
            var field = sheet.Cell(row, 1).GetString().Trim();
            if (string.IsNullOrWhiteSpace(field))
                continue;
            map[field] = sheet.Cell(row, 2).GetString().Trim();
        }

        return map;
    }

    private static List<SectionSheetRow> ReadSections(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet(SpecWorkbookSheets.Sections, out var sheet))
            return [];

        return ReadTableRows(sheet, (rowNum, get) =>
        {
            var id = get("SectionId");
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return new SectionSheetRow
            {
                RowNumber = rowNum,
                SectionId = id,
                Title = get("Title") ?? id,
                Status = get("Status") ?? "planned",
                Summary = get("Summary"),
                Tags = get("Tags"),
                NavNum = ParseInt(get("NavNum")),
                NavLabel = get("NavLabel")
            };
        });
    }

    private static List<EntitySheetRow> ReadEntities(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet(SpecWorkbookSheets.Entities, out var sheet))
            return [];

        return ReadTableRows(sheet, (rowNum, get) =>
        {
            var name = get("EntityName");
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return new EntitySheetRow
            {
                RowNumber = rowNum,
                EntityName = name,
                TableName = get("TableName"),
                IncludeInUi = ParseBool(get("IncludeInUi"), defaultValue: true),
                IncludeAuditColumns = ParseBool(get("IncludeAuditColumns"), defaultValue: false)
            };
        });
    }

    private static List<PropertySheetRow> ReadProperties(XLWorkbook workbook)
    {
        if (!workbook.TryGetWorksheet(SpecWorkbookSheets.Properties, out var sheet))
            return [];

        return ReadTableRows(sheet, (rowNum, get) =>
        {
            var entity = get("EntityName");
            var property = get("PropertyName");
            if (string.IsNullOrWhiteSpace(entity) || string.IsNullOrWhiteSpace(property))
                return null;

            return new PropertySheetRow
            {
                RowNumber = rowNum,
                EntityName = entity,
                PropertyName = property,
                ClrType = get("ClrType") ?? "string",
                IsKey = ParseBool(get("IsKey"), defaultValue: false),
                IsNullable = ParseBool(get("IsNullable"), defaultValue: false),
                ForeignKeyEntity = get("ForeignKeyEntity"),
                ColumnName = get("ColumnName")
            };
        });
    }

    private static List<Dictionary<string, string>> ReadDataSheet(IXLWorksheet sheet)
    {
        var rows = new List<Dictionary<string, string>>();
        var headerRow = sheet.Row(1);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastCol == 0 || lastRow < 2)
            return rows;

        var headers = new List<string>();
        for (var col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim();
            headers.Add(string.IsNullOrWhiteSpace(header) ? $"Column{col}" : header);
        }

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var col = 0; col < headers.Count; col++)
                dict[headers[col]] = ReadCellValue(row.Cell(col + 1));

            rows.Add(dict);
        }

        return rows;
    }

    private static string ReadCellValue(IXLCell cell)
    {
        var value = cell.GetString().Trim();
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (cell.TryGetValue(out DateTime dateTime))
            return dateTime.ToString("O");

        if (cell.TryGetValue(out DateOnly dateOnly))
            return dateOnly.ToString("O");

        if (cell.TryGetValue(out bool boolean))
            return boolean ? "true" : "false";

        if (cell.TryGetValue(out double number))
        {
            if (number == Math.Truncate(number))
                return ((long)number).ToString();
            return number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }

    private static List<T> ReadTableRows<T>(IXLWorksheet sheet, Func<int, Func<string, string?>, T?> mapRow) where T : class
    {
        var headers = ReadHeaderMap(sheet);
        var results = new List<T>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;

        string? Get(IXLRow row, string header)
        {
            if (!headers.TryGetValue(header, out var col))
                return null;
            var cell = row.Cell(col);
            var value = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(value) && cell.TryGetValue(out double number))
                value = ((long)number).ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.CellsUsed().All(c => string.IsNullOrWhiteSpace(c.GetString())))
                continue;

            var mapped = mapRow(rowNum, h => Get(row, h));
            if (mapped is not null)
                results.Add(mapped);
        }

        return results;
    }

    private static Dictionary<string, int> ReadHeaderMap(IXLWorksheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        for (var col = 1; col <= lastCol; col++)
        {
            var header = sheet.Cell(1, col).GetString().Trim();
            if (!string.IsNullOrWhiteSpace(header))
                map[header] = col;
        }

        return map;
    }

    private static string? Get(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

    private static bool? GetBool(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return null;
        return ParseBool(raw, defaultValue: false);
    }

    private static bool ParseBool(string? raw, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return defaultValue;

        return raw.Trim() switch
        {
            "1" or "yes" or "y" or "true" => true,
            "0" or "no" or "n" or "false" => false,
            _ => bool.TryParse(raw, out var b) ? b : defaultValue
        };
    }

    private static int? ParseInt(string? raw) =>
        int.TryParse(raw, out var value) ? value : null;
}
