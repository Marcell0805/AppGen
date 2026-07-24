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

    /// <summary>
    /// Shows a native Windows folder picker (STA). Returns null if cancelled or unavailable.
    /// </summary>
    public string? PickFolder(string? initialDirectory = null)
    {
        if (!OperatingSystem.IsWindows())
            return null;

        string? selected = null;
        var thread = new Thread(() =>
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select AppGen output folder",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrWhiteSpace(initialDirectory))
            {
                try
                {
                    var full = Path.GetFullPath(initialDirectory.Trim());
                    if (Directory.Exists(full))
                        dialog.SelectedPath = full;
                    else
                    {
                        var parent = Path.GetDirectoryName(full);
                        if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
                            dialog.SelectedPath = parent;
                    }
                }
                catch
                {
                    // Keep dialog default.
                }
            }

            if (dialog.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                selected = dialog.SelectedPath;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        thread.Join();
        return selected;
    }
}
