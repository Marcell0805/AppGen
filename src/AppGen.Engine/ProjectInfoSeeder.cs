using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class ProjectInfoSeeder
{
    public static SolutionSpec ApplyToPortalSpec(SolutionSpec spec)
    {
        if (spec.Portal is null || string.IsNullOrWhiteSpace(spec.Project?.Description))
            return spec;

        var description = spec.Project.Description.Trim();
        var appName = spec.ApplicationName;
        var defaultPlaceholder = $"Describe what {appName} will deliver and who it serves.";

        var sections = spec.Portal.Sections.Select(section =>
        {
            if (!string.Equals(section.Id, "vision", StringComparison.OrdinalIgnoreCase))
                return section;

            var blocks = section.Blocks.Select(block =>
            {
                if (!string.Equals(block.Id, "vision-statement", StringComparison.OrdinalIgnoreCase))
                    return block;

                var content = block.Content?.Trim();
                if (!string.IsNullOrWhiteSpace(content) &&
                    !content.Equals(defaultPlaceholder, StringComparison.Ordinal))
                    return block;

                return new PortalBlockSpec
                {
                    Id = block.Id,
                    Heading = block.Heading,
                    Content = description,
                    Bullets = block.Bullets
                };
            }).ToList();

            return new PortalSectionSpec
            {
                Id = section.Id,
                Title = section.Title,
                Status = section.Status,
                Tags = section.Tags,
                Summary = section.Summary,
                Blocks = blocks
            };
        }).ToList();

        var portal = new PortalSpec
        {
            Preset = spec.Portal.Preset,
            Settings = spec.Portal.Settings,
            Nav = spec.Portal.Nav,
            Sections = sections,
            Features = spec.Portal.Features
        };

        if (!string.IsNullOrWhiteSpace(spec.Project.Tagline))
        {
            portal = new PortalSpec
            {
                Preset = portal.Preset,
                Settings = new PortalSettings
                {
                    PortalName = portal.Settings.PortalName,
                    Tagline = spec.Project.Tagline.Trim(),
                    Version = portal.Settings.Version,
                    HomeQuote = portal.Settings.HomeQuote,
                    MaintainerDocsUrl = portal.Settings.MaintainerDocsUrl,
                    ProductRepoUrl = portal.Settings.ProductRepoUrl,
                    Auth = portal.Settings.Auth,
                    Theme = portal.Settings.Theme
                },
                Nav = portal.Nav,
                Sections = portal.Sections,
                Features = portal.Features
            };
        }

        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = spec.Project,
            Phase = spec.Phase,
            Portal = portal,
            EntitySketches = spec.EntitySketches,
            Targets = spec.Targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }
}
