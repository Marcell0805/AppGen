namespace AppGen.Engine;

public static class GenerationOutputHelper
{
    public static string ResolveOutputDirectory(string outputRootDirectory, string applicationName) =>
        Path.GetFullPath(Path.Combine(outputRootDirectory, applicationName));

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
