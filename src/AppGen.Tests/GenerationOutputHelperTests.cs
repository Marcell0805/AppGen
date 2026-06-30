using AppGen.Engine;

namespace AppGen.Tests;

public class GenerationOutputHelperTests
{
    [Theory]
    [InlineData("Tested", ProjectOutputLayer.Hub, "Tested")]
    [InlineData("Tested", ProjectOutputLayer.Documentation, "Tested Doc")]
    [InlineData("Tested", ProjectOutputLayer.Web, "Tested Web")]
    [InlineData("Tested", ProjectOutputLayer.Mobile, "Tested Mobile")]
    public void GetLayerFolderName_uses_layer_suffix(string appName, ProjectOutputLayer layer, string expectedFolder)
    {
        Assert.Equal(expectedFolder, GenerationOutputHelper.GetLayerFolderName(appName, layer));
    }

    [Fact]
    public void ResolveOutputDirectory_uses_web_layer()
    {
        var path = GenerationOutputHelper.ResolveOutputDirectory(@"C:\output", "MyApp");
        Assert.EndsWith(Path.Combine("output", "MyApp Web"), path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindManifestDirectory_prefers_requested_layer()
    {
        var root = Path.Combine(Path.GetTempPath(), "appgen-layer-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var webDir = GenerationOutputHelper.ResolveLayerDirectory(root, "Demo", ProjectOutputLayer.Web);
            Directory.CreateDirectory(webDir);
            File.WriteAllText(Path.Combine(webDir, "appgen.json"), "{}");

            var found = GenerationOutputHelper.FindManifestDirectory(root, "Demo", ProjectOutputLayer.Web);
            Assert.Equal(webDir, found);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveOutputRootFromManifestDirectory_returns_parent_for_hub_or_layer()
    {
        var root = @"C:\output";
        Assert.Equal(root, GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(
            Path.Combine(root, "Demo"), "Demo"));
        Assert.Equal(root, GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(
            Path.Combine(root, "Demo Mobile"), "Demo"));
        Assert.Equal(root, GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(
            Path.Combine(root, "Demo Web"), "Demo"));
    }

    [Fact]
    public void ResolveOutputRootFromManifestDirectory_returns_parent_when_hub_name_differs_from_app_name()
    {
        var root = Path.Combine(Path.GetTempPath(), "appgen-root-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var hubDir = Path.Combine(root, "TestingFunc");
            var webDir = Path.Combine(root, "TestingFunc Web");
            Directory.CreateDirectory(hubDir);
            Directory.CreateDirectory(webDir);
            File.WriteAllText(Path.Combine(hubDir, "appgen.json"), "{}");

            Assert.Equal(root, GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(
                hubDir, "Testing_App_Gen"));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveExistingLayerDirectories_includes_hub_folder_named_web_layer()
    {
        var root = Path.Combine(Path.GetTempPath(), "appgen-layer-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var hubDir = Path.Combine(root, "TestingFunc");
            var webDir = Path.Combine(root, "TestingFunc Web");
            Directory.CreateDirectory(hubDir);
            Directory.CreateDirectory(webDir);

            var found = GenerationOutputHelper.ResolveExistingLayerDirectories(
                hubDir, "Testing_App_Gen", ProjectOutputLayer.Web).ToList();

            Assert.Contains(webDir, found, StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ResolveOutputRootFromManifestDirectory_returns_same_when_already_output_root()
    {
        var root = @"C:\output";
        Assert.Equal(root, GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(root, "Demo"));
    }
}
