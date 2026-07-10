using System.Text.Json;
using AppGen.Core.Branding;
using AppGen.Core.Models;

namespace AppGen.Engine;

public sealed record ProjectBrandingEmitResult(bool Emitted, string? Message);

public static class ProjectBrandingEmitter
{
    private static readonly (string Folder, int SizePx)[] AndroidLauncherSizes =
    [
        ("mipmap-mdpi", 48),
        ("mipmap-hdpi", 72),
        ("mipmap-xhdpi", 96),
        ("mipmap-xxhdpi", 144),
        ("mipmap-xxxhdpi", 192)
    ];

    private static readonly (string FileName, int SizePx)[] IosAppIconSizes =
    [
        ("Icon-20@2x.png", 40),
        ("Icon-20@3x.png", 60),
        ("Icon-29@2x.png", 58),
        ("Icon-29@3x.png", 87),
        ("Icon-40@2x.png", 80),
        ("Icon-40@3x.png", 120),
        ("Icon-60@2x.png", 120),
        ("Icon-60@3x.png", 180),
        ("Icon-76@2x.png", 152),
        ("Icon-83.5@2x.png", 167),
        ("Icon-1024.png", 1024)
    ];

    public static async Task<ProjectBrandingEmitResult> EmitAllAsync(
        SolutionSpec spec,
        string hubDirectory,
        ProjectBrandingTargets targets,
        CancellationToken ct = default)
    {
        var sourcePath = ProjectBrandingPaths.TryResolveHubIconPath(hubDirectory, spec);
        if (sourcePath is null)
            return new ProjectBrandingEmitResult(false, null);

        var messages = new List<string>();

        if (targets.DocumentationDirectory is not null)
        {
            await EmitDocumentationAsync(sourcePath, targets.DocumentationDirectory, ct);
            messages.Add("Documentation branding");
        }

        if (targets.WebDirectory is not null)
        {
            await EmitMvcWebAsync(sourcePath, targets.WebDirectory, spec, ct);
            messages.Add("Web/MVC branding");
        }

        if (targets.MobileDirectory is not null)
        {
            await EmitMobileAsync(sourcePath, targets.MobileDirectory, ct);
            messages.Add("Mobile branding");
        }

        return new ProjectBrandingEmitResult(true, string.Join(", ", messages));
    }

