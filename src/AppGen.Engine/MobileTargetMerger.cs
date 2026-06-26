using AppGen.Core.Models;

namespace AppGen.Engine;

public static class MobileTargetMerger
{
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
            Capabilities = mobileSettings.Capabilities ?? existing.Capabilities
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
