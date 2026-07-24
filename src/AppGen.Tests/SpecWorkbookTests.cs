using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;
using AppGen.Engine;

namespace AppGen.Tests;

public class SpecWorkbookTests
{
    [Fact]
    public async Task Import_workbook_replaces_portal_sections()
    {
        var tempDir = CreateTempDir();
        var projectDir = Path.Combine(tempDir, "hub");
        var xlsxPath = Path.Combine(tempDir, "sections.xlsx");

        try
        {
            var document = SpecWorkbookWriter.CreateTemplate("SectionImportApp");
            document.Sections.Clear();
            document.Sections.Add(new SectionSheetRow
            {
                RowNumber = 2,
                SectionId = "vision",
                Title = "Product Vision",
                Status = "planned",
                Summary = "What this app is for",
                Tags = "strategy",
                NavNum = 1,
                NavLabel = "Vision"
            });
            document.Sections.Add(new SectionSheetRow
            {
                RowNumber = 3,
                SectionId = "ideas",
                Title = "IIID",
                Status = "planned",
                Summary = "What is this?",
                Tags = "strategy",
                NavNum = 2,
                NavLabel = "Ideas"
            });
            document.Sections.Add(new SectionSheetRow
            {
                RowNumber = 4,
                SectionId = "goal",
                Title = "Goaly",
                Status = "planned",
                Summary = "Yes",
                Tags = "goal",
                NavNum = 3,
                NavLabel = "Goals"
            });

            SpecWorkbookWriter.Write(xlsxPath, document);

            var result = await SpecImportService.ImportAsync(
                projectDir,
                xlsxPath,
                new SpecImportOptions());

            Assert.True(result.Success, result.Message);

            var spec = await SpecLoader.LoadAsync(projectDir);
            Assert.NotNull(spec.Portal);
            Assert.Equal(3, spec.Portal!.Sections.Count);
            Assert.Contains(spec.Portal.Sections, s => s.Id == "ideas" && s.Title == "IIID");
            Assert.Equal(3, spec.Portal.Nav.Count);
            Assert.Equal("ideas", spec.Portal.Nav[1].Id);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    [Fact]
    public void Workbook_round_trip_preserves_application_entities_and_properties()
    {
        var tempDir = CreateTempDir();
        var xlsxPath = Path.Combine(tempDir, "roundtrip.xlsx");

        try
        {
            var original = SpecWorkbookWriter.CreateTemplate("RoundTripApp");
            SpecWorkbookWriter.Write(xlsxPath, original);
            var imported = SpecWorkbookReader.Read(xlsxPath);

            Assert.Equal(original.Application.ApplicationName, imported.Application.ApplicationName);
            Assert.Equal(original.Application.Database, imported.Application.Database);
            Assert.Equal(original.Entities.Count, imported.Entities.Count);
            Assert.Equal("User", imported.Entities[0].EntityName);
            Assert.Equal(original.Properties.Count, imported.Properties.Count);
            Assert.Contains(imported.Properties, p => p.PropertyName == "Username" && p.ClrType == "string");
            Assert.Contains(imported.Properties, p => p.PropertyName == "Age" && p.ClrType == "int");
            Assert.True(imported.EntityData.ContainsKey("User"));
            Assert.Equal(2, imported.EntityData["User"].Count);
            Assert.Equal("jane", imported.EntityData["User"][0]["Username"]);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Import_writes_seed_sql_from_data_sheets()
    {
        var tempDir = CreateTempDir();
        var projectDir = Path.Combine(tempDir, "hub");
        var xlsxPath = Path.Combine(tempDir, "import.xlsx");

        try
        {
            var document = SpecWorkbookWriter.CreateTemplate("SeedTestApp");
            SpecWorkbookWriter.Write(xlsxPath, document);

            var result = await SpecImportService.ImportAsync(
                projectDir,
                xlsxPath,
                new SpecImportOptions());

            Assert.True(result.Success, result.Message);
            Assert.True(File.Exists(Path.Combine(projectDir, "appgen.json")));

            var seedPath = Path.Combine(projectDir, "scripts", "sqlserver", "002-seed-data.sql");
            Assert.True(File.Exists(seedPath));
            var seedSql = await File.ReadAllTextAsync(seedPath);
            Assert.Contains("jane", seedSql, StringComparison.Ordinal);
            Assert.Contains("bob", seedSql, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    [Fact]
    public void Validator_reports_unknown_clr_type_and_invalid_data_values()
    {
        var document = SpecWorkbookWriter.CreateTemplate("ValidationApp");
        document.Properties.Add(new PropertySheetRow
        {
            RowNumber = 5,
            EntityName = "User",
            PropertyName = "Score",
            ClrType = "NotARealType",
            IsKey = false,
            IsNullable = true
        });

        document.EntityData["User"][0]["Age"] = "not-a-number";

        var issues = SpecDocumentValidator.Validate(document);

        Assert.True(SpecDocumentValidator.HasErrors(issues));
        Assert.Contains(issues, i => i.IsError && i.Message.Contains("NotARealType", StringComparison.Ordinal));
        Assert.Contains(issues, i => i.IsError && i.Message.Contains("not-a-number", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_reports_missing_application_name()
    {
        var document = SpecWorkbookWriter.CreateTemplate("TempApp");
        document.Application.ApplicationName = string.Empty;

        var issues = SpecDocumentValidator.Validate(document);

        Assert.True(SpecDocumentValidator.HasErrors(issues));
        Assert.Contains(issues, i => i.IsError && i.Message.Contains("ApplicationName", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadDataSheet_reads_numeric_cell_values()
    {
        var tempDir = CreateTempDir();
        var xlsxPath = Path.Combine(tempDir, "numeric.xlsx");

        try
        {
            var document = SpecWorkbookWriter.CreateTemplate("NumericApp");
            document.Entities.Add(new EntitySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                IncludeInUi = true
            });
            document.Properties.Add(new PropertySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                PropertyName = "Location_Id",
                ClrType = "long",
                IsKey = true
            });
            document.EntityData["Location"] =
            [
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Location_Id"] = "1" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Location_Id"] = "2" }
            ];

            SpecWorkbookWriter.Write(xlsxPath, document);
            var imported = SpecWorkbookReader.Read(xlsxPath);

            Assert.True(imported.EntityData.ContainsKey("Location"));
            Assert.Equal(2, imported.EntityData["Location"].Count);
            Assert.Equal("1", imported.EntityData["Location"][0]["Location_Id"]);
            Assert.Equal("2", imported.EntityData["Location"][1]["Location_Id"]);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Import_writes_hybrid_seed_and_persists_for_web_regen()
    {
        var tempDir = CreateTempDir();
        var outputRoot = Path.Combine(tempDir, "output");
        var hubDir = Path.Combine(outputRoot, "HybridApp");
        var webDir = Path.Combine(outputRoot, "HybridApp Web");
        var xlsxPath = Path.Combine(tempDir, "hybrid.xlsx");

        try
        {
            var document = SpecWorkbookWriter.CreateTemplate("HybridApp");
            document.Application.Database = "PostgreSql";
            document.Entities.Add(new EntitySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                IncludeInUi = true
            });
            document.Properties.Add(new PropertySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                PropertyName = "Location_Id",
                ClrType = "long",
                IsKey = true
            });
            document.EntityData["Location"] =
            [
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Location_Id"] = "1" },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Location_Id"] = "2" }
            ];

            SpecWorkbookWriter.Write(xlsxPath, document);
            Directory.CreateDirectory(webDir);
            await ProjectSpecWriter.WriteAsync(SpecLoader.CreateDefault("HybridApp", null, DatabaseProvider.PostgreSql), webDir);

            var result = await SpecImportService.ImportAsync(hubDir, xlsxPath, new SpecImportOptions());
            Assert.True(result.Success, result.Message);

            var webSeedPath = Path.Combine(webDir, "scripts", "postgresql", "002-seed-data.sql");
            Assert.True(File.Exists(webSeedPath));
            var webSeed = await File.ReadAllTextAsync(webSeedPath);
            Assert.Contains("INSERT INTO \"public\".\"Location\"", webSeed, StringComparison.Ordinal);
            Assert.Contains("VALUES (1);", webSeed, StringComparison.Ordinal);
            Assert.Contains("VALUES (2);", webSeed, StringComparison.Ordinal);
            Assert.Contains("jane", webSeed, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(hubDir, WorkbookSeedDataStore.FileName)));

            await DatabaseScriptGenerator.WriteAsync(result.Spec!, webDir);
            var regenSeed = await File.ReadAllTextAsync(webSeedPath);
            Assert.Contains("VALUES (1);", regenSeed, StringComparison.Ordinal);
            Assert.Contains("jane", regenSeed, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    [Fact]
    public async Task Import_writes_web_seed_when_hub_folder_name_differs_from_application_name()
    {
        var tempDir = CreateTempDir();
        var outputRoot = Path.Combine(tempDir, "output");
        var hubDir = Path.Combine(outputRoot, "LegacyHub");
        var webDir = Path.Combine(outputRoot, "LegacyHub Web");
        var xlsxPath = Path.Combine(tempDir, "legacy.xlsx");

        try
        {
            var document = SpecWorkbookWriter.CreateTemplate("RenamedApp");
            document.Application.Database = "PostgreSql";
            document.Entities.Add(new EntitySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                IncludeInUi = true
            });
            document.Properties.Add(new PropertySheetRow
            {
                RowNumber = 3,
                EntityName = "Location",
                PropertyName = "Location_Id",
                ClrType = "long",
                IsKey = true
            });
            document.EntityData["Location"] =
            [
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Location_Id"] = "1" }
            ];

            SpecWorkbookWriter.Write(xlsxPath, document);
            Directory.CreateDirectory(webDir);

            var result = await SpecImportService.ImportAsync(hubDir, xlsxPath, new SpecImportOptions());
            Assert.True(result.Success, result.Message);

            var webSeedPath = Path.Combine(webDir, "scripts", "postgresql", "002-seed-data.sql");
            Assert.True(File.Exists(webSeedPath));
            var webSeed = await File.ReadAllTextAsync(webSeedPath);
            Assert.Contains("VALUES (1);", webSeed, StringComparison.Ordinal);
        }
        finally
        {
            Cleanup(tempDir);
        }
    }

    private static string CreateTempDir() =>
        Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

    private static void Cleanup(string tempDir)
    {
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, recursive: true);
    }
}
