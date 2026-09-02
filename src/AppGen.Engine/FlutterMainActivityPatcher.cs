using AppGen.Templates;

namespace AppGen.Engine;

public static class FlutterMainActivityPatcher
{
    public static async Task<PlatformPatchResult> PatchAsync(
        TemplateRenderer renderer,
        string flutterRoot,
        string packageName,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return new PlatformPatchResult(false, string.Empty);

        var kotlinRelative = packageName.Replace('.', Path.DirectorySeparatorChar);
        var mainActivityPath = Path.Combine(
            flutterRoot,
            "android",
            "app",
            "src",
            "main",
            "kotlin",
            kotlinRelative,
            "MainActivity.kt");

        if (!File.Exists(mainActivityPath))
        {
            var kotlinRoot = Path.Combine(flutterRoot, "android", "app", "src", "main", "kotlin");
            if (Directory.Exists(kotlinRoot))
            {
                var found = Directory
                    .GetFiles(kotlinRoot, "MainActivity.kt", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (found is not null)
                    mainActivityPath = found;
            }
        }

        var directory = Path.GetDirectoryName(mainActivityPath);
        if (string.IsNullOrWhiteSpace(directory))
            return new PlatformPatchResult(false, string.Empty);

        Directory.CreateDirectory(directory);

        var model = new { package_name = packageName.Trim() };
        var content = renderer.Render(TemplateProvider.Load("Mobile/flutter/MainActivity.kt.scriban"), model);
        await File.WriteAllTextAsync(mainActivityPath, content, ct);
        return new PlatformPatchResult(true, "MainActivity back handler patched.");
    }
}
