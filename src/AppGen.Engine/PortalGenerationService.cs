using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed class PortalGenerationService(PortalGenerator portalGenerator)
{
    public async Task<PortalGenerationResult> GenerateAsync(
        SolutionSpec spec,
        string outputRootDirectory,
        bool overwrite = false,
        CancellationToken ct = default)
    {
        if (spec.Portal is null)
            return PortalGenerationResult.Fail("Portal configuration is required.");

        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return PortalGenerationResult.Fail("Output folder is required.");

        var outputDir = GenerationOutputHelper.ResolveLayerDirectory(
            outputRootDirectory.Trim(),
            spec.ApplicationName,
            ProjectOutputLayer.Documentation);

        var exists = GenerationOutputHelper.OutputDirectoryExists(outputDir);
        if (exists && !overwrite && File.Exists(Path.Combine(outputDir, "portal", "index.html")))
            return PortalGenerationResult.Fail($"Portal output already exists: {outputDir}. Use regenerate to overwrite portal/.");

        Directory.CreateDirectory(outputDir);

        try
        {
            var prepared = PortalGenerator.PrepareSpec(spec);
            await portalGenerator.GenerateAsync(prepared, outputDir, overwritePortal: overwrite || exists, ct);
            return PortalGenerationResult.Ok(
                outputDir,
                $"Portal generated at {Path.Combine(outputDir, "portal")}. Open portal/index.html with Live Server.");
        }
        catch (Exception ex)
        {
            return PortalGenerationResult.Fail(ex.Message);
        }
    }

    public async Task<PortalGenerationResult> ImportAsync(string projectDirectory, CancellationToken ct = default)
    {
        try
        {
            var spec = await PortalSpecImporter.ImportAsync(projectDirectory, ct);
            return PortalGenerationResult.Ok(projectDirectory, $"Imported portal edits into appgen.json ({spec.Portal?.Sections.Count ?? 0} sections).");
        }
        catch (Exception ex)
        {
            return PortalGenerationResult.Fail(ex.Message);
        }
    }
}

public sealed record PortalGenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static PortalGenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Portal generated successfully.", outputDirectory);

    public static PortalGenerationResult Fail(string message) =>
        new(false, message, null);
}
