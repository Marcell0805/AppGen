namespace AppGen.Core.Models;

public static class MobileOfflineTargetNormalizer
{
    public static MobileOfflineTargetSpec Normalize(MobileOfflineTargetSpec offline)
    {
        var mode = ResolveMode(offline);
        return new MobileOfflineTargetSpec
        {
            Mode = mode,
            Provider = string.IsNullOrWhiteSpace(offline.Provider) ? "sqlite" : offline.Provider.Trim(),
            Enabled = mode == MobileOfflineModes.ApiCache
        };
    }

    public static string ResolveMode(MobileOfflineTargetSpec? offline)
    {
        if (offline is null)
            return MobileOfflineModes.None;

        var mode = offline.Mode?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(mode) &&
            !mode.Equals(MobileOfflineModes.None, StringComparison.OrdinalIgnoreCase))
        {
            if (mode.Equals(MobileOfflineModes.ApiCache, StringComparison.OrdinalIgnoreCase))
                return MobileOfflineModes.ApiCache;
            if (mode.Equals(MobileOfflineModes.StandaloneLocal, StringComparison.OrdinalIgnoreCase))
                return MobileOfflineModes.StandaloneLocal;
        }

        return offline.Enabled ? MobileOfflineModes.ApiCache : MobileOfflineModes.None;
    }
}
