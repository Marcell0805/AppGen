using AppGen.Core.Models;
using AppGen.Core.Themes;

namespace AppGen.Engine;

public static class MobileTargetMerger
{
    /// <summary>
    /// Applies the shared app theme preset to mobile targets and portal settings.
    /// </summary>
    public static SolutionSpec ApplyAppThemePreset(SolutionSpec spec, string? presetId)
    {
        var preset = MobileThemeCatalog.NormalizePreset(presetId);
        var withMobile = ApplyWizardMobileSettings(spec, new MobileTargetSpec
        {
            Theme = new MobileThemeSpec { Preset = preset }
        });

        if (withMobile.Portal is null)
            return withMobile;

        return new SolutionSpec
        {
            SchemaVersion = withMobile.SchemaVersion,
            ApplicationName = withMobile.ApplicationName,
            RootNamespace = withMobile.RootNamespace,
            Project = withMobile.Project,
            Phase = withMobile.Phase,
            Portal = new PortalSpec
            {
                Preset = withMobile.Portal.Preset,
                Settings = MobileThemeCatalog.WithAppTheme(withMobile.Portal.Settings, preset),
                Sections = withMobile.Portal.Sections,
                Nav = withMobile.Portal.Nav,
                Features = withMobile.Portal.Features
            },
            EntitySketches = withMobile.EntitySketches,
            Targets = withMobile.Targets,
            Generation = withMobile.Generation,
            Database = withMobile.Database,
            UiTargets = withMobile.UiTargets,
            Setup = withMobile.Setup,
            Entities = withMobile.Entities
        };
    }

    /// <summary>
    /// Applies Project-tab mobile settings (theme, capabilities, offline) onto a loaded manifest spec.
    /// </summary>
    public static SolutionSpec ApplyWizardMobileSettings(SolutionSpec spec, MobileTargetSpec mobileSettings)
    {
        var existing = spec.Targets?.Mobile ?? new MobileTargetSpec();
        var merged = new MobileTargetSpec
        {
            Enabled = true,
            Framework = existing.Framework,
            PackageName = string.IsNullOrWhiteSpace(mobileSettings.PackageName)
                ? existing.PackageName
                : mobileSettings.PackageName,
            ApiBaseUrl = string.IsNullOrWhiteSpace(mobileSettings.ApiBaseUrl)
                ? existing.ApiBaseUrl
                : mobileSettings.ApiBaseUrl,
            StateManagement = existing.StateManagement,
            Theme = mobileSettings.Theme ?? existing.Theme,
            Offline = mobileSettings.Offline ?? existing.Offline,
            Capabilities = mobileSettings.Capabilities ?? existing.Capabilities,
            Publish = existing.Publish
        };

        var targets = spec.Targets ?? new ApplicationTargets();
        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = spec.Project,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = new ApplicationTargets
            {
                Documentation = targets.Documentation,
                Web = targets.Web,
                Mobile = merged
            },
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }
}
