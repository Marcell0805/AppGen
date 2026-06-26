namespace AppGen.Engine;

/// <summary>
/// Resolves paths for generated Flutter mobile output.
/// The Mobile layer folder ({AppName} Mobile) is the Flutter project root.
/// </summary>
public static class FlutterProjectPaths
{
    public static string GetFlutterRoot(string mobileLayerDirectory) => mobileLayerDirectory;

    /// <summary>Pre-v9 nested layout: {MobileLayer}/mobile/flutter/</summary>
    public static string GetLegacyFlutterRoot(string mobileLayerDirectory) =>
        Path.Combine(mobileLayerDirectory, "mobile", "flutter");
}
