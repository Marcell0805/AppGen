namespace AppGen.Engine;

internal static class FlutterGeneratedOutputPruner
{
    public static void Prune(
        string flutterRoot,
        IReadOnlyList<string> activeEntitySnakeNames,
        bool authEnabled,
        bool offlineEnabled)
    {
        PruneStaleEntityFeatures(flutterRoot, activeEntitySnakeNames);

        if (!authEnabled)
            PruneAuthArtifacts(flutterRoot);

        if (!offlineEnabled)
            PruneOfflineArtifacts(flutterRoot);
    }

    private static void PruneStaleEntityFeatures(string flutterRoot, IReadOnlyList<string> activeEntitySnakeNames)
    {
        var featuresDir = Path.Combine(flutterRoot, "lib", "features");
        if (!Directory.Exists(featuresDir))
            return;

        var active = new HashSet<string>(activeEntitySnakeNames, StringComparer.OrdinalIgnoreCase);
        foreach (var dir in Directory.EnumerateDirectories(featuresDir))
        {
            var name = Path.GetFileName(dir);
            if (name.Equals("auth", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!active.Contains(name))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static void PruneAuthArtifacts(string flutterRoot)
    {
        DeleteIfExists(Path.Combine(flutterRoot, "lib", "features", "auth"));
        DeleteIfExists(Path.Combine(flutterRoot, "lib", "core", "auth"));
    }

    private static void PruneOfflineArtifacts(string flutterRoot)
    {
        DeleteIfExists(Path.Combine(flutterRoot, "lib", "core", "offline", "offline_cache.dart"));
        DeleteIfExists(Path.Combine(flutterRoot, "lib", "core", "widgets", "offline_banner.dart"));

        var offlineDir = Path.Combine(flutterRoot, "lib", "core", "offline");
        if (Directory.Exists(offlineDir) && !Directory.EnumerateFileSystemEntries(offlineDir).Any())
            Directory.Delete(offlineDir);
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        else if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
