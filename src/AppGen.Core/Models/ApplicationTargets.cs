namespace AppGen.Core.Models;

public sealed class ApplicationTargets
{
    public DocumentationTargetSpec Documentation { get; init; } = new();
    public WebTargetSpec Web { get; init; } = new();
    public MobileTargetSpec Mobile { get; init; } = new();
}

public sealed class DocumentationTargetSpec
{
    public bool Enabled { get; init; } = true;
    public string Preset { get; init; } = "engineering-portal";
}

public sealed class WebTargetSpec
{
    public bool Enabled { get; init; } = true;
    public WebAuthTargetSpec Auth { get; init; } = new();
}

public sealed class WebAuthTargetSpec
{
    public bool Enabled { get; init; }
    public string Issuer { get; init; } = string.Empty;
    public int TokenLifetimeMinutes { get; init; } = 60;
}

public sealed class MobileCapabilitiesSpec
{
    public List<string> Enabled { get; init; } = [];
}

public sealed class MobileTargetSpec
{
    public bool Enabled { get; init; }
    public string Framework { get; init; } = "flutter";
    public string PackageName { get; init; } = string.Empty;
    public string ApiBaseUrl { get; init; } = "https://localhost:5001";
    public string StateManagement { get; init; } = "riverpod";
    public MobileThemeSpec Theme { get; init; } = new();
    public MobileOfflineTargetSpec Offline { get; init; } = new();
    public MobileCapabilitiesSpec Capabilities { get; init; } = new();
}

public sealed class MobileOfflineTargetSpec
{
    public bool Enabled { get; init; }
    public string Provider { get; init; } = "sqlite";
}

public sealed class MobileThemeSpec
{
    public string Preset { get; init; } = "appgen";
    public string? PrimaryColor { get; init; }
    public string? AccentColor { get; init; }
    public string? BackgroundColor { get; init; }
    public string? HighlightColor { get; init; }
}

public sealed class GenerationMetadata
{
    public TargetGenerationInfo? Mobile { get; init; }
    public TargetGenerationInfo? Documentation { get; init; }
    public TargetGenerationInfo? Web { get; init; }
}

public sealed class TargetGenerationInfo
{
    public string? LastGenerated { get; init; }
    public List<string> Entities { get; init; } = [];
}
