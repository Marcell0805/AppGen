using System.Text;
using System.Xml.Linq;
using AppGen.Core.Capabilities;

namespace AppGen.Engine;

public static class FlutterPlatformConfigPatcher
{
    private const string MarkerPrefix = "AppGen:capability:";

    public static async Task<PlatformPatchResult> PatchAsync(
        string flutterRoot,
        IReadOnlyList<MobileCapabilityDefinition> capabilities,
        CancellationToken ct = default)
    {
        var androidPath = Path.Combine(flutterRoot, "android", "app", "src", "main", "AndroidManifest.xml");
        var iosPath = Path.Combine(flutterRoot, "ios", "Runner", "Info.plist");

        var messages = new List<string>();
        var needsPlatform = capabilities.Any(c =>
            c.AndroidPermissions.Count > 0 || c.IosPlistKeys.Count > 0);

        if (File.Exists(androidPath))
        {
            await PatchAndroidManifestAsync(androidPath, capabilities, ct);
            messages.Add("Android permissions patched.");
        }

        if (File.Exists(iosPath))
        {
            await PatchIosPlistAsync(iosPath, capabilities, ct);
            messages.Add("iOS Info.plist patched.");
        }

        if (messages.Count == 0 && needsPlatform)
        {
            await WriteCapabilitiesChecklistAsync(flutterRoot, capabilities, ct);
            messages.Add("Platform folders missing — wrote CAPABILITIES.md checklist.");
        }

        if (capabilities.Any(c => c.RequiresNativePlatform))
        {
            await WriteNativeLaunchGuidanceAsync(flutterRoot, ct);
            messages.Add("Native capabilities enabled — use Android/iOS launch target (not Chrome).");
        }

        return new PlatformPatchResult(messages.Count > 0, string.Join(" ", messages));
    }

    private static async Task PatchAndroidManifestAsync(
        string path,
        IReadOnlyList<MobileCapabilityDefinition> capabilities,
        CancellationToken ct)
    {
        var doc = XDocument.Load(path, LoadOptions.PreserveWhitespace);
        var manifest = doc.Root ?? throw new InvalidOperationException("AndroidManifest root missing.");
        var ns = manifest.Name.Namespace;

        RemoveAppGenBlocks(manifest);

        var application = manifest.Elements().FirstOrDefault(e => e.Name.LocalName == "application");
        var insertBefore = application ?? manifest.Elements().LastOrDefault();

        foreach (var capability in capabilities.Where(c => c.AndroidPermissions.Count > 0))
        {
            var marker = new XComment($" {MarkerPrefix}{capability.Id} ");
            if (insertBefore is not null)
                insertBefore.AddBeforeSelf(marker);
            else
                manifest.Add(marker);

            foreach (var permission in capability.AndroidPermissions)
            {
                var fullName = $"android.permission.{permission}";
                if (manifest.ToString().Contains(fullName, StringComparison.Ordinal))
                    continue;

                var element = new XElement(ns + "uses-permission", new XAttribute(ns + "name", fullName));
                if (insertBefore is not null)
                    insertBefore.AddBeforeSelf(element);
                else
                    manifest.Add(element);
            }
        }

        await File.WriteAllTextAsync(path, doc.ToString(), ct);
    }

    private static void RemoveAppGenBlocks(XElement manifest)
    {
        var comments = manifest.Nodes().OfType<XComment>()
            .Where(c => c.Value.Contains(MarkerPrefix, StringComparison.Ordinal))
            .ToList();

        foreach (var comment in comments)
        {
            var next = comment.NextNode;
            while (next is XElement { Name.LocalName: "uses-permission" } perm)
            {
                var toRemove = perm;
                next = perm.NextNode;
                toRemove.Remove();
            }

            comment.Remove();
        }
    }

    private static async Task PatchIosPlistAsync(
        string path,
        IReadOnlyList<MobileCapabilityDefinition> capabilities,
        CancellationToken ct)
    {
        var text = await File.ReadAllTextAsync(path, ct);

        foreach (var capability in capabilities)
        {
            foreach (var key in capability.IosPlistKeys)
            {
                if (text.Contains(key, StringComparison.Ordinal))
                    continue;

                var description = key switch
                {
                    "NSCameraUsageDescription" => "This app needs camera access to capture photos.",
                    "NSLocationWhenInUseUsageDescription" => "This app needs your location to show nearby data.",
                    "NSFaceIDUsageDescription" => "This app uses Face ID for secure sign-in.",
                    _ => $"This app needs access for {capability.DisplayName}."
                };

                var snippet = $"\n\t<key>{key}</key>\n\t<string>{description}</string>";
                var insertAt = text.LastIndexOf("</dict>", StringComparison.Ordinal);
                if (insertAt >= 0)
                    text = text.Insert(insertAt, snippet);
            }
        }

        await File.WriteAllTextAsync(path, text, ct);
    }

    private static async Task WriteCapabilitiesChecklistAsync(
        string flutterRoot,
        IReadOnlyList<MobileCapabilityDefinition> capabilities,
        CancellationToken ct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AppGen capabilities — manual platform setup");
        sb.AppendLine();
        sb.AppendLine("Flutter platforms were not scaffolded. After running `flutter create`, apply:");
        sb.AppendLine();

        foreach (var c in capabilities.Where(x => x.AndroidPermissions.Count > 0 || x.IosPlistKeys.Count > 0))
        {
            sb.AppendLine($"## {c.DisplayName} (`{c.Id}`)");
            if (c.AndroidPermissions.Count > 0)
                sb.AppendLine("- Android: " + string.Join(", ", c.AndroidPermissions.Select(p => $"android.permission.{p}")));
            if (c.IosPlistKeys.Count > 0)
                sb.AppendLine("- iOS: " + string.Join(", ", c.IosPlistKeys));
            sb.AppendLine();
        }

        await File.WriteAllTextAsync(Path.Combine(flutterRoot, "CAPABILITIES.md"), sb.ToString(), ct);
    }

    private static async Task WriteNativeLaunchGuidanceAsync(string flutterRoot, CancellationToken ct)
    {
        var path = Path.Combine(flutterRoot, "MOBILE_RUN.md");
        var sb = new StringBuilder();
        sb.AppendLine("# AppGen — run on a native device");
        sb.AppendLine();
        sb.AppendLine("This project includes capabilities that use native plugins (`dart:ffi` / `dart:io`).");
        sb.AppendLine("They **cannot** compile for Chrome/web and may crash the Dart compiler with:");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine("type 'InvalidType' is not a subtype of type 'FunctionType' in type cast");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## What to do");
        sb.AppendLine();
        sb.AppendLine("1. In VS Code/Cursor, pick **Android** (or iOS) from the Run and Debug dropdown — not Chrome.");
        sb.AppendLine("2. Ensure platform folders exist: `flutter create . --platforms android,ios,windows,web`");
        sb.AppendLine("3. Run `flutter pub get` after regenerating.");
        sb.AppendLine();
        await File.WriteAllTextAsync(path, sb.ToString(), ct);
    }
}

public sealed record PlatformPatchResult(bool Patched, string Message);
