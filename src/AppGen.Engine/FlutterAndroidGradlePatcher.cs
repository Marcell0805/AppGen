using AppGen.Core;
using System.Text.RegularExpressions;

namespace AppGen.Engine;

public static partial class FlutterAndroidGradlePatcher
{
    private static string CompileSdkReplacement =>
        $"compileSdk = maxOf(flutter.compileSdkVersion, {AppGenConstants.MinAndroidCompileSdk})";

    public static async Task<PlatformPatchResult> PatchAsync(string flutterRoot, CancellationToken ct = default)
    {
        var messages = new List<string>();

        var appPath = Path.Combine(flutterRoot, "android", "app", "build.gradle.kts");
        if (File.Exists(appPath))
        {
            var text = await File.ReadAllTextAsync(appPath, ct);
            if (!text.Contains("maxOf(flutter.compileSdkVersion", StringComparison.Ordinal))
            {
                var updated = CompileSdkLinePattern().Replace(text, CompileSdkReplacement);
                if (!ReferenceEquals(updated, text) && updated != text)
                {
                    await File.WriteAllTextAsync(appPath, updated, ct);
                    messages.Add($"app compileSdk min {AppGenConstants.MinAndroidCompileSdk}");
                }
            }
        }

        var rootPath = Path.Combine(flutterRoot, "android", "build.gradle.kts");
        if (File.Exists(rootPath))
        {
            var rootText = await File.ReadAllTextAsync(rootPath, ct);
            if (!rootText.Contains("LibraryExtension", StringComparison.Ordinal))
            {
                var patched = PatchRootGradleForPlugins(rootText);
                if (patched != rootText)
                {
                    await File.WriteAllTextAsync(rootPath, patched, ct);
                    messages.Add("Android plugin compileSdk alignment");
                }
            }
        }

        if (messages.Count == 0)
            return new PlatformPatchResult(false, string.Empty);

        return new PlatformPatchResult(true, string.Join("; ", messages) + ".");
    }

    internal static string PatchRootGradleForPlugins(string gradleText)
    {
        const string marker = "import com.android.build.gradle.LibraryExtension";
        if (gradleText.Contains(marker, StringComparison.Ordinal))
            return gradleText;

        var minSdk = AppGenConstants.MinAndroidCompileSdk;
        var block =
            "\n\nsubprojects {\n    afterEvaluate {\n        extensions.findByType(LibraryExtension::class.java)?.apply {\n            compileSdk = " +
            minSdk +
            "\n        }\n    }\n}\n";

        if (gradleText.Contains("afterEvaluate", StringComparison.Ordinal) &&
            gradleText.Contains("LibraryExtension", StringComparison.Ordinal))
            return gradleText;

        var insert = marker + block;
        var idx = gradleText.IndexOf("allprojects", StringComparison.Ordinal);
        if (idx < 0)
            return marker + "\n\n" + gradleText + block;

        return gradleText.Insert(idx, insert + "\n\n");
    }

    internal static string PatchCompileSdkLine(string gradleText) =>
        CompileSdkLinePattern().Replace(gradleText, CompileSdkReplacement);

    [GeneratedRegex(@"^\s*compileSdk\s*=\s*flutter\.compileSdkVersion\s*$", RegexOptions.Multiline)]
    private static partial Regex CompileSdkLinePattern();
}
