namespace AppGen.Engine;

public static class RegionMergeHelper
{
    public static string BuildRegion(string name, params string[] lines) =>
        $"// <AppGen-{name}>" + Environment.NewLine +
        string.Join(Environment.NewLine, lines) + Environment.NewLine +
        $"// </AppGen-{name}>";

    public static async Task MergeRegionAsync(
        string filePath,
        string regionContent,
        CancellationToken ct = default) =>
        await MergeRegionAsync(filePath, regionContent, null, null, ct);

    public static async Task MergeRegionAsync(
        string filePath,
        string regionContent,
        string? parentStart,
        string? parentEnd,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found for region merge: {filePath}");

        var content = await File.ReadAllTextAsync(filePath, ct);
        var entityStart = regionContent.Split(Environment.NewLine)[0];
        var entityEnd = regionContent.Split(Environment.NewLine)[^1];

        var entityStartIdx = content.IndexOf(entityStart, StringComparison.Ordinal);
        if (entityStartIdx >= 0)
        {
            var entityEndIdx = content.IndexOf(entityEnd, entityStartIdx, StringComparison.Ordinal);
            if (entityEndIdx >= 0)
            {
                content = content[..entityStartIdx] + regionContent + content[(entityEndIdx + entityEnd.Length)..];
                await File.WriteAllTextAsync(filePath, content, ct);
                return;
            }
        }

        if (parentStart is null || parentEnd is null)
            throw new InvalidOperationException($"Cannot merge region into {filePath}");

        var parentStartIdx = content.IndexOf(parentStart, StringComparison.Ordinal);
        var parentEndIdx = content.IndexOf(parentEnd, StringComparison.Ordinal);
        if (parentStartIdx < 0 || parentEndIdx < 0)
            throw new InvalidOperationException($"Parent region not found in {filePath}");

        var insertAt = parentEndIdx;
        content = content.Insert(insertAt, Environment.NewLine + regionContent + Environment.NewLine);
        await File.WriteAllTextAsync(filePath, content, ct);
    }
}
