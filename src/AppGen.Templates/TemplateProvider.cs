using System.Reflection;

namespace AppGen.Templates;

public static class TemplateProvider
{
    private static readonly Assembly Assembly = typeof(TemplateProvider).Assembly;

    public static string Load(string relativePath)
    {
        var suffix = relativePath.Replace('/', '.').Replace('\\', '.');
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

    private static IEnumerable<string> ListResources(string prefix)
    {
        return Assembly.GetManifestResourceNames()
            .Where(n => n.Contains(prefix, StringComparison.Ordinal))
            .Select(n => n.Replace("AppGen.Templates.", "", StringComparison.Ordinal)
                .Replace('.', Path.DirectorySeparatorChar));
    }
}
