using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core.Models;

namespace AppGen.UI.Models;

public sealed class WizardDraft
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public DateTime SavedAt { get; init; } = DateTime.UtcNow;
    public string ApplicationName { get; init; } = string.Empty;
    public string? RootNamespace { get; init; }
    public string? Tagline { get; init; }
    public string? Description { get; init; }
    public DatabaseProvider Database { get; init; } = DatabaseProvider.SqlServer;
    public string OutputRoot { get; init; } = string.Empty;
    public bool IncludeMvcWeb { get; init; } = true;
    public bool EnableDocumentation { get; init; }
    public bool EnableWeb { get; init; } = true;
    public bool EnableMobile { get; init; }
    public bool EnableWebAuth { get; init; }
    public bool EnableMobileOffline { get; init; }
    public string MobileThemePreset { get; init; } = "appgen";
    public string MobilePackageName { get; init; } = string.Empty;
    public string MobileApiBaseUrl { get; init; } = "http://localhost:5000";
    public string ActiveConnectionName { get; init; } = "Dev";
    public string? OracleSchemaPrefix { get; init; }
    public bool EnsureCreatedInDevelopment { get; init; } = true;
    public List<ConfigEntryDraft> ConfigEntries { get; init; } = [];
    public List<EntityDraftData> Entities { get; init; } = [];

    public sealed class ConfigEntryDraft
    {
        public string Name { get; init; } = string.Empty;
        public ConfigEntryKind Kind { get; init; } = ConfigEntryKind.ConnectionString;
        public string? Key { get; init; }
        public string? Value { get; init; }
    }

    public sealed class EntityDraftData
    {
        public string Name { get; init; } = string.Empty;
        public bool IncludeInUi { get; init; } = true;
        public bool IncludeAuditColumns { get; init; }
        public List<PropertyDraftData> Properties { get; init; } = [];
    }

    public sealed class PropertyDraftData
    {
        public string Name { get; init; } = string.Empty;
        public string ClrType { get; init; } = "string";
        public bool IsKey { get; init; }
        public bool IsNullable { get; init; }
        public string? ForeignKeyEntity { get; init; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static WizardDraft? TryParse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<WizardDraft>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static WizardDraft FromWizard(
        string applicationName,
        string? rootNamespace,
        DatabaseProvider database,
        string outputRoot,
        bool includeMvcWeb,
        string activeConnectionName,
        string? oracleSchemaPrefix,
        bool ensureCreatedInDevelopment,
        IEnumerable<ConfigEntryRow> configEntries,
        IEnumerable<EntityDraft> entities,
        bool enableDocumentation = false,
        bool enableWeb = true,
        bool enableMobile = false,
        bool enableWebAuth = false,
        bool enableMobileOffline = false,
        string mobileThemePreset = "appgen",
        string? mobilePackageName = null,
        string? mobileApiBaseUrl = null,
        string? tagline = null,
        string? description = null) => new()
    {
        ApplicationName = applicationName,
        RootNamespace = rootNamespace,
        Tagline = tagline,
        Description = description,
        Database = database,
        OutputRoot = outputRoot,
        IncludeMvcWeb = includeMvcWeb,
        EnableDocumentation = enableDocumentation,
        EnableWeb = enableWeb,
        EnableMobile = enableMobile,
        EnableWebAuth = enableWebAuth,
        EnableMobileOffline = enableMobileOffline,
        MobileThemePreset = string.IsNullOrWhiteSpace(mobileThemePreset) ? "appgen" : mobileThemePreset,
        MobilePackageName = mobilePackageName ?? string.Empty,
        MobileApiBaseUrl = string.IsNullOrWhiteSpace(mobileApiBaseUrl) ? "http://localhost:5000" : mobileApiBaseUrl,
        ActiveConnectionName = activeConnectionName,
        OracleSchemaPrefix = oracleSchemaPrefix,
        EnsureCreatedInDevelopment = ensureCreatedInDevelopment,
        ConfigEntries = configEntries
            .Select(c => new ConfigEntryDraft
            {
                Name = c.Name,
                Kind = c.Kind,
                Key = c.Key,
                Value = c.Value
            })
            .ToList(),
        Entities = entities
            .Select(e => new EntityDraftData
            {
                Name = e.Name,
                IncludeInUi = e.IncludeInUi,
                IncludeAuditColumns = e.IncludeAuditColumns,
                Properties = e.Properties
                    .Select(p => new PropertyDraftData
                    {
                        Name = p.Name,
                        ClrType = p.ClrType,
                        IsKey = p.IsKey,
                        IsNullable = p.IsNullable,
                        ForeignKeyEntity = p.ForeignKeyEntity
                    })
                    .ToList()
            })
            .ToList()
    };
}
