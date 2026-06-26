namespace AppGen.Core.Capabilities;

public static class MobileCapabilityPlatforms
{
    public static readonly MobileRunPlatform[] DisplayOrder =
    [
        MobileRunPlatform.Android,
        MobileRunPlatform.Chrome,
        MobileRunPlatform.Mac,
        MobileRunPlatform.IPhone
    ];

    public static string GetDisplayName(MobileRunPlatform platform) => platform switch
    {
        MobileRunPlatform.Android => "Android",
        MobileRunPlatform.Chrome => "Chrome",
        MobileRunPlatform.Mac => "Mac",
        MobileRunPlatform.IPhone => "iPhone",
        _ => platform.ToString()
    };

    public static bool IsSupported(MobileCapabilityDefinition cap, MobileRunPlatform platform)
    {
        if (!cap.IsImplemented)
        {
            return false;
        }

        return platform switch
        {
            MobileRunPlatform.Android => true,
            MobileRunPlatform.Chrome => !cap.RequiresNativePlatform,
            MobileRunPlatform.Mac => !cap.RequiresNativePlatform,
            MobileRunPlatform.IPhone => SupportsIos(cap),
            _ => false
        };
    }

    private static bool SupportsIos(MobileCapabilityDefinition cap)
    {
        if (!cap.RequiresNativePlatform)
        {
            return true;
        }

        if (cap.IosPlistKeys.Count > 0)
        {
            return true;
        }

        return cap.Id is not MobileCapabilityId.Nfc;
    }
}
