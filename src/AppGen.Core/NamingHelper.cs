namespace AppGen.Core;

public static class NamingHelper
{
    public static string NormalizeAppName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

        // Replace whitespace with underscore, then remove invalid identifier characters.
        var name = string.Join("_", rawName.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var filtered = new string(name
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_')
            .ToArray());

        // Identifiers cannot start with a digit; prefix underscore if needed.
        if (filtered.Length > 0 && char.IsDigit(filtered[0]))
            filtered = "_" + filtered;

        // Collapse multiple underscores for readability.
        while (filtered.Contains("__", StringComparison.Ordinal))
            filtered = filtered.Replace("__", "_", StringComparison.Ordinal);

        return filtered;
    }

    public static string ToPlural(string name)
    {
        if (name.EndsWith('y') && name.Length > 1 && !IsVowel(name[^2]))
            return name[..^1] + "ies";
        if (name.EndsWith('s') || name.EndsWith('x') || name.EndsWith('z') ||
            name.EndsWith("ch", StringComparison.Ordinal) || name.EndsWith("sh", StringComparison.Ordinal))
            return name + "es";
        return name + "s";
    }

    public static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];

    private static bool IsVowel(char c) =>
        c is 'a' or 'e' or 'i' or 'o' or 'u';
}
