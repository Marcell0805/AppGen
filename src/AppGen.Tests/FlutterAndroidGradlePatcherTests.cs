using AppGen.Core;
using AppGen.Engine;

namespace AppGen.Tests;

public class FlutterAndroidGradlePatcherTests
{
    [Fact]
    public void PatchCompileSdkLine_replaces_flutter_default()
    {
        const string input = """
            android {
                compileSdk = flutter.compileSdkVersion
            }
            """;

        var output = FlutterAndroidGradlePatcher.PatchCompileSdkLine(input);

        Assert.Contains($"maxOf(flutter.compileSdkVersion, {AppGenConstants.MinAndroidCompileSdk})", output);
        Assert.DoesNotContain("compileSdk = flutter.compileSdkVersion", output);
    }

    [Fact]
    public void PatchCompileSdkLine_is_idempotent_when_maxOf_present()
    {
        const string input = "compileSdk = maxOf(flutter.compileSdkVersion, 36)";

        var output = FlutterAndroidGradlePatcher.PatchCompileSdkLine(input);

        Assert.Equal(input, output);
    }

    [Fact]
    public void PatchRootGradleForPlugins_adds_library_compileSdk_block()
    {
        const string input = """
            allprojects {
                repositories {
                    google()
                }
            }
            """;

        var output = FlutterAndroidGradlePatcher.PatchRootGradleForPlugins(input);

        Assert.Contains("LibraryExtension", output);
        Assert.Contains("compileSdk = 36", output);
    }
}
