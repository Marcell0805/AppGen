using AppGen.Core.Models;

namespace AppGen.Core.Branding;

public static class ProjectBrandingHelper
{
    public static bool IsConfigured(SolutionSpec spec) =>
        !string.IsNullOrWhiteSpace(spec.Project?.Branding?.IconPath);
}
