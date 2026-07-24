using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed class DocumentationApplicationGenerator(PortalGenerator portalGenerator) : IApplicationGenerator
{
    public string TargetId => "documentation";

    public async Task<GeneratorTargetResult> GenerateAsync(
        SolutionSpec spec,
        string projectDirectory,
        GeneratorOptions options,
        CancellationToken ct = default)
    {
        if (spec.Portal is null)
            return GeneratorTargetResult.Fail("Portal configuration is required for documentation generation.");

        try
        {
            var prepared = PortalGenerator.PrepareSpec(ProjectInfoSeeder.ApplyToPortalSpec(spec));
            await portalGenerator.GenerateAsync(prepared, projectDirectory, overwritePortal: options.Force, ct);
            await ReadmeGenerator.WriteDocumentationAsync(new ReadmeContext(spec, projectDirectory), ct);
            return GeneratorTargetResult.Ok(
                Path.Combine(projectDirectory, "portal"),
                "Documentation portal generated.");
        }
        catch (Exception ex)
        {
            return GeneratorTargetResult.Fail(ex.Message);
        }
    }
}
