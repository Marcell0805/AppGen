using AppGen.Engine;

namespace AppGen.Tests;

public class FlutterPlatformScaffolderTests
{
    [Theory]
    [InlineData("com.myproduct.app", "com.myproduct")]
    [InlineData("io.example.demo.app", "io.example.demo")]
    [InlineData("invalid", null)]
    [InlineData(null, null)]
    public void ResolveAndroidOrg_parses_package_name(string? packageName, string? expected)
    {
        Assert.Equal(expected, FlutterPlatformScaffolder.ResolveAndroidOrg(packageName));
    }

    [Fact]
    public void WriteVsCodeConfig_writes_launch_and_extension_recommendations()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var flutterRoot = tempRoot;

        try
        {
            Directory.CreateDirectory(flutterRoot);
            FlutterPlatformScaffolder.WriteVsCodeConfig(flutterRoot, "MyProduct", tempRoot);

            var launchPath = Path.Combine(flutterRoot, ".vscode", "launch.json");
            var extensionsPath = Path.Combine(flutterRoot, ".vscode", "extensions.json");
            var workspaceLaunchPath = Path.Combine(tempRoot, ".vscode", "launch.json");
            Assert.True(File.Exists(launchPath));
            Assert.True(File.Exists(extensionsPath));
            Assert.True(File.Exists(workspaceLaunchPath));

            var launch = File.ReadAllText(launchPath);
            Assert.Contains("MyProduct (Chrome)", launch);
            Assert.Contains("\"deviceId\": \"chrome\"", launch);
            Assert.Contains("\"cwd\": \"${workspaceFolder}\"", File.ReadAllText(workspaceLaunchPath));
            Assert.Contains("\"program\": \"lib/main.dart\"", File.ReadAllText(workspaceLaunchPath));
            Assert.DoesNotContain("mobile/flutter", File.ReadAllText(workspaceLaunchPath));
            Assert.Contains("Dart-Code.flutter", File.ReadAllText(extensionsPath));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ScaffoldAsync_returns_skip_message_when_flutter_missing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var originalPath = Environment.GetEnvironmentVariable("PATH");
            Environment.SetEnvironmentVariable("PATH", string.Empty);

            var result = await FlutterPlatformScaffolder.ScaffoldAsync(tempRoot, "myproduct");

            Assert.False(result.Succeeded);
            Assert.Contains("Flutter SDK not found", result.Message);
            Environment.SetEnvironmentVariable("PATH", originalPath);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
