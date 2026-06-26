using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class PortalDefaults
{
    public static PortalSpec CreateDefaultPortal(string applicationName, string preset = "engineering-portal") => new()
    {
        Preset = preset,
        Settings = new PortalSettings
        {
            PortalName = $"{applicationName} Engineering Portal",
            Tagline = "Share the vision, architecture and roadmap",
            HomeQuote = "Built with AppGen — static portal for early sharing.",
            Auth = new PortalAuthSettings
            {
                Password = NamingHelper.NormalizeAppName(applicationName).ToLowerInvariant(),
                StorageKey = $"{NamingHelper.NormalizeAppName(applicationName).ToLowerInvariant()}_portal_auth"
            },
            Theme = new PortalThemeSettings()
        },
        Sections =
        [
            new PortalSectionSpec
            {
                Id = "vision",
                Title = "Vision",
                Status = "live",
                Tags = ["vision"],
                Summary = $"Executive vision for {applicationName}.",
                Blocks =
                [
                    new PortalBlockSpec
                    {
                        Id = "vision-statement",
                        Heading = "Vision statement",
                        Content = $"Describe what {applicationName} will deliver and who it serves."
                    }
                ]
            },
            new PortalSectionSpec
            {
                Id = "roadmap",
                Title = "Product Roadmap",
                Status = "planned",
                Tags = ["roadmap"],
                Summary = "High-level phases and milestones.",
                Blocks =
                [
                    new PortalBlockSpec
                    {
                        Id = "phases",
                        Heading = "Product phases",
                        Bullets =
                        [
                            "Phase 1 — Core MVP",
                            "Phase 2 — Expanded capabilities",
                            "Phase 3 — Scale and intelligence"
                        ]
                    }
                ]
            }
        ],
        Features = new PortalFeatures()
    };

    public static List<PortalNavItem> BuildNav(IReadOnlyList<PortalSectionSpec> sections)
    {
        var nav = new List<PortalNavItem>();
        var num = 1;
        foreach (var section in sections)
        {
            nav.Add(new PortalNavItem
            {
                Id = section.Id,
                Num = num++,
                File = $"{section.Id}.html",
                Label = section.Title,
                Available = true
            });
        }
        return nav;
    }

    public static PortalSectionSpec BuildEntitiesSection(
        string applicationName,
        IReadOnlyList<EntitySketch> sketches)
    {
        var byPhase = sketches
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Phase) ? "Core" : s.Phase!)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var blocks = new List<PortalBlockSpec>();
        foreach (var group in byPhase)
        {
            blocks.Add(new PortalBlockSpec
            {
                Id = $"phase-{group.Key.Replace(' ', '-').ToLowerInvariant()}",
                Heading = $"Phase {group.Key}",
                Bullets = group
                    .Select(s => string.IsNullOrWhiteSpace(s.Description)
                        ? s.Name
                        : $"{s.Name} — {s.Description}")
                    .ToList()
            });
        }

        if (blocks.Count == 0)
        {
            blocks.Add(new PortalBlockSpec
            {
                Id = "domain-model",
                Heading = "Domain model",
                Content = $"Define entity sketches in AppGen to populate the {applicationName} domain model."
            });
        }

        return new PortalSectionSpec
        {
            Id = "entities",
            Title = "Database and Entities",
            Status = sketches.Count > 0 ? "in_progress" : "planned",
            Tags = ["entities", "database"],
            Summary = "Core domain entities (sketched — refine properties in AppGen API tab before scaffolding).",
            Blocks = blocks
        };
    }
}
