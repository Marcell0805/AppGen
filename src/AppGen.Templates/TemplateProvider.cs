using System.Reflection;

namespace AppGen.Templates;

public static class TemplateProvider
{
    private static readonly Assembly Assembly = typeof(TemplateProvider).Assembly;

    public static string Load(string relativePath)
    {
        var suffix = NormalizeResourceSuffix(relativePath);
        var resourceName = Assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? "AppGen.Templates.Templates." + suffix;
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException(
                $"Template not found: {relativePath}. Available: {string.Join(", ", Assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static IEnumerable<string> ListSolutionTemplates() =>
        ListResources("Solution.");

    public static IEnumerable<string> ListEntityTemplates() =>
        ListResources("Entity.");

    public static IEnumerable<string> ListPortalAssets(string preset) =>
        Assembly.GetManifestResourceNames()
            .Where(n => n.Contains($"Portal.{preset}.", StringComparison.Ordinal))
            .Where(n => !n.EndsWith(".scriban", StringComparison.OrdinalIgnoreCase))
            .Select(n => n.Replace("AppGen.Templates.Templates.", "", StringComparison.Ordinal));

    public static byte[] LoadBytes(string relativePath)
    {
        var suffix = NormalizeResourceSuffix(relativePath);
        var resourceName = Assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException(
                $"Asset not found: {relativePath}. Available: {string.Join(", ", Assembly.GetManifestResourceNames())}");
        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Asset not found: {relativePath}");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static IEnumerable<string> ListResources(string prefix)
    {
        return Assembly.GetManifestResourceNames()
            .Where(n => n.Contains(prefix, StringComparison.Ordinal))
            .Select(n => n.Replace("AppGen.Templates.", "", StringComparison.Ordinal)
                .Replace('.', Path.DirectorySeparatorChar));
    }

    private static string NormalizeResourceSuffix(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/');
        if (normalized.StartsWith("Portal/", StringComparison.OrdinalIgnoreCase))
        {
            var parts = normalized.Split('/');
            if (parts.Length > 1)
                parts[1] = parts[1].Replace('-', '_');
            normalized = string.Join('/', parts);
        }

        return normalized.Replace('/', '.');
    }
}
