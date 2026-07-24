using System.Text.Json;
using AppGen.Core.Models.SpecWorkbook;

namespace AppGen.Engine;

public static class WorkbookSeedDataStore
{
    public const string FileName = "workbook-seed-data.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static async Task SaveAsync(
        string hubDirectory,
        IReadOnlyDictionary<string, List<Dictionary<string, string>>> entityData,
        CancellationToken ct = default)
    {
        var path = Path.Combine(hubDirectory, FileName);
        if (entityData.Count == 0 || !entityData.Values.Any(rows => rows.Any(HasMeaningfulValues)))
        {
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        var payload = entityData.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);

        Directory.CreateDirectory(hubDirectory);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(payload, JsonOptions), ct);
    }

    public static async Task<IReadOnlyDictionary<string, List<Dictionary<string, string>>>?> TryLoadAsync(
        string? hubDirectory,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(hubDirectory))
            return null;

        var path = Path.Combine(hubDirectory, FileName);
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<Dictionary<string, string>>>>(stream, JsonOptions, ct);
        return data;
    }

    public static IReadOnlyDictionary<string, List<Dictionary<string, string>>>? TryLoad(string? hubDirectory)
    {
        if (string.IsNullOrWhiteSpace(hubDirectory))
            return null;

        var path = Path.Combine(hubDirectory, FileName);
        if (!File.Exists(path))
            return null;

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, List<Dictionary<string, string>>>>(json, JsonOptions);
    }

    public static string? ResolveHubDirectory(string outputDirectory, string applicationName)
    {
        var dir = Path.GetFullPath(outputDirectory.Trim());
        var folder = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var hubName = GenerationOutputHelper.GetLayerFolderName(applicationName, ProjectOutputLayer.Hub);

        if (folder.Equals(hubName, StringComparison.OrdinalIgnoreCase) &&
            File.Exists(Path.Combine(dir, "appgen.json")))
            return dir;

        var parent = Path.GetDirectoryName(dir);
        if (!string.IsNullOrEmpty(parent))
        {
            var hubDir = Path.Combine(parent, hubName);
            if (File.Exists(Path.Combine(hubDir, "appgen.json")))
                return hubDir;
        }

        return File.Exists(Path.Combine(dir, "appgen.json")) ? dir : null;
    }

    internal static bool HasMeaningfulValues(IReadOnlyDictionary<string, string> row) =>
        row.Values.Any(v => !string.IsNullOrWhiteSpace(v));
}
