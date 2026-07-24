using System.Text.Json;
using System.Text.Json.Nodes;
using AppGen.Core.Models;
using AppGen.Core.Themes;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed record ThemeEmitTargets(
    string? DocumentationDirectory = null,
    string? WebDirectory = null,
    string? MobileDirectory = null);

public sealed record ThemeEmitResult(bool Success, string Message, IReadOnlyList<string> UpdatedLayers);

public static class ThemeEmitter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<ThemeEmitResult> ApplyAsync(
        SolutionSpec spec,
        ThemeEmitTargets targets,
        TemplateRenderer? renderer = null,
        CancellationToken ct = default)
    {
        renderer ??= new TemplateRenderer();
        var updated = new List<string>();
        var skipped = new List<string>();

        if (targets.DocumentationDirectory is not null)
        {
            if (await TryApplyDocumentationAsync(spec, targets.DocumentationDirectory, ct))
                updated.Add("Documentation");
            else
                skipped.Add("Documentation (not generated yet)");
        }

        if (targets.WebDirectory is not null)
        {
            if (await TryApplyWebAsync(spec, targets.WebDirectory, renderer, ct))
                updated.Add("Web");
            else
                skipped.Add("Web (not generated yet)");
        }

        if (targets.MobileDirectory is not null)
        {
            if (await TryApplyMobileAsync(spec, targets.MobileDirectory, renderer, ct))
                updated.Add("Mobile");
            else
                skipped.Add("Mobile (not generated yet)");
        }

        if (updated.Count == 0)
        {
            var detail = skipped.Count > 0
                ? string.Join("; ", skipped)
                : "No generated outputs found yet.";
            return new ThemeEmitResult(false, detail, updated);
        }

        var message = $"Updated {string.Join(" and ", updated)}.";
        if (skipped.Count > 0)
            message += $" Skipped: {string.Join("; ", skipped)}.";

        return new ThemeEmitResult(true, message, updated);
    }

    private static async Task<bool> TryApplyDocumentationAsync(
        SolutionSpec spec,
        string documentationDirectory,
        CancellationToken ct)
    {
        var portalDir = Path.Combine(documentationDirectory, "portal");
        var cssPath = Path.Combine(portalDir, "css", "theme-overrides.css");
        if (!Directory.Exists(portalDir) && !File.Exists(cssPath))
            return false;

        var preset = ResolvePreset(spec);
        var theme = PortalThemeCss.Resolve(preset, spec.Portal?.Settings.Theme);
        var cssDir = Path.Combine(portalDir, "css");
        Directory.CreateDirectory(cssDir);
        await File.WriteAllTextAsync(Path.Combine(cssDir, "theme-overrides.css"), PortalThemeCss.ToCss(theme), ct);

        var settingsPath = Path.Combine(portalDir, "data", "portal-settings.json");
        if (File.Exists(settingsPath))
            await PatchPortalSettingsThemeAsync(settingsPath, preset, ct);

        return true;
    }

    private static async Task PatchPortalSettingsThemeAsync(string settingsPath, string preset, CancellationToken ct)
    {
        var portalTheme = MobileThemeCatalog.ToPortalThemeSettings(preset);
        var text = await File.ReadAllTextAsync(settingsPath, ct);
        var root = JsonNode.Parse(text) as JsonObject ?? new JsonObject();
        root["theme"] = new JsonObject
        {
            ["primaryColor"] = portalTheme.PrimaryColor,
            ["accentColor"] = portalTheme.AccentColor,
            ["highlightColor"] = portalTheme.HighlightColor,
            ["backgroundColor"] = portalTheme.BackgroundColor
        };
        await File.WriteAllTextAsync(settingsPath, root.ToJsonString(JsonOptions) + Environment.NewLine, ct);
    }

    private static async Task<bool> TryApplyWebAsync(
        SolutionSpec spec,
        string webDirectory,
        TemplateRenderer renderer,
        CancellationToken ct)
    {
        var mvcRoot = FindMvcProjectRoot(webDirectory, spec.ApplicationName);
        if (mvcRoot is null)
            return false;

        var siteCss = Path.Combine(mvcRoot, "wwwroot", "css", "site.css");
        var layout = Path.Combine(mvcRoot, "Views", "Shared", "_Layout.cshtml");
        if (!File.Exists(siteCss) && !File.Exists(layout))
            return false;

        var model = SolutionGenerator.BuildModel(EnsureMvcTarget(spec));
        if (File.Exists(siteCss) || Directory.Exists(Path.GetDirectoryName(siteCss)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(siteCss)!);
            var css = renderer.Render(TemplateProvider.Load("Solution/mvc/wwwroot/css/site.scriban"), model);
            await File.WriteAllTextAsync(siteCss, css, ct);
        }

        if (File.Exists(layout) || Directory.Exists(Path.GetDirectoryName(layout)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(layout)!);
            var html = renderer.Render(TemplateProvider.Load("Solution/mvc/Views/Shared/_Layout.scriban"), model);
            await File.WriteAllTextAsync(layout, html, ct);
        }

        return File.Exists(siteCss) || File.Exists(layout);
    }

    private static SolutionSpec EnsureMvcTarget(SolutionSpec spec)
    {
        if (spec.UiTargets.HasFlag(UiTarget.MvcWeb))
            return spec;

        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = spec.Project,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = spec.Targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets | UiTarget.MvcWeb,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }

    private static string? FindMvcProjectRoot(string webDirectory, string applicationName)
    {
        var expected = Path.Combine(webDirectory, "src", $"{applicationName}.MVC");
        if (Directory.Exists(expected))
            return expected;

        var src = Path.Combine(webDirectory, "src");
        if (!Directory.Exists(src))
            return null;

        return Directory.GetDirectories(src, "*.MVC")
            .FirstOrDefault(d => File.Exists(Path.Combine(d, "wwwroot", "css", "site.css"))
                                 || File.Exists(Path.Combine(d, "Views", "Shared", "_Layout.cshtml")));
    }

    private static async Task<bool> TryApplyMobileAsync(
        SolutionSpec spec,
        string mobileDirectory,
        TemplateRenderer renderer,
        CancellationToken ct)
    {
        var configPath = Path.Combine(mobileDirectory, "lib", "app", "app_theme_config.dart");
        if (!File.Exists(configPath) && !Directory.Exists(Path.Combine(mobileDirectory, "lib")))
            return false;

        var mobile = spec.Targets?.Mobile ?? new MobileTargetSpec { Enabled = true };
        var entities = spec.Entities.Count > 0
            ? spec.Entities
            : [CreatePlaceholderEntity()];

        var model = FlutterGenerator.BuildAppModel(spec, mobile, entities);
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        var content = renderer.Render(TemplateProvider.Load("Mobile/flutter/app_theme_config.dart.scriban"), model);
        await File.WriteAllTextAsync(configPath, content, ct);
        return true;
    }

    private static EntitySpec CreatePlaceholderEntity() =>
        new()
        {
            Name = "Item",
            Properties =
            [
                new PropertySpec { Name = "Id", ClrType = "int", IsKey = true },
                new PropertySpec { Name = "Name", ClrType = "string" }
            ]
        };

    private static string ResolvePreset(SolutionSpec spec) =>
        MobileThemeCatalog.NormalizePreset(spec.Targets?.Mobile.Theme?.Preset);
}
