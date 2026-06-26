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
