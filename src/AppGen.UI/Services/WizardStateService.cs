using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.UI.Models;

namespace AppGen.UI.Services;

public sealed class WizardStateService
{
    public event Action? Changed;

    private WizardDraft? _draft;
    private SolutionSpec? _lastManifestSpec;
    private string? _lastHubDirectory;
    private PortalUiDraft? _portalDraft;

    public bool HasData => _draft is not null && _draft.Entities.Count > 0;

    public WizardDraft? Current => _draft;

    public SolutionSpec? LastManifestSpec => _lastManifestSpec;

    public string? LastHubDirectory => _lastHubDirectory;

    public PortalUiDraft? PortalDraft => _portalDraft;

    public IReadOnlyList<string> EntityNames =>
        _draft?.Entities.Select(e => e.Name).ToList() ?? [];

    public void Update(WizardDraft draft)
    {
        _draft = draft;
        Changed?.Invoke();
    }

    public void NotifyManifestLoaded(SolutionSpec spec, string hubDirectory)
    {
        _lastManifestSpec = spec;
        _lastHubDirectory = Path.GetFullPath(hubDirectory);
        if (spec.Portal is not null && spec.Portal.Sections.Count > 0)
        {
            var outputRoot = _draft?.OutputRoot;
            if (string.IsNullOrWhiteSpace(outputRoot))
            {
                var parent = Path.GetDirectoryName(_lastHubDirectory);
                outputRoot = parent is not null ? Path.GetDirectoryName(parent) ?? parent : string.Empty;
            }

            _portalDraft = ProjectManifestMapper.ToPortalUiDraft(spec, outputRoot);
        }

        Changed?.Invoke();
    }

    public void UpdatePortalDraft(PortalUiDraft draft, bool notify = true)
    {
        _portalDraft = draft;
        if (notify)
            Changed?.Invoke();
    }

    public SolutionSpec ToSolutionSpec()
    {
        if (_draft is null)
            throw new InvalidOperationException("No wizard state is available.");

        var allNames = _draft.Entities.Select(e => e.Name).ToList();
        var entities = _draft.Entities
            .Select(e => EntityDraftMapper.ToEntitySpec(e, allNames))
            .ToList();

        var uiTargets = _draft.IncludeMvcWeb ? UiTarget.MvcWeb : UiTarget.None;
        var setup = new ProjectSetupSpec
        {
            ActiveConnectionName = _draft.ActiveConnectionName,
            EnsureCreatedInDevelopment = _draft.EnsureCreatedInDevelopment,
            OracleSchemaPrefix = _draft.OracleSchemaPrefix,
            ConfigEntries = _draft.ConfigEntries
                .Select(c => new ConfigEntrySpec
                {
                    Name = c.Name,
                    Kind = c.Kind,
                    Key = c.Key,
                    Value = c.Value
                })
                .ToList()
        };

        var appName = NamingHelper.NormalizeAppName(_draft.ApplicationName);
        var rootNs = NamingHelper.NormalizeAppName(_draft.RootNamespace ?? _draft.ApplicationName);
        var packageName = string.IsNullOrWhiteSpace(_draft.MobilePackageName)
            ? $"com.{appName.ToLowerInvariant()}.app"
            : _draft.MobilePackageName.Trim();

        var legacy = new SolutionSpec
        {
            ApplicationName = appName,
            RootNamespace = rootNs,
            Phase = ProjectPhase.Solution
        };

        var targets = SpecNormalizer.BuildTargetsFromLegacy(legacy);
        targets = new ApplicationTargets
        {
            Documentation = new DocumentationTargetSpec
            {
                Enabled = _draft.EnableDocumentation,
                Preset = targets.Documentation.Preset
            },
            Web = new WebTargetSpec
            {
                Enabled = _draft.EnableWeb,
                Auth = new WebAuthTargetSpec { Enabled = _draft.EnableWeb && _draft.EnableWebAuth }
            },
            Mobile = new MobileTargetSpec
            {
                Enabled = _draft.EnableMobile,
                Framework = targets.Mobile.Framework,
                PackageName = packageName,
                ApiBaseUrl = _draft.MobileApiBaseUrl,
                StateManagement = targets.Mobile.StateManagement,
                Theme = new MobileThemeSpec { Preset = _draft.MobileThemePreset },
                Offline = new MobileOfflineTargetSpec { Enabled = _draft.EnableMobile && _draft.EnableMobileOffline },
                Capabilities = new MobileCapabilitiesSpec
                {
                    Enabled = _draft.MobileCapabilities.ToList()
                }
            }
        };

        PortalSpec? portal = null;
        var sketches = entities.Select(e => new EntitySketch
        {
            Name = e.Name,
            Status = "draft",
            Description = $"{e.Name} entity"
        }).ToList();

        if (_draft.EnableDocumentation)
        {
            var portalDefault = SpecLoader.CreatePortalDefault(
                appName,
                rootNs,
                _draft.Database,
                uiTargets,
                setup: setup,
                entitySketches: sketches);
            portal = portalDefault.Portal;
        }

        return new SolutionSpec
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = appName,
            RootNamespace = rootNs,
            Project = BuildProjectInfo(),
            Phase = _draft.EnableDocumentation && !_draft.EnableWeb ? ProjectPhase.Portal : ProjectPhase.Solution,
            Portal = portal,
            EntitySketches = sketches,
            Targets = targets,
            Database = _draft.Database,
            UiTargets = uiTargets,
            Setup = setup,
            Entities = entities
        };
    }

    private ProjectInfoSpec? BuildProjectInfo()
    {
        if (_draft is null)
            return null;

        if (string.IsNullOrWhiteSpace(_draft.Tagline) && string.IsNullOrWhiteSpace(_draft.Description))
            return null;

        return new ProjectInfoSpec
        {
            Tagline = string.IsNullOrWhiteSpace(_draft.Tagline) ? null : _draft.Tagline.Trim(),
            Description = string.IsNullOrWhiteSpace(_draft.Description) ? null : _draft.Description.Trim()
        };
    }
}

internal static class EntityDraftMapper
{
    public static EntitySpec ToEntitySpec(WizardDraft.EntityDraftData data, IReadOnlyList<string> allNames)
    {
        var draft = new EntityDraft(data.Name)
        {
            IncludeInUi = data.IncludeInUi,
            IncludeAuditColumns = data.IncludeAuditColumns
        };
        draft.Properties.Clear();
        foreach (var property in data.Properties)
        {
            draft.Properties.Add(new PropertyRow
            {
                Name = property.Name,
                ClrType = property.ClrType,
                IsKey = property.IsKey,
                IsNullable = property.IsNullable,
                ForeignKeyEntity = property.ForeignKeyEntity
            });
        }

        return draft.ToSpec(allNames);
    }
}
