using AppGen.Core;
using AppGen.Engine;

namespace AppGen.UI.Services;

public static class ProjectOutputPaths
{
    public static string HubDirectory(string outputRoot, string applicationName) =>
        GenerationOutputHelper.ResolveLayerDirectory(outputRoot, applicationName, ProjectOutputLayer.Hub);

    public static string WebDirectory(string outputRoot, string applicationName) =>
        GenerationOutputHelper.ResolveLayerDirectory(outputRoot, applicationName, ProjectOutputLayer.Web);

    public static string DocumentationDirectory(string outputRoot, string applicationName) =>
        GenerationOutputHelper.ResolveLayerDirectory(outputRoot, applicationName, ProjectOutputLayer.Documentation);

    public static string MobileDirectory(string outputRoot, string applicationName) =>
        GenerationOutputHelper.ResolveLayerDirectory(outputRoot, applicationName, ProjectOutputLayer.Mobile);

    public static string? FindManifestDirectory(string outputRoot, string applicationName, ProjectOutputLayer preferredLayer)
    {
        if (string.IsNullOrWhiteSpace(outputRoot) || string.IsNullOrWhiteSpace(applicationName))
            return null;

        return GenerationOutputHelper.FindManifestDirectory(
            outputRoot.Trim(),
            NamingHelper.NormalizeAppName(applicationName.Trim()),
            preferredLayer);
    }
}
