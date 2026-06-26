namespace AppGen.UI.Services;

public sealed class OutputFolderService
{
    /// <summary>
    /// Default folder for generated solutions: {AppGen repo}/output
    /// (ContentRoot is src/AppGen.UI when running the UI).
    /// </summary>
    public string GetDefaultOutputRoot(string contentRootPath)
    {
        var appGenRoot = Path.GetFullPath(Path.Combine(contentRootPath, "..", ".."));
        return Path.Combine(appGenRoot, "output");
    }

    public void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException(path);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }
}