    public static async Task EmitDocumentationAsync(
        string sourceIconPath,
        string documentationDirectory,
        CancellationToken ct = default)
    {
        var assetsDir = Path.Combine(documentationDirectory, "portal", "assets");
        Directory.CreateDirectory(assetsDir);

        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(assetsDir, "logo.png"), 192, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(assetsDir, "favicon-32.png"), 32, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(assetsDir, "favicon-192.png"), 192, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(assetsDir, "apple-touch-icon.png"), 180, ct);
    }

    public static async Task EmitMvcWebAsync(
        string sourceIconPath,
        string webDirectory,
        SolutionSpec spec,
        CancellationToken ct = default)
    {
        var wwwroot = Path.Combine(webDirectory, "src", spec.MvcProject, "wwwroot");
        if (!Directory.Exists(wwwroot))
            return;

        var brandingDir = Path.Combine(wwwroot, "branding");
        Directory.CreateDirectory(brandingDir);

        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(brandingDir, "logo.png"), 128, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(wwwroot, "favicon.png"), 32, ct);
    }

    public static async Task EmitMobileAsync(
        string sourceIconPath,
        string mobileLayerDirectory,
        CancellationToken ct = default)
    {
        var flutterRoot = FlutterProjectPaths.GetFlutterRoot(mobileLayerDirectory);

        var assetDir = Path.Combine(flutterRoot, "assets", "branding");
        Directory.CreateDirectory(assetDir);
        await IconRasterizer.SaveResizedAsync(
            sourceIconPath,
            Path.Combine(assetDir, "icon.png"),
            ProjectBrandingConstants.RecommendedIconSizePx,
            ct);

        await EmitAndroidLauncherIconsAsync(sourceIconPath, flutterRoot, ct);
        await EmitIosAppIconsAsync(sourceIconPath, flutterRoot, ct);
        await EmitFlutterWebIconsAsync(sourceIconPath, flutterRoot, ct);
    }

    public static async Task EmitAndroidLauncherIconsAsync(
        string sourceIconPath,
        string flutterRoot,
        CancellationToken ct = default)
    {
        var resDir = Path.Combine(flutterRoot, "android", "app", "src", "main", "res");
        if (!Directory.Exists(resDir))
            return;

        foreach (var (folder, size) in AndroidLauncherSizes)
        {
            var targetDir = Path.Combine(resDir, folder);
            Directory.CreateDirectory(targetDir);
            await IconRasterizer.SaveResizedAsync(
                sourceIconPath,
                Path.Combine(targetDir, "ic_launcher.png"),
                size,
                ct);
        }
    }

    public static async Task EmitIosAppIconsAsync(
        string sourceIconPath,
        string flutterRoot,
        CancellationToken ct = default)
    {
        var iconSetDir = Path.Combine(flutterRoot, "ios", "Runner", "Assets.xcassets", "AppIcon.appiconset");
        if (!Directory.Exists(Path.GetDirectoryName(iconSetDir)!))
            return;

        Directory.CreateDirectory(iconSetDir);

        foreach (var (fileName, size) in IosAppIconSizes)
            await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(iconSetDir, fileName), size, ct);

        var contents = BuildIosContentsJson();
        await File.WriteAllTextAsync(Path.Combine(iconSetDir, "Contents.json"), contents, ct);
    }

    public static async Task EmitFlutterWebIconsAsync(
        string sourceIconPath,
        string flutterRoot,
        CancellationToken ct = default)
    {
        var webDir = Path.Combine(flutterRoot, "web");
        if (!Directory.Exists(webDir))
            return;

        var iconsDir = Path.Combine(webDir, "icons");
        Directory.CreateDirectory(iconsDir);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(iconsDir, "Icon-192.png"), 192, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(iconsDir, "Icon-512.png"), 512, ct);
        await IconRasterizer.SaveResizedAsync(sourceIconPath, Path.Combine(webDir, "favicon.png"), 32, ct);
    }

    public static async Task SaveHubIconAsync(
        byte[] pngBytes,
        string hubDirectory,
        CancellationToken ct = default)
    {
        var relative = ProjectBrandingConstants.DefaultIconRelativePath;
        var fullPath = Path.Combine(
            hubDirectory,
            relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllBytesAsync(fullPath, pngBytes, ct);
    }

    public static string? ResolveHubDirectoryFromLayer(string layerDirectory)
    {
        if (File.Exists(Path.Combine(layerDirectory, "appgen.json")))
            return layerDirectory;

        var parent = Directory.GetParent(layerDirectory)?.FullName;
        if (parent is not null && File.Exists(Path.Combine(parent, "appgen.json")))
            return parent;

        return null;
    }

    private static string BuildIosContentsJson()
    {
        var images = new List<object>
        {
            new { idiom = "iphone", scale = "2x", size = "20x20", filename = "Icon-20@2x.png" },
            new { idiom = "iphone", scale = "3x", size = "20x20", filename = "Icon-20@3x.png" },
            new { idiom = "iphone", scale = "2x", size = "29x29", filename = "Icon-29@2x.png" },
            new { idiom = "iphone", scale = "3x", size = "29x29", filename = "Icon-29@3x.png" },
            new { idiom = "iphone", scale = "2x", size = "40x40", filename = "Icon-40@2x.png" },
            new { idiom = "iphone", scale = "3x", size = "40x40", filename = "Icon-40@3x.png" },
            new { idiom = "iphone", scale = "2x", size = "60x60", filename = "Icon-60@2x.png" },
            new { idiom = "iphone", scale = "3x", size = "60x60", filename = "Icon-60@3x.png" },
            new { idiom = "ipad", scale = "2x", size = "76x76", filename = "Icon-76@2x.png" },
            new { idiom = "ipad", scale = "2x", size = "83.5x83.5", filename = "Icon-83.5@2x.png" },
            new { idiom = "ios-marketing", scale = "1x", size = "1024x1024", filename = "Icon-1024.png" }
        };

        return JsonSerializer.Serialize(new { images, info = new { author = "AppGen", version = 1 } }, new JsonSerializerOptions { WriteIndented = true });
    }
}

public sealed class ProjectBrandingTargets
{
    public string? DocumentationDirectory { get; init; }
    public string? WebDirectory { get; init; }
    public string? MobileDirectory { get; init; }
}
