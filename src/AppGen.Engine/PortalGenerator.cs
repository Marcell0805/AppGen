using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class PortalGenerator(TemplateRenderer renderer)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<SolutionSpec> GenerateAsync(
        SolutionSpec spec,
        string projectDirectory,
        bool overwritePortal = false,
        CancellationToken ct = default)
    {
        if (spec.Portal is null)
            throw new InvalidOperationException("Project spec does not include portal configuration.");

        var portalDir = Path.Combine(projectDirectory, "portal");
        if (Directory.Exists(portalDir) && Directory.EnumerateFileSystemEntries(portalDir).Any() && !overwritePortal)
            throw new InvalidOperationException($"Portal directory is not empty: {portalDir}");

        Directory.CreateDirectory(projectDirectory);
        if (overwritePortal && Directory.Exists(portalDir))
            Directory.Delete(portalDir, recursive: true);

        var prepared = PrepareSpec(spec);
        var portal = prepared.Portal!;
        var preset = portal.Preset;

        await WriteDataFilesAsync(portal, portalDir, ct);
        await CopyStaticAssetsAsync(preset, portalDir, portal.Features, ct);
        await WriteIndexHtmlAsync(prepared, portalDir, portal.Features, preset, ct);
        await WriteSectionShellsAsync(portal, portalDir, portal.Features, ct);
        await PortalDataBuilder.BuildAsync(portalDir, portal.Features, ct);
        await ProjectSpecWriter.WriteAsync(prepared, projectDirectory, ct);

        return prepared;
    }

    internal static SolutionSpec PrepareSpec(SolutionSpec spec)
    {
        if (spec.Portal is null)
            throw new InvalidOperationException("Portal configuration is required.");

        var sections = spec.Portal.Sections
            .Where(s => !string.Equals(s.Id, "entities", StringComparison.OrdinalIgnoreCase))
            .ToList();
        sections.Add(PortalDefaults.BuildEntitiesSection(spec.ApplicationName, spec.EntitySketches));
        var nav = PortalDefaults.BuildNav(sections);

        var portal = new PortalSpec
        {
            Preset = spec.Portal.Preset,
            Settings = spec.Portal.Settings,
            Sections = sections,
            Nav = nav,
            Features = spec.Portal.Features
        };

        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = ProjectPhase.Portal,
            Portal = portal,
            EntitySketches = spec.EntitySketches,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }

    private static async Task WriteDataFilesAsync(PortalSpec portal, string portalDir, CancellationToken ct)
    {
        var dataDir = Path.Combine(portalDir, "data");
        Directory.CreateDirectory(dataDir);

        await WriteJsonAsync(Path.Combine(dataDir, "portal-settings.json"), MapSettings(portal.Settings), ct);
        await WriteJsonAsync(Path.Combine(dataDir, "nav.json"), new { items = portal.Nav }, ct);

        foreach (var section in portal.Sections)
            await WriteJsonAsync(Path.Combine(dataDir, $"{section.Id}.json"), section, ct);
    }

    private static object MapSettings(PortalSettings settings) => new
    {
        portalName = settings.PortalName,
        tagline = settings.Tagline,
        version = settings.Version,
        homeQuote = settings.HomeQuote,
        maintainerDocsUrl = settings.MaintainerDocsUrl,
        productRepoUrl = settings.ProductRepoUrl,
        auth = new
        {
            password = settings.Auth.Password,
            storageKey = settings.Auth.StorageKey
        },
        theme = new
        {
            primaryColor = settings.Theme.PrimaryColor,
            accentColor = settings.Theme.AccentColor,
            highlightColor = settings.Theme.HighlightColor,
            backgroundColor = settings.Theme.BackgroundColor
        }
    };

    private async Task WriteIndexHtmlAsync(
        SolutionSpec spec,
        string portalDir,
        PortalFeatures features,
        string preset,
        CancellationToken ct)
    {
        var settings = spec.Portal!.Settings;
        var model = new
        {
            portal_name = settings.PortalName,
            portal_title = settings.PortalName.ToUpperInvariant(),
            app_name = spec.ApplicationName,
            tagline = settings.Tagline ?? string.Empty,
            home_quote = settings.HomeQuote ?? string.Empty,
            password_gate = features.PasswordGate,
            search_enabled = features.Search
        };

        var template = TemplateProvider.Load($"Portal/{preset}/index.html.scriban");
        var html = renderer.Render(template, model);
        await File.WriteAllTextAsync(Path.Combine(portalDir, "index.html"), html, ct);
    }

    private static async Task WriteSectionShellsAsync(PortalSpec portal, string portalDir, PortalFeatures features, CancellationToken ct)
    {
        var sectionsDir = Path.Combine(portalDir, "sections");
        Directory.CreateDirectory(sectionsDir);
        foreach (var section in portal.Sections)
        {
            var html = PortalSectionShell.Render(section.Id, section.Title, features);
            await File.WriteAllTextAsync(Path.Combine(sectionsDir, $"{section.Id}.html"), html, ct);
        }
    }

    private static async Task CopyStaticAssetsAsync(
        string preset,
        string portalDir,
        PortalFeatures features,
        CancellationToken ct)
    {
        var assetPrefix = $"Portal/{preset}/";
        var mappings = new (string Asset, string Output)[]
        {
            ("css/portal.css", "css/portal.css"),
            ("js/portal.js", "js/portal.js"),
            ("js/auth.js", "js/auth.js"),
            ("assets/logo.svg", "assets/logo.svg"),
            ("scripts/build-portal.ps1", "scripts/build-portal.ps1")
        };

        foreach (var (asset, output) in mappings)
        {
            if (asset == "js/auth.js" && !features.PasswordGate)
                continue;

            var bytes = TemplateProvider.LoadBytes($"{assetPrefix}{asset}");
            var fullPath = Path.Combine(portalDir, output);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);
        }

        if (features.Search)
        {
            foreach (var asset in new[] { "js/search.js", "js/vendor/fuse.min.js" })
            {
                var bytes = TemplateProvider.LoadBytes($"{assetPrefix}{asset}");
                var fullPath = Path.Combine(portalDir, asset.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllBytesAsync(fullPath, bytes, ct);
            }
        }
    }

    private static async Task WriteJsonAsync(string path, object value, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        await File.WriteAllTextAsync(path, json + Environment.NewLine, ct);
    }
}
