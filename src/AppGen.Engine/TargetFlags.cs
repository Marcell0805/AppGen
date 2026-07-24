using AppGen.Core.Capabilities;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class TargetFlags
{
    public static bool AuthEnabled(SolutionSpec spec) =>
        spec.Targets?.Web.Auth.Enabled == true;

    public static string MobileOfflineMode(SolutionSpec spec) =>
        MobileOfflineTargetNormalizer.ResolveMode(spec.Targets?.Mobile.Offline);

    public static bool ApiOfflineCacheEnabled(SolutionSpec spec) =>
        MobileOfflineMode(spec) == MobileOfflineModes.ApiCache;

    public static bool StandaloneLocalEnabled(SolutionSpec spec) =>
        MobileOfflineMode(spec) == MobileOfflineModes.StandaloneLocal;

    public static bool UsesMobileApiClient(SolutionSpec spec) =>
        !StandaloneLocalEnabled(spec);

    /// <summary>API read-through cache (legacy name).</summary>
    public static bool OfflineEnabled(SolutionSpec spec) =>
        ApiOfflineCacheEnabled(spec);

    public static bool MobileEnabled(SolutionSpec spec) =>
        spec.Targets?.Mobile.Enabled == true;

    public static bool HasCapability(SolutionSpec spec, string capabilityId) =>
        MobileCapabilityResolver.Has(spec, capabilityId);
}
