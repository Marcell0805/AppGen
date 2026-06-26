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
}
