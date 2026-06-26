namespace AppGen.Core.Models;

public sealed class PortalSpec
{
    public string Preset { get; init; } = "engineering-portal";
    public PortalSettings Settings { get; init; } = new();
    public List<PortalNavItem> Nav { get; init; } = [];
    public List<PortalSectionSpec> Sections { get; init; } = [];
    public PortalFeatures Features { get; init; } = new();
}

public sealed class PortalSettings
{
    public string PortalName { get; init; } = string.Empty;
    public string? Tagline { get; init; }
    public string? Version { get; init; } = "1.0";
    public string? HomeQuote { get; init; }
    public string? MaintainerDocsUrl { get; init; }
    public string? ProductRepoUrl { get; init; }
    public PortalAuthSettings Auth { get; init; } = new();
    public PortalThemeSettings Theme { get; init; } = new();
}

public sealed class PortalAuthSettings
{
    public string Password { get; init; } = "portal";
    public string StorageKey { get; init; } = "appgen_portal_auth";
}

public sealed class PortalThemeSettings
{
    public string PrimaryColor { get; init; } = "#1B3A5C";
    public string AccentColor { get; init; } = "#2E75B6";
    public string HighlightColor { get; init; } = "#F28C28";
    public string BackgroundColor { get; init; } = "#E8F1F8";
}

public sealed class PortalNavItem
{
    public required string Id { get; init; }
    public int Num { get; init; }
    public string File { get; init; } = string.Empty;
    public required string Label { get; init; }
    public bool Available { get; init; } = true;
}

public sealed class PortalSectionSpec
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public string Status { get; init; } = "planned";
    public List<string> Tags { get; init; } = [];
    public List<string> SearchKeywords { get; init; } = [];
    public string? Summary { get; init; }
    public string? SidebarNote { get; init; }
    public List<PortalBlockSpec> Blocks { get; init; } = [];
}

public sealed class PortalBlockSpec
{
    public string? Id { get; init; }
    public string? Heading { get; init; }
    public string? Content { get; init; }
    public List<string> Bullets { get; init; } = [];
}

public sealed class PortalFeatures
{
    public bool PasswordGate { get; init; } = true;
    public bool Search { get; init; } = true;
}
