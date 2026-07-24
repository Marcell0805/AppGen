using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

public static class SpecImportService
{
    public static async Task<SpecImportResult> ImportAsync(
        string projectDirectory,
        string inputPath,
        SpecImportOptions options,
        CancellationToken ct = default)
    {
        var document = SpecWorkbookReader.Read(Path.GetFullPath(inputPath));
        var issues = SpecDocumentValidator.Validate(document);
        if (SpecDocumentValidator.HasErrors(issues))
        {
            return new SpecImportResult
            {
                Success = false,
                Message = FormatIssues(issues),
                Issues = issues
            };
        }

        if (options.ValidateOnly)
        {
            return new SpecImportResult
            {
                Success = true,
                Message = issues.Count == 0
                    ? "Workbook is valid."
                    : $"Workbook is valid with {issues.Count} warning(s).{Environment.NewLine}{FormatIssues(issues.Where(i => !i.IsError).ToList())}",
                Issues = issues
            };
        }

        var projectDir = Path.GetFullPath(projectDirectory);
        Directory.CreateDirectory(projectDir);

        SolutionSpec? existing = null;
        var manifestPath = Path.Combine(projectDir, "appgen.json");
        if (File.Exists(manifestPath))
            existing = await SpecLoader.LoadAsync(projectDir, ct);

        var merged = SolutionSpecMerger.Merge(existing, document, options.MergeEntities);
        await ProjectSpecWriter.WriteAsync(merged, projectDir, ct);

        if (options.WriteSeedScripts && merged.Entities.Count > 0)
        {
            await WorkbookSeedDataStore.SaveAsync(projectDir, document.EntityData, ct);
            await DatabaseScriptGenerator.WriteAsync(merged, projectDir, document.EntityData, ct);

            foreach (var webDir in GenerationOutputHelper.ResolveExistingLayerDirectories(
                         projectDir, merged.ApplicationName, ProjectOutputLayer.Web))
            {
                await DatabaseScriptGenerator.WriteAsync(merged, webDir, document.EntityData, ct);
            }
        }

        var warningText = issues.Count == 0
            ? string.Empty
            : $"{Environment.NewLine}Warnings:{Environment.NewLine}{FormatIssues(issues.Where(i => !i.IsError).ToList())}";

        return new SpecImportResult
        {
            Success = true,
            Spec = merged,
            Message =
                $"Imported workbook into {manifestPath} ({merged.Entities.Count} entities, {document.Sections.Count} sections).{warningText}",
            Issues = issues
        };
    }

    private static string FormatIssues(IReadOnlyList<SpecValidationIssue> issues)
    {
        if (issues.Count == 0)
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            issues.Select(i => i.Row is int row
                ? $"[{i.Sheet} row {row}] {i.Message}"
                : $"[{i.Sheet}] {i.Message}"));
    }
}
