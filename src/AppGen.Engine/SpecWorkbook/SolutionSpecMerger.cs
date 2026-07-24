using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

public static class SolutionSpecMerger
{
    public static SolutionSpec Merge(SolutionSpec? existing, AppGenSpecDocument document, bool mergeEntities)
    {
        existing ??= CreateBaseline(document);
        var database = ParseDatabase(document.Application.Database);
        var entities = BuildEntities(document);

        var mergedEntities = mergeEntities && existing.Entities.Count > 0
            ? MergeEntityLists(existing.Entities, entities)
            : entities;

        var targets = BuildTargets(existing, document);
        var portal = BuildPortal(existing, document);

        return new SolutionSpec
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = document.Application.ApplicationName?.Trim() is { Length: > 0 } name
                ? NamingHelper.NormalizeAppName(name)
                : existing.ApplicationName,
            RootNamespace = document.Application.RootNamespace?.Trim() is { Length: > 0 } ns
                ? NamingHelper.NormalizeAppName(ns)
                : existing.RootNamespace,
            Project = new ProjectInfoSpec
            {
                Tagline = document.Application.Tagline ?? existing.Project?.Tagline,
                Description = document.Application.Description ?? existing.Project?.Description
            },
            Phase = portal is not null && targets.Web.Enabled
                ? ProjectPhase.Both
                : portal is not null
                    ? ProjectPhase.Portal
                    : ProjectPhase.Solution,
            Portal = portal,
            EntitySketches = existing.EntitySketches,
            Targets = targets,
            Generation = existing.Generation,
            Database = database,
            UiTargets = existing.UiTargets,
            Setup = existing.Setup.ConfigEntries.Count > 0
                ? existing.Setup
                : NamingHelper.DefaultSetup(database),
            Entities = mergedEntities
        };
    }

    private static SolutionSpec CreateBaseline(AppGenSpecDocument document)
    {
        var appName = NamingHelper.NormalizeAppName(document.Application.ApplicationName ?? "MyApp");
        var database = ParseDatabase(document.Application.Database);
        return SpecLoader.CreateDefault(appName, document.Application.RootNamespace, database);
    }

    private static List<EntitySpec> BuildEntities(AppGenSpecDocument document)
    {
        var entities = new List<EntitySpec>();
        foreach (var row in document.Entities)
        {
            var properties = document.Properties
                .Where(p => p.EntityName.Equals(row.EntityName, StringComparison.OrdinalIgnoreCase))
                .Select(p => new PropertySpec
                {
                    Name = p.PropertyName,
                    ClrType = NormalizeClrType(p.ClrType),
                    IsKey = p.IsKey,
                    IsNullable = p.IsNullable,
                    ForeignKeyEntity = string.IsNullOrWhiteSpace(p.ForeignKeyEntity) ? null : p.ForeignKeyEntity.Trim(),
                    ColumnName = string.IsNullOrWhiteSpace(p.ColumnName) ? null : p.ColumnName.Trim()
                })
                .ToList();

            if (properties.All(p => !p.IsKey))
            {
                var keyName = NamingHelper.ToKeyPropertyName(row.EntityName);
                properties.Insert(0, new PropertySpec
                {
                    Name = keyName,
                    ClrType = "long",
                    IsKey = true
                });
            }

            entities.Add(new EntitySpec
            {
                Name = row.EntityName.Trim(),
                TableName = string.IsNullOrWhiteSpace(row.TableName) ? null : row.TableName.Trim(),
                IncludeInUi = row.IncludeInUi,
                IncludeAuditColumns = row.IncludeAuditColumns,
                Properties = properties
            });
        }

        return entities;
    }

    private static List<EntitySpec> MergeEntityLists(IReadOnlyList<EntitySpec> existing, IReadOnlyList<EntitySpec> imported)
    {
        var map = existing.ToDictionary(e => e.Name, e => e, StringComparer.OrdinalIgnoreCase);
        foreach (var entity in imported)
            map[entity.Name] = entity;
        return map.Values.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static ApplicationTargets BuildTargets(SolutionSpec existing, AppGenSpecDocument document)
    {
        var prior = existing.Targets ?? SpecNormalizer.BuildTargetsFromLegacy(existing);
        var app = document.Application;

        return new ApplicationTargets
        {
            Documentation = new DocumentationTargetSpec
            {
                Enabled = app.EnableDocumentation ?? prior.Documentation.Enabled,
                Preset = app.DocumentationPreset ?? prior.Documentation.Preset
            },
            Web = new WebTargetSpec
            {
                Enabled = app.EnableWeb ?? prior.Web.Enabled,
                Auth = new WebAuthTargetSpec
                {
                    Enabled = app.EnableWebAuth ?? prior.Web.Auth.Enabled,
                    Issuer = prior.Web.Auth.Issuer,
                    TokenLifetimeMinutes = prior.Web.Auth.TokenLifetimeMinutes
                }
            },
            Mobile = new MobileTargetSpec
            {
                Enabled = app.EnableMobile ?? prior.Mobile.Enabled,
                Framework = prior.Mobile.Framework,
                PackageName = string.IsNullOrWhiteSpace(prior.Mobile.PackageName)
                    ? $"com.{NamingHelper.NormalizeAppName(existing.ApplicationName).ToLowerInvariant()}.app"
                    : prior.Mobile.PackageName,
                ApiBaseUrl = app.MobileApiBaseUrl ?? prior.Mobile.ApiBaseUrl,
                StateManagement = prior.Mobile.StateManagement,
                Theme = new MobileThemeSpec
                {
                    Preset = app.MobileThemePreset ?? prior.Mobile.Theme.Preset,
                    PrimaryColor = prior.Mobile.Theme.PrimaryColor,
                    AccentColor = prior.Mobile.Theme.AccentColor,
                    BackgroundColor = prior.Mobile.Theme.BackgroundColor,
                    HighlightColor = prior.Mobile.Theme.HighlightColor
                },
                Offline = new MobileOfflineTargetSpec
                {
                    Enabled = app.EnableMobileOffline ?? prior.Mobile.Offline.Enabled,
                    Provider = prior.Mobile.Offline.Provider
                },
                Capabilities = prior.Mobile.Capabilities
            }
        };
    }

    private static PortalSpec? BuildPortal(SolutionSpec existing, AppGenSpecDocument document)
    {
        if (document.Sections.Count == 0)
            return existing.Portal;

        var preset = document.Application.DocumentationPreset
            ?? existing.Portal?.Preset
            ?? "engineering-portal";

        var settings = existing.Portal?.Settings ?? PortalDefaults.CreateDefaultPortal(
            existing.ApplicationName,
            preset).Settings;

        var sections = document.Sections.Select(s => new PortalSectionSpec
        {
            Id = s.SectionId.Trim(),
            Title = s.Title.Trim(),
            Status = string.IsNullOrWhiteSpace(s.Status) ? "planned" : s.Status.Trim(),
            Summary = s.Summary,
            Tags = ParseTags(s.Tags),
            Blocks = existing.Portal?.Sections
                .FirstOrDefault(x => x.Id.Equals(s.SectionId, StringComparison.OrdinalIgnoreCase))
                ?.Blocks ?? []
        }).ToList();

        var nav = document.Sections
            .Where(s => s.NavNum.HasValue)
            .OrderBy(s => s.NavNum)
            .Select(s => new PortalNavItem
            {
                Id = s.SectionId.Trim(),
                Num = s.NavNum ?? 0,
                Label = s.NavLabel ?? s.Title,
                File = $"{s.SectionId.Trim()}.html",
                Available = true
            })
            .ToList();

        if (nav.Count == 0 && existing.Portal?.Nav.Count > 0)
            nav = existing.Portal.Nav.ToList();

        return new PortalSpec
        {
            Preset = preset,
            Settings = settings,
            Nav = nav,
            Sections = sections,
            Features = existing.Portal?.Features ?? new PortalFeatures()
        };
    }

    private static List<string> ParseTags(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string NormalizeClrType(string clrType) =>
        clrType.Trim() switch
        {
            "datetime" => "DateTime",
            "dateonly" => "DateOnly",
            "guid" => "Guid",
            "bool" => "bool",
            "int" => "int",
            "long" => "long",
            "decimal" => "decimal",
            "double" => "double",
            "string" => "string",
            _ => clrType.Trim()
        };

    private static DatabaseProvider ParseDatabase(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DatabaseProvider.SqlServer;

        if (raw.Equals("Postgres", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            return DatabaseProvider.PostgreSql;

        return Enum.TryParse<DatabaseProvider>(raw, ignoreCase: true, out var db)
            ? db
            : DatabaseProvider.SqlServer;
    }
}
