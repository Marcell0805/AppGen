using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed class MobileGenerationService(MobileApplicationGenerator mobileGenerator)
{
    public async Task<MobileGenerationResult> GenerateAsync(
        SolutionSpec spec,
        string outputRootDirectory,
        IReadOnlyList<string>? entityNames = null,
        string? packageName = null,
        string? apiBaseUrl = null,
        bool forceRegenerate = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(outputRootDirectory))
            return MobileGenerationResult.Fail("Output folder is required.");

        var outputDir = GenerationOutputHelper.ResolveLayerDirectory(
            outputRootDirectory.Trim(),
            spec.ApplicationName,
            ProjectOutputLayer.Mobile);

        var normalized = SpecNormalizer.Normalize(spec);
        var mobile = normalized.Targets?.Mobile ?? SpecNormalizer.BuildTargetsFromLegacy(normalized).Mobile;
        mobile = new MobileTargetSpec
        {
            Enabled = true,
            Framework = mobile.Framework,
            PackageName = string.IsNullOrWhiteSpace(packageName) ? mobile.PackageName : packageName.Trim(),
            ApiBaseUrl = string.IsNullOrWhiteSpace(apiBaseUrl) ? mobile.ApiBaseUrl : apiBaseUrl.Trim(),
            StateManagement = mobile.StateManagement,
            Theme = mobile.Theme,
            Offline = mobile.Offline,
            Capabilities = mobile.Capabilities
        };

        normalized = new SolutionSpec
        {
            SchemaVersion = normalized.SchemaVersion,
            ApplicationName = normalized.ApplicationName,
            RootNamespace = normalized.RootNamespace,
            Project = normalized.Project,
            Phase = normalized.Phase,
            Portal = normalized.Portal,
            EntitySketches = normalized.EntitySketches,
            Targets = new ApplicationTargets
            {
                Documentation = normalized.Targets?.Documentation ?? SpecNormalizer.BuildTargetsFromLegacy(normalized).Documentation,
                Web = normalized.Targets?.Web ?? SpecNormalizer.BuildTargetsFromLegacy(normalized).Web,
                Mobile = mobile
            },
            Generation = normalized.Generation,
            Database = normalized.Database,
            UiTargets = normalized.UiTargets,
            Setup = normalized.Setup,
            Entities = normalized.Entities
        };

        if (!Directory.Exists(outputDir) && normalized.Entities.Count == 0)
            return MobileGenerationResult.Fail("Load a project with entities first, or generate the Web stack before Mobile.");

        Directory.CreateDirectory(outputDir);

        var result = await mobileGenerator.GenerateAsync(
            normalized,
            outputDir,
            new GeneratorOptions { EntityNames = entityNames, Force = forceRegenerate },
            ct);

        if (result.Success)
        {
            await ReadmeGenerator.WriteMobileAsync(new ReadmeContext(
                normalized,
                outputDir,
                ApiBaseUrl: apiBaseUrl,
                EnableMobile: true), ct);
        }

        return result.Success
            ? MobileGenerationResult.Ok(result.OutputPath ?? outputDir, result.Message)
            : MobileGenerationResult.Fail(result.Message);
    }
}

public sealed record MobileGenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static MobileGenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Mobile generated successfully.", outputDirectory);

    public static MobileGenerationResult Fail(string message) =>
        new(false, message, null);
}
