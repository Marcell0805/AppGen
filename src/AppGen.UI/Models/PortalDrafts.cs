namespace AppGen.UI.Models;

public sealed class PortalSectionDraft
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = "planned";
    public string? Summary { get; set; }
    public List<PortalBlockDraft> Blocks { get; } = [];
}

public sealed class PortalBlockDraft
{
    public string? Id { get; set; }
    public string? Heading { get; set; }
    public string? Content { get; set; }
    public List<string> Bullets { get; } = [];
}

public sealed class EntitySketchDraft
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Phase { get; set; } = "1";
    public string Status { get; set; } = "planned";
}

/// <summary>Documentation tab state shared across navigation (scoped per session).</summary>
public sealed class PortalUiDraft
{
    public string ApplicationName { get; init; } = string.Empty;
    public string OutputRoot { get; init; } = string.Empty;
    public string PortalName { get; init; } = string.Empty;
    public string? Tagline { get; init; }
    public string? HomeQuote { get; init; }
    public bool PasswordGate { get; init; } = true;
    public bool SearchEnabled { get; init; } = true;
    public List<PortalSectionDraft> Sections { get; init; } = [];
    public List<EntitySketchDraft> EntitySketches { get; init; } = [];

    public bool MatchesProject(string applicationName, string outputRoot) =>
        ApplicationName.Equals(applicationName, StringComparison.OrdinalIgnoreCase) &&
        OutputRoot.Equals(outputRoot, StringComparison.OrdinalIgnoreCase);
}
