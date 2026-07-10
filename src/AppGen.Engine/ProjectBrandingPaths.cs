using AppGen.Core.Branding;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class ProjectBrandingPaths
{
    public static string GetHubIconRelativePath(SolutionSpec spec) =>
        string.IsNullOrWhiteSpace(spec.Project?.Branding?.IconPath)
            ? ProjectBrandingConstants.DefaultIconRelativePath
            : spec.Project!.Branding!.IconPath!.Trim();

    public static string GetHubIconFullPath(string hubDirectory, SolutionSpec spec) =>
        Path.Combine(
            hubDirectory,
            GetHubIconRelativePath(spec).Replace('/', Path.DirectorySeparatorChar));

    public static string? TryResolveHubIconPath(string hubDirectory, SolutionSpec spec)
    {
        var path = GetHubIconFullPath(hubDirectory, spec);
        return File.Exists(path) ? path : null;
    }

    public static bool HasCustomIcon(string hubDirectory, SolutionSpec spec) =>
        TryResolveHubIconPath(hubDirectory, spec) is not null;
}
