using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class PortalSpecImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(), new UiTargetJsonConverter() }
    };

    public static async Task<SolutionSpec> ImportAsync(string projectDirectory, CancellationToken ct = default)
    {
        var portalDataDir = Path.Combine(projectDirectory, "portal", "data");
        if (!Directory.Exists(portalDataDir))
            throw new DirectoryNotFoundException($"Portal data folder not found: {portalDataDir}");

        var manifestPath = Path.Combine(projectDirectory, "appgen.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException("appgen.json not found in project directory.", manifestPath);

        var manifestJson = await File.ReadAllTextAsync(manifestPath, ct);
        var spec = JsonSerializer.Deserialize<SolutionSpec>(manifestJson, Options)
            ?? throw new InvalidOperationException("Failed to deserialize appgen.json.");

        if (spec.Portal is null)
            throw new InvalidOperationException("appgen.json does not include portal configuration.");

        var portal = spec.Portal;
        var settings = portal.Settings;

        var settingsPath = Path.Combine(portalDataDir, "portal-settings.json");
        if (File.Exists(settingsPath))
        {
            await using var stream = File.OpenRead(settingsPath);
            var settingsDoc = await JsonSerializer.DeserializeAsync<PortalSettingsImport>(stream, Options, ct);
            if (settingsDoc is not null)
                settings = MapSettings(settingsDoc);
        }

        var nav = portal.Nav;
        var navPath = Path.Combine(portalDataDir, "nav.json");
        if (File.Exists(navPath))
        {
            await using var stream = File.OpenRead(navPath);
            var navDoc = await JsonSerializer.DeserializeAsync<NavImport>(stream, Options, ct);
            if (navDoc?.Items is not null)
                nav = navDoc.Items;
        }

        var sections = new List<PortalSectionSpec>();
        foreach (var file in Directory.GetFiles(portalDataDir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var name = Path.GetFileName(file);
            if (name is "portal-settings.json" or "nav.json" or "entities.json")
                continue;

            await using var stream = File.OpenRead(file);
            var section = await JsonSerializer.DeserializeAsync<PortalSectionSpec>(stream, Options, ct);
            if (section is not null)
                sections.Add(section);
        }

        if (sections.Count == 0)
            sections = portal.Sections.ToList();

        portal = new PortalSpec
        {
            Preset = portal.Preset,
            Settings = settings,
            Nav = nav,
            Sections = sections,
            Features = portal.Features
        };

        spec = new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = spec.Phase,
            Portal = portal,
            EntitySketches = spec.EntitySketches,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };

        await ProjectSpecWriter.WriteAsync(spec, projectDirectory, ct);
        return spec;
    }

    private static PortalSettings MapSettings(PortalSettingsImport doc) => new()
    {
        PortalName = doc.PortalName ?? string.Empty,
        Tagline = doc.Tagline,
        Version = doc.Version,
        HomeQuote = doc.HomeQuote,
        MaintainerDocsUrl = doc.MaintainerDocsUrl,
        ProductRepoUrl = doc.ProductRepoUrl,
        Auth = doc.Auth is null
            ? new PortalAuthSettings()
            : new PortalAuthSettings
            {
                Password = doc.Auth.Password ?? "portal",
                StorageKey = doc.Auth.StorageKey ?? "appgen_portal_auth"
            },
        Theme = doc.Theme is null
            ? new PortalThemeSettings()
            : new PortalThemeSettings
            {
                PrimaryColor = doc.Theme.PrimaryColor ?? "#1B3A5C",
                AccentColor = doc.Theme.AccentColor ?? "#2E75B6",
                HighlightColor = doc.Theme.HighlightColor ?? "#F28C28",
                BackgroundColor = doc.Theme.BackgroundColor ?? "#E8F1F8"
            }
    };

    private sealed class PortalSettingsImport
    {
        public string? PortalName { get; init; }
        public string? Tagline { get; init; }
        public string? Version { get; init; }
        public string? HomeQuote { get; init; }
        public string? MaintainerDocsUrl { get; init; }
        public string? ProductRepoUrl { get; init; }
        public PortalAuthImport? Auth { get; init; }
        public PortalThemeImport? Theme { get; init; }
    }

    private sealed class PortalAuthImport
    {
        public string? Password { get; init; }
        public string? StorageKey { get; init; }
    }

    private sealed class PortalThemeImport
    {
        public string? PrimaryColor { get; init; }
        public string? AccentColor { get; init; }
        public string? HighlightColor { get; init; }
        public string? BackgroundColor { get; init; }
    }

    private sealed class NavImport
    {
        public List<PortalNavItem>? Items { get; init; }
    }
}
