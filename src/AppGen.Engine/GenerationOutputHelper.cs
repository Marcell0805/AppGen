using AppGen.Core;

namespace AppGen.Engine;

public enum ProjectOutputLayer
{
    Hub,
    Documentation,
    Web,
    Mobile
}

public static class GenerationOutputHelper
{
    public static string GetLayerFolderName(string applicationName, ProjectOutputLayer layer)
    {
        var normalized = NamingHelper.NormalizeAppName(applicationName);
        return layer switch
        {
            ProjectOutputLayer.Hub => normalized,
            ProjectOutputLayer.Documentation => $"{normalized} Doc",
            ProjectOutputLayer.Web => $"{normalized} Web",
            ProjectOutputLayer.Mobile => $"{normalized} Mobile",
            _ => normalized
        };
    }

    public static string ResolveLayerDirectory(
        string outputRootDirectory,
        string applicationName,
        ProjectOutputLayer layer) =>
        Path.GetFullPath(Path.Combine(
            outputRootDirectory.Trim(),
            GetLayerFolderName(applicationName, layer)));

    /// <summary>
    /// When --project points at a hub, layer, or output root folder, returns the output root
    /// that contains layer folders ({AppName}, {AppName} Mobile, etc.).
    /// </summary>
    public static string ResolveOutputRootFromManifestDirectory(string manifestDirectory, string applicationName)
    {
        var dir = Path.GetFullPath(manifestDirectory.Trim());
        var folder = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var hub = GetLayerFolderName(applicationName, ProjectOutputLayer.Hub);
        var mobile = GetLayerFolderName(applicationName, ProjectOutputLayer.Mobile);
        var web = GetLayerFolderName(applicationName, ProjectOutputLayer.Web);
        var doc = GetLayerFolderName(applicationName, ProjectOutputLayer.Documentation);

        if (folder.Equals(mobile, StringComparison.OrdinalIgnoreCase) ||
            folder.Equals(web, StringComparison.OrdinalIgnoreCase) ||
            folder.Equals(doc, StringComparison.OrdinalIgnoreCase) ||
            folder.Equals(hub, StringComparison.OrdinalIgnoreCase))
        {
            var parent = Path.GetDirectoryName(dir);
            if (!string.IsNullOrEmpty(parent))
                return parent;
        }

        var outputParent = Path.GetDirectoryName(dir);
        if (!string.IsNullOrEmpty(outputParent) &&
            HasSiblingLayerFolders(outputParent, folder, web, mobile, doc))
        {
            return outputParent;
        }

        return dir;
    }

    /// <summary>
    /// Returns existing layer directories for a manifest folder, including hub-folder-named siblings
    /// when the on-disk hub name differs from <paramref name="applicationName"/>.
    /// </summary>
    public static IEnumerable<string> ResolveExistingLayerDirectories(
        string manifestDirectory,
        string applicationName,
        ProjectOutputLayer layer)
    {
        var manifestDir = Path.GetFullPath(manifestDirectory.Trim());
        var outputRoot = ResolveOutputRootFromManifestDirectory(manifestDir, applicationName);
        var hubFolder = Path.GetFileName(manifestDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

        var candidates = new List<string> { ResolveLayerDirectory(outputRoot, applicationName, layer) };

        var hubBasedName = layer switch
        {
            ProjectOutputLayer.Documentation => $"{hubFolder} Doc",
            ProjectOutputLayer.Web => $"{hubFolder} Web",
            ProjectOutputLayer.Mobile => $"{hubFolder} Mobile",
            _ => hubFolder
        };

        if (!hubBasedName.Equals(GetLayerFolderName(applicationName, layer), StringComparison.OrdinalIgnoreCase))
            candidates.Add(Path.Combine(outputRoot, hubBasedName));

        return candidates
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(Directory.Exists);
    }

    private static bool HasSiblingLayerFolders(
        string parent,
        string hubFolder,
        string appWeb,
        string appMobile,
        string appDoc) =>
        Directory.Exists(Path.Combine(parent, $"{hubFolder} Web")) ||
        Directory.Exists(Path.Combine(parent, $"{hubFolder} Mobile")) ||
        Directory.Exists(Path.Combine(parent, $"{hubFolder} Doc")) ||
        Directory.Exists(Path.Combine(parent, appWeb)) ||
        Directory.Exists(Path.Combine(parent, appMobile)) ||
        Directory.Exists(Path.Combine(parent, appDoc));

    /// <summary>Legacy entry point — resolves the Web layer folder.</summary>
    public static string ResolveOutputDirectory(string outputRootDirectory, string applicationName) =>
        ResolveLayerDirectory(outputRootDirectory, applicationName, ProjectOutputLayer.Web);

    public static string? FindManifestDirectory(
        string outputRootDirectory,
        string applicationName,
        ProjectOutputLayer preferredLayer)
    {
        foreach (var layer in ManifestSearchOrder(preferredLayer))
        {
            var dir = ResolveLayerDirectory(outputRootDirectory, applicationName, layer);
            if (File.Exists(Path.Combine(dir, "appgen.json")))
                return dir;
        }

        return null;
    }

    private static IEnumerable<ProjectOutputLayer> ManifestSearchOrder(ProjectOutputLayer preferred)
    {
        yield return preferred;
        if (preferred != ProjectOutputLayer.Hub)
            yield return ProjectOutputLayer.Hub;
        if (preferred != ProjectOutputLayer.Web)
            yield return ProjectOutputLayer.Web;
        if (preferred != ProjectOutputLayer.Documentation)
            yield return ProjectOutputLayer.Documentation;
        if (preferred != ProjectOutputLayer.Mobile)
            yield return ProjectOutputLayer.Mobile;
    }

    public static bool OutputDirectoryExists(string outputDirectory) =>
        Directory.Exists(outputDirectory) &&
        Directory.EnumerateFileSystemEntries(outputDirectory).Any();

    public static void DeleteOutputDirectory(string outputDirectory)
    {
        if (!Directory.Exists(outputDirectory))
            return;

        try
        {
            Directory.Delete(outputDirectory, recursive: true);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException(
                $"Could not delete output directory (files may be in use — close Visual Studio or stop running apps): {outputDirectory}",
                ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException(
                $"Could not delete output directory (access denied): {outputDirectory}",
                ex);
        }
    }
}
