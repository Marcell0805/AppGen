using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class FlutterGeneratedOutputPrunerTests
{
    [Fact]
    public async Task Prune_removes_stale_entity_feature_folder()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var flutterRoot = Path.Combine(tempRoot, "PruneApp Mobile");

        try
        {
            var staleDir = Path.Combine(flutterRoot, "lib", "features", "old_entity");
            Directory.CreateDirectory(staleDir);
            await File.WriteAllTextAsync(Path.Combine(staleDir, "stale.dart"), "// stale");

            var keepDir = Path.Combine(flutterRoot, "lib", "features", "widget");
            Directory.CreateDirectory(keepDir);
            await File.WriteAllTextAsync(Path.Combine(keepDir, "keep.dart"), "// keep");

            FlutterGeneratedOutputPruner.Prune(
                flutterRoot,
                ["widget"],
                authEnabled: false,
                offlineEnabled: false);

            Assert.False(Directory.Exists(staleDir));
            Assert.True(Directory.Exists(keepDir));
            Assert.False(Directory.Exists(Path.Combine(flutterRoot, "lib", "features", "auth")));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }
}
