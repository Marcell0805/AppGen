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
        Converters = { new JsonStringEnumConverter(), new UiTargetJsonConverter() }
    };

    public static async Task<SolutionSpec> LoadAsync(string projectDirectory, CancellationToken ct = default)
    {
        var path = Path.Combine(projectDirectory, "appgen.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("appgen.json not found. Run 'appgen create' first.", path);

        await using var stream = File.OpenRead(path);
        var spec = await JsonSerializer.DeserializeAsync<SolutionSpec>(stream, Options, ct);
        return SpecNormalizer.Normalize(spec ?? throw new InvalidOperationException("Failed to deserialize appgen.json."));
    }

    public static SolutionSpec CreateDefault(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        UiTarget uiTargets = UiTarget.None,
        ProjectSetupSpec? setup = null)
    {
        var normalizedName = NamingHelper.NormalizeAppName(applicationName);
        var normalizedNamespace = NamingHelper.NormalizeAppName(rootNamespace ?? normalizedName);
        return new()
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = normalizedName,
            RootNamespace = normalizedNamespace,
            Phase = ProjectPhase.Solution,
            Database = database,
            UiTargets = uiTargets,
            Setup = setup ?? NamingHelper.DefaultSetup(database),
            Entities = []
        };
    }

    public static SolutionSpec CreatePortalDefault(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database = DatabaseProvider.PostgreSql,
        UiTarget uiTargets = UiTarget.MvcWeb,
        string preset = "engineering-portal",
        ProjectSetupSpec? setup = null,
        IEnumerable<EntitySketch>? entitySketches = null)
    {
        var normalizedName = NamingHelper.NormalizeAppName(applicationName);
        var normalizedNamespace = NamingHelper.NormalizeAppName(rootNamespace ?? normalizedName);
        var sketches = entitySketches?.ToList() ?? [];
        var portal = PortalDefaults.CreateDefaultPortal(normalizedName, preset);
        var sections = portal.Sections.ToList();
        sections.Add(PortalDefaults.BuildEntitiesSection(normalizedName, sketches));
        portal = new PortalSpec
        {
            Preset = portal.Preset,
            Settings = portal.Settings,
            Sections = sections,
            Nav = PortalDefaults.BuildNav(sections),
            Features = portal.Features
        };

        return new()
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = normalizedName,
            RootNamespace = normalizedNamespace,
            Phase = ProjectPhase.Portal,
            Portal = portal,
            EntitySketches = sketches,
            Database = database,
            UiTargets = uiTargets,
            Setup = setup ?? NamingHelper.DefaultSetup(database),
            Entities = []
        };
    }
}
