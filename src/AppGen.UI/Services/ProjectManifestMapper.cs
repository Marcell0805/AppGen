using AppGen.Core;
using AppGen.Core.Models;
using AppGen.UI.Models;

namespace AppGen.UI.Services;

public static class ProjectManifestMapper
{
    public static SolutionSpec ToSpec(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets,
        ProjectSetupSpec setup,
        string portalName,
        string? tagline,
        string? homeQuote,
        bool passwordGate,
        bool searchEnabled,
        IEnumerable<PortalSectionDraft> sections,
        IEnumerable<EntitySketchDraft> sketches,
        IEnumerable<EntitySpec>? entities = null) => new()
    {
        SchemaVersion = SolutionSpec.CurrentSchemaVersion,
        ApplicationName = NamingHelper.NormalizeAppName(applicationName),
        RootNamespace = NamingHelper.NormalizeAppName(rootNamespace ?? applicationName),
        Phase = ProjectPhase.Portal,
        Portal = new PortalSpec
        {
            Preset = "engineering-portal",
            Settings = new PortalSettings
            {
                PortalName = portalName,
                Tagline = tagline,
                HomeQuote = homeQuote,
                Auth = new PortalAuthSettings
                {
                    Password = NamingHelper.NormalizeAppName(applicationName).ToLowerInvariant(),
                    StorageKey = $"{NamingHelper.NormalizeAppName(applicationName).ToLowerInvariant()}_portal_auth"
                }
            },
            Sections = sections.Select(ToSection).ToList(),
            Features = new PortalFeatures
            {
                PasswordGate = passwordGate,
                Search = searchEnabled
            }
        },
        EntitySketches = sketches.Select(ToSketch).ToList(),
        Database = database,
        UiTargets = uiTargets,
        Setup = setup,
        Entities = entities?.ToList() ?? []
    };

    public static void ApplySketchesToEntities(List<EntityDraft> entities, IEnumerable<EntitySketch> sketches)
    {
        var existing = entities.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var sketch in sketches)
        {
            if (string.IsNullOrWhiteSpace(sketch.Name) || existing.Contains(sketch.Name))
                continue;
            entities.Add(new EntityDraft(sketch.Name));
            existing.Add(sketch.Name);
        }
    }

    public static void ApplyEntitiesToDrafts(List<EntityDraft> entities, IReadOnlyList<EntitySpec> entitySpecs)
    {
        entities.Clear();
        foreach (var spec in entitySpecs)
        {
            var draft = new EntityDraft(spec.Name)
            {
                IncludeInUi = spec.IncludeInUi,
                IncludeAuditColumns = spec.IncludeAuditColumns
            };
            draft.Properties.Clear();
            foreach (var property in spec.Properties)
            {
                draft.Properties.Add(new PropertyRow
                {
                    Name = property.Name,
                    ClrType = property.ClrType,
                    IsKey = property.IsKey,
                    IsNullable = property.IsNullable,
                    ForeignKeyEntity = property.ForeignKeyEntity
                });
            }
            entities.Add(draft);
        }
    }

    private static PortalSectionSpec ToSection(PortalSectionDraft draft) => new()
    {
        Id = draft.Id,
        Title = draft.Title,
        Status = draft.Status,
        Summary = draft.Summary,
        Blocks = draft.Blocks.Select(b => new PortalBlockSpec
        {
            Id = b.Id,
            Heading = b.Heading,
            Content = b.Content,
            Bullets = b.Bullets.ToList()
        }).ToList()
    };

    private static EntitySketch ToSketch(EntitySketchDraft draft) => new()
    {
        Name = draft.Name,
        Description = draft.Description,
        Phase = draft.Phase,
        Status = draft.Status
    };

    public static PortalSectionDraft ToSectionDraft(PortalSectionSpec section)
    {
        var draft = new PortalSectionDraft
        {
            Id = section.Id,
            Title = section.Title,
            Status = section.Status,
            Summary = section.Summary
        };
        foreach (var block in section.Blocks)
        {
            var blockDraft = new PortalBlockDraft
            {
                Id = block.Id,
                Heading = block.Heading,
                Content = block.Content
            };
            blockDraft.Bullets.AddRange(block.Bullets);
            draft.Blocks.Add(blockDraft);
        }
        return draft;
    }

    public static EntitySketchDraft ToSketchDraft(EntitySketch sketch) => new()
    {
        Name = sketch.Name,
        Description = sketch.Description,
        Phase = sketch.Phase,
        Status = sketch.Status
    };
}
