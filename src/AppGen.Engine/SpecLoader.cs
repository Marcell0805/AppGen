using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class SpecLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<SolutionSpec> LoadAsync(string projectDirectory, CancellationToken ct = default)
    {
        var path = Path.Combine(projectDirectory, "appgen.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("appgen.json not found. Run 'appgen create' first.", path);

        await using var stream = File.OpenRead(path);
        var spec = await JsonSerializer.DeserializeAsync<SolutionSpec>(stream, Options, ct);
        return spec ?? throw new InvalidOperationException("Failed to deserialize appgen.json.");
    }

    public static SolutionSpec CreateDefault(string applicationName, string? rootNamespace, DatabaseProvider database)
    {
        var normalizedName = NamingHelper.NormalizeAppName(applicationName);
        var normalizedNamespace = NamingHelper.NormalizeAppName(rootNamespace ?? normalizedName);
        return new()
        {
            ApplicationName = normalizedName,
            RootNamespace = normalizedNamespace,
            Database = database,
            Entities = []
        };
    }
}
