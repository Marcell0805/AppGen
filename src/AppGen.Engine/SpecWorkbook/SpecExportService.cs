using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

public static class SpecExportService
{
    public static async Task ExportAsync(
        string outputPath,
        string? projectDirectory = null,
        bool templateOnly = false,
        CancellationToken ct = default)
    {
        AppGenSpecDocument document;
        if (templateOnly || string.IsNullOrWhiteSpace(projectDirectory))
        {
            document = SpecWorkbookWriter.CreateTemplate();
        }
        else
        {
            var projectDir = Path.GetFullPath(projectDirectory);
            var manifestPath = Path.Combine(projectDir, "appgen.json");
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("appgen.json not found in project directory.", manifestPath);

            var spec = await SpecLoader.LoadAsync(projectDir, ct);
            document = SpecWorkbookWriter.FromSolutionSpec(spec);
        }

        SpecWorkbookWriter.Write(Path.GetFullPath(outputPath), document);
    }
}
