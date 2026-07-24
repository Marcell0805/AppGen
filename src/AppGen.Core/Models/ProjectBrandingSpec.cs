namespace AppGen.Core.Models;

public sealed class ProjectBrandingSpec
{
    /// <summary>Relative path from hub folder, e.g. assets/branding/icon.png</summary>
    public string? IconPath { get; init; }

    /// <summary>Original upload filename for UI display.</summary>
    public string? OriginalFileName { get; init; }
}
