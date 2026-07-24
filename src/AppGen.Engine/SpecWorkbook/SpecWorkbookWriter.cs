using AppGen.Core;

using AppGen.Core.Models;

using AppGen.Core.Models.SpecWorkbook;

using ClosedXML.Excel;



namespace AppGen.Engine;



public static class SpecWorkbookWriter

{

    public static void Write(string filePath, AppGenSpecDocument document)

    {

        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))

            Directory.CreateDirectory(directory);



        using var workbook = new XLWorkbook();

        WriteListsSheet(workbook);

        WriteInstructions(workbook);

        var applicationRows = WriteApplication(workbook, document.Application);

        var sectionsLastRow = WriteSections(workbook, document.Sections);

        var entitiesLastRow = WriteEntities(workbook, document.Entities);

        WriteProperties(workbook, document.Properties);

        WriteDataSheets(workbook, document.Entities, document.Properties, document.EntityData);



        ApplyValidations(workbook, applicationRows, sectionsLastRow, entitiesLastRow);

        workbook.Worksheet(SpecWorkbookLists.ListsSheetName).Hide();

        workbook.SaveAs(filePath);

    }



    public static AppGenSpecDocument CreateTemplate(string? applicationName = null)

    {

        var appName = string.IsNullOrWhiteSpace(applicationName) ? "MyApp" : applicationName.Trim();

        var keyName = NamingHelper.ToKeyPropertyName("User");



        return new AppGenSpecDocument

        {

            Application = new ApplicationSheetData

            {

                ApplicationName = appName,

                RootNamespace = appName,

                Tagline = "Short description of the app",

                Description = "Goals, audience, and scope for this application.",

                Database = "SqlServer",

                EnableDocumentation = true,

                EnableWeb = true,

                EnableMobile = false,

                EnableWebAuth = false,

                EnableMobileOffline = false,

                MobileThemePreset = "appgen",

                MobileApiBaseUrl = "http://localhost:5000",

                DocumentationPreset = "engineering-portal"

            },

            Sections =

            [

                new SectionSheetRow

                {

                    RowNumber = 2,

                    SectionId = "vision",

                    Title = "Product Vision",

                    Status = "planned",

                    Summary = "What this app is for",

                    Tags = "strategy",

                    NavNum = 1,

                    NavLabel = "Vision"

                }

            ],

            Entities =

            [

                new EntitySheetRow

                {

                    RowNumber = 2,

                    EntityName = "User",

                    TableName = "Users",

                    IncludeInUi = true,

                    IncludeAuditColumns = false

                }

            ],

            Properties =

            [

                new PropertySheetRow

                {

                    RowNumber = 2,

                    EntityName = "User",

                    PropertyName = keyName,

                    ClrType = "long",

                    IsKey = true,

                    IsNullable = false

                },

                new PropertySheetRow

                {

                    RowNumber = 3,

                    EntityName = "User",

                    PropertyName = "Username",

                    ClrType = "string",

                    IsKey = false,

                    IsNullable = false

                },

                new PropertySheetRow

                {

                    RowNumber = 4,

                    EntityName = "User",

                    PropertyName = "Age",

                    ClrType = "int",

                    IsKey = false,

                    IsNullable = true

                }

            ],

            EntityData = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase)

            {

                ["User"] =

                [

                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

                    {

                        [keyName] = "1",

                        ["Username"] = "jane",

                        ["Age"] = "32"

                    },

                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)

                    {

                        [keyName] = "2",

                        ["Username"] = "bob",

                        ["Age"] = "28"

                    }

                ]

            }

        };

    }



    public static AppGenSpecDocument FromSolutionSpec(SolutionSpec spec)

    {

        var targets = spec.Targets ?? SpecNormalizer.BuildTargetsFromLegacy(spec);

        var document = new AppGenSpecDocument

        {

            Application = new ApplicationSheetData

            {

                ApplicationName = spec.ApplicationName,

                RootNamespace = spec.RootNamespace,

                Tagline = spec.Project?.Tagline,

                Description = spec.Project?.Description,

                Database = spec.Database.ToString(),

                EnableDocumentation = targets.Documentation.Enabled,

                EnableWeb = targets.Web.Enabled,

                EnableMobile = targets.Mobile.Enabled,

                EnableWebAuth = targets.Web.Auth.Enabled,

                EnableMobileOffline = targets.Mobile.Offline.Enabled,

                MobileThemePreset = targets.Mobile.Theme.Preset,

                MobileApiBaseUrl = targets.Mobile.ApiBaseUrl,

                DocumentationPreset = targets.Documentation.Preset

            }

        };



        var navById = (spec.Portal?.Nav ?? [])

            .ToDictionary(n => n.Id, n => n, StringComparer.OrdinalIgnoreCase);



        foreach (var section in spec.Portal?.Sections ?? [])

        {

            navById.TryGetValue(section.Id, out var nav);

            document.Sections.Add(new SectionSheetRow

            {

                RowNumber = document.Sections.Count + 2,

                SectionId = section.Id,

                Title = section.Title,

                Status = section.Status,

                Summary = section.Summary,

                Tags = section.Tags.Count > 0 ? string.Join(", ", section.Tags) : null,

                NavNum = nav?.Num,

                NavLabel = nav?.Label

            });

        }



        foreach (var entity in spec.Entities)

        {

            document.Entities.Add(new EntitySheetRow

            {

                RowNumber = document.Entities.Count + 2,

                EntityName = entity.Name,

                TableName = entity.TableName,

                IncludeInUi = entity.IncludeInUi,

                IncludeAuditColumns = entity.IncludeAuditColumns

            });



            foreach (var property in entity.Properties)

            {

                document.Properties.Add(new PropertySheetRow

                {

                    RowNumber = document.Properties.Count + 2,

                    EntityName = entity.Name,

                    PropertyName = property.Name,

                    ClrType = property.ClrType,

                    IsKey = property.IsKey,

                    IsNullable = property.IsNullable,

                    ForeignKeyEntity = property.ForeignKeyEntity,

                    ColumnName = property.ColumnName

                });

            }



            var headers = entity.Properties.Select(p => p.Name).ToList();

            document.EntityData[entity.Name] =

            [

                headers.ToDictionary(h => h, _ => string.Empty, StringComparer.OrdinalIgnoreCase)

            ];

        }



        return document;

    }



    private static void WriteListsSheet(XLWorkbook workbook)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookLists.ListsSheetName);

        WriteListColumn(sheet, 1, "Database", SpecWorkbookLists.Databases);

        WriteListColumn(sheet, 2, "Boolean", SpecWorkbookLists.Booleans);

        WriteListColumn(sheet, 3, "MobileThemePreset", SpecWorkbookLists.MobileThemePresets);

        WriteListColumn(sheet, 4, "DocumentationPreset", SpecWorkbookLists.DocumentationPresets);

        WriteListColumn(sheet, 5, "SectionStatus", SpecWorkbookLists.SectionStatuses);

        WriteListColumn(sheet, 6, "ClrType", SpecWorkbookLists.ClrTypes);

        sheet.Columns().AdjustToContents();

    }



    private static void WriteListColumn(IXLWorksheet sheet, int column, string header, IReadOnlyList<string> values)

    {

        sheet.Cell(1, column).Value = header;

        for (var i = 0; i < values.Count; i++)

            sheet.Cell(i + 2, column).Value = values[i];

    }



    private static void WriteInstructions(XLWorkbook workbook)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookLists.InstructionsSheetName);

        sheet.Cell(1, 1).Value = "AppGen Spec Workbook";

        sheet.Cell(1, 1).Style.Font.Bold = true;

        sheet.Cell(1, 1).Style.Font.FontSize = 14;



        var lines = new[]

        {

            "",

            "1. Fill Application settings (column B). Field names in column A are locked.",

            "2. Add portal sections on Sections, entities on Entities, columns on Properties.",

            "3. Add sample rows on Data_<Entity> sheets (headers must match property names).",

            "4. Import via AppGen Project tab or: dotnet run --project src/AppGen.CLI -- spec import",

            "",

            $"Full guide: {SpecWorkbookLists.GuidePath}",

            "",

            "Dropdowns: Database, booleans, ClrType, theme presets, section status.",

            "EntityName and ForeignKeyEntity on Properties use the Entities sheet.",

            "Row 2 on each table sheet is styled as an example — copy it for new rows.",

            "",

            "Sheets:",

            "  Application — app name, layers, database",

            "  Sections — documentation portal sections",

            "  Entities / Properties — data model",

            "  Data_* — seed data for SQL scripts"

        };



        for (var i = 0; i < lines.Length; i++)

            sheet.Cell(i + 2, 1).Value = lines[i];



        sheet.Column(1).Width = 90;

        sheet.TabColor = XLColor.FromHtml("#2E75B6");

    }



    private static Dictionary<string, int> WriteApplication(XLWorkbook workbook, ApplicationSheetData app)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookSheets.Application);

        sheet.Cell(1, 1).Value = "Field";

        sheet.Cell(1, 2).Value = "Value";

        StyleHeader(sheet.Range(1, 1, 1, 2));



        var rows = new (string Field, string? Value)[]

        {

            ("ApplicationName", app.ApplicationName),

            ("RootNamespace", app.RootNamespace),

            ("Tagline", app.Tagline),

            ("Description", app.Description),

            ("Database", app.Database ?? "SqlServer"),

            ("EnableDocumentation", FormatBool(app.EnableDocumentation)),

            ("EnableWeb", FormatBool(app.EnableWeb)),

            ("EnableMobile", FormatBool(app.EnableMobile)),

            ("EnableWebAuth", FormatBool(app.EnableWebAuth)),

            ("EnableMobileOffline", FormatBool(app.EnableMobileOffline)),

            ("MobileThemePreset", app.MobileThemePreset ?? "appgen"),

            ("MobileApiBaseUrl", app.MobileApiBaseUrl ?? "http://localhost:5000"),

            ("DocumentationPreset", app.DocumentationPreset ?? "engineering-portal")

        };



        var fieldRows = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var row = 2;

        foreach (var (field, value) in rows)

        {

            sheet.Cell(row, 1).Value = field;

            sheet.Cell(row, 2).Value = value ?? string.Empty;

            fieldRows[field] = row;

            row++;

        }



        sheet.Columns().AdjustToContents();

        SpecWorkbookValidation.LockFieldNameColumn(sheet, row - 1);

        return fieldRows;

    }



    private static int WriteSections(XLWorkbook workbook, IReadOnlyList<SectionSheetRow> sections)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookSheets.Sections);

        WriteHeader(sheet, "SectionId", "Title", "Status", "Summary", "Tags", "NavNum", "NavLabel");

        var row = 2;

        foreach (var section in sections)

        {

            sheet.Cell(row, 1).Value = section.SectionId;

            sheet.Cell(row, 2).Value = section.Title;

            sheet.Cell(row, 3).Value = section.Status;

            sheet.Cell(row, 4).Value = section.Summary ?? string.Empty;

            sheet.Cell(row, 5).Value = section.Tags ?? string.Empty;

            sheet.Cell(row, 6).Value = section.NavNum?.ToString() ?? string.Empty;

            sheet.Cell(row, 7).Value = section.NavLabel ?? string.Empty;

            row++;

        }



        SpecWorkbookValidation.FreezeHeaderRow(sheet);

        if (sections.Count > 0)

            SpecWorkbookValidation.StyleExampleRow(sheet, 2, 7);



        sheet.Columns().AdjustToContents();

        return Math.Max(row - 1, 2);

    }



    private static int WriteEntities(XLWorkbook workbook, IReadOnlyList<EntitySheetRow> entities)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookSheets.Entities);

        WriteHeader(sheet, "EntityName", "TableName", "IncludeInUi", "IncludeAuditColumns");

        var row = 2;

        foreach (var entity in entities)

        {

            sheet.Cell(row, 1).Value = entity.EntityName;

            sheet.Cell(row, 2).Value = entity.TableName ?? string.Empty;

            sheet.Cell(row, 3).Value = entity.IncludeInUi ? "true" : "false";

            sheet.Cell(row, 4).Value = entity.IncludeAuditColumns ? "true" : "false";

            row++;

        }



        SpecWorkbookValidation.FreezeHeaderRow(sheet);

        if (entities.Count > 0)

            SpecWorkbookValidation.StyleExampleRow(sheet, 2, 4);



        sheet.Columns().AdjustToContents();

        return Math.Max(row - 1, 2);

    }



    private static void WriteProperties(XLWorkbook workbook, IReadOnlyList<PropertySheetRow> properties)

    {

        var sheet = workbook.Worksheets.Add(SpecWorkbookSheets.Properties);

        WriteHeader(sheet, "EntityName", "PropertyName", "ClrType", "IsKey", "IsNullable", "ForeignKeyEntity", "ColumnName");

        var row = 2;

        foreach (var property in properties)

        {

            sheet.Cell(row, 1).Value = property.EntityName;

            sheet.Cell(row, 2).Value = property.PropertyName;

            sheet.Cell(row, 3).Value = property.ClrType;

            sheet.Cell(row, 4).Value = property.IsKey ? "true" : "false";

            sheet.Cell(row, 5).Value = property.IsNullable ? "true" : "false";

            sheet.Cell(row, 6).Value = property.ForeignKeyEntity ?? string.Empty;

            sheet.Cell(row, 7).Value = property.ColumnName ?? string.Empty;

            row++;

        }



        SpecWorkbookValidation.FreezeHeaderRow(sheet);

        if (properties.Count > 0)

            SpecWorkbookValidation.StyleExampleRow(sheet, 2, 7);



        sheet.Columns().AdjustToContents();

    }



    private static void WriteDataSheets(

        XLWorkbook workbook,

        IReadOnlyList<EntitySheetRow> entities,

        IReadOnlyList<PropertySheetRow> properties,

        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData)

    {

        foreach (var entity in entities)

        {

            var sheetName = SpecWorkbookSheets.DataPrefix + entity.EntityName;

            if (sheetName.Length > 31)

                sheetName = sheetName[..31];



            var sheet = workbook.Worksheets.Add(sheetName);

            var headers = properties

                .Where(p => p.EntityName.Equals(entity.EntityName, StringComparison.OrdinalIgnoreCase))

                .Select(p => p.PropertyName)

                .ToList();



            if (headers.Count == 0)

                continue;



            WriteHeader(sheet, headers.ToArray());

            entityData.TryGetValue(entity.EntityName, out var rows);

            rows ??= [];



            var rowNum = 2;

            var hasExample = false;

            foreach (var dataRow in rows)

            {

                if (dataRow.Values.All(string.IsNullOrWhiteSpace))

                    continue;



                for (var col = 0; col < headers.Count; col++)

                {

                    dataRow.TryGetValue(headers[col], out var value);

                    sheet.Cell(rowNum, col + 1).Value = value ?? string.Empty;

                }



                if (!hasExample)

                {

                    SpecWorkbookValidation.StyleExampleRow(sheet, rowNum, headers.Count);

                    hasExample = true;

                }



                rowNum++;

            }



            SpecWorkbookValidation.FreezeHeaderRow(sheet);

            sheet.Columns().AdjustToContents();

        }

    }



    private static void ApplyValidations(

        XLWorkbook workbook,

        IReadOnlyDictionary<string, int> applicationRows,

        int sectionsLastRow,

        int entitiesLastRow)

    {

        ApplyApplicationValidations(workbook, applicationRows);

        ApplyTableValidations(workbook, SpecWorkbookSheets.Sections, sectionsLastRow, 7,

            statusColumn: 3);

        ApplyTableValidations(workbook, SpecWorkbookSheets.Entities, entitiesLastRow, 4,

            boolColumns: [3, 4]);

        ApplyPropertiesValidations(workbook);

    }



    private static void ApplyApplicationValidations(XLWorkbook workbook, IReadOnlyDictionary<string, int> fieldRows)

    {

        var sheet = workbook.Worksheet(SpecWorkbookSheets.Application);



        if (fieldRows.TryGetValue("Database", out var dbRow))

            SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Cell(dbRow, 2), listColumn: 1);



        foreach (var field in new[]

                 {

                     "EnableDocumentation", "EnableWeb", "EnableMobile", "EnableWebAuth", "EnableMobileOffline"

                 })

        {

            if (fieldRows.TryGetValue(field, out var boolRow))

                SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Cell(boolRow, 2), listColumn: 2);

        }



        if (fieldRows.TryGetValue("MobileThemePreset", out var themeRow))

            SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Cell(themeRow, 2), listColumn: 3);



        if (fieldRows.TryGetValue("DocumentationPreset", out var docRow))

            SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Cell(docRow, 2), listColumn: 4);

    }



    private static void ApplyTableValidations(

        XLWorkbook workbook,

        string sheetName,

        int lastDataRow,

        int lastColumn,

        int? statusColumn = null,

        int[]? boolColumns = null)

    {

        var sheet = workbook.Worksheet(sheetName);

        var endRow = SpecWorkbookLists.DataEntryMaxRow;



        if (statusColumn is int statusCol)

        {

            SpecWorkbookValidation.ApplyListFromListsSheet(

                workbook,

                sheet.Range(2, statusCol, endRow, statusCol),

                listColumn: 5);

        }



        if (boolColumns is not null)

        {

            foreach (var col in boolColumns)

            {

                SpecWorkbookValidation.ApplyListFromListsSheet(

                    workbook,

                    sheet.Range(2, col, endRow, col),

                    listColumn: 2);

            }

        }

    }



    private static void ApplyPropertiesValidations(XLWorkbook workbook)

    {

        var sheet = workbook.Worksheet(SpecWorkbookSheets.Properties);

        var endRow = SpecWorkbookLists.DataEntryMaxRow;



        SpecWorkbookValidation.ApplyEntityNameList(workbook, sheet.Range(2, 1, endRow, 1));

        SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Range(2, 3, endRow, 3), listColumn: 6);

        SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Range(2, 4, endRow, 4), listColumn: 2);

        SpecWorkbookValidation.ApplyListFromListsSheet(workbook, sheet.Range(2, 5, endRow, 5), listColumn: 2);

        SpecWorkbookValidation.ApplyEntityNameList(workbook, sheet.Range(2, 6, endRow, 6));

    }



    private static void WriteHeader(IXLWorksheet sheet, params string[] headers)

    {

        for (var col = 0; col < headers.Length; col++)

            sheet.Cell(1, col + 1).Value = headers[col];

        StyleHeader(sheet.Range(1, 1, 1, headers.Length));

    }



    private static void StyleHeader(IXLRange range)

    {

        range.Style.Font.Bold = true;

        range.Style.Fill.BackgroundColor = XLColor.LightGray;

    }



    private static string FormatBool(bool? value) => value == true ? "true" : "false";

}


