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
