using AppGen.Core.Branding;
using AppGen.Core.Models;
using AppGen.Engine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace AppGen.Tests;

public class IconRasterizerTests
{
    [Fact]
    public async Task SaveResizedAsync_writes_expected_dimensions()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            var source = Path.Combine(tempRoot, "source.png");
            Directory.CreateDirectory(tempRoot);
            await CreateSquarePngAsync(source, 1024);

            var output = Path.Combine(tempRoot, "out.png");
            await IconRasterizer.SaveResizedAsync(source, output, 128);

            var (width, height) = await IconRasterizer.GetDimensionsAsync(await File.ReadAllBytesAsync(output));
            Assert.Equal(128, width);
            Assert.Equal(128, height);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ValidatePngAsync_rejects_non_square()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(tempRoot);
            var path = Path.Combine(tempRoot, "wide.png");
            using (var image = new Image<Rgba32>(600, 500))
                await image.SaveAsPngAsync(path);

            var error = await IconValidation.ValidatePngAsync(await File.ReadAllBytesAsync(path));
            Assert.NotNull(error);
            Assert.Contains("square", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static async Task CreateSquarePngAsync(string path, int size)
    {
        using var image = new Image<Rgba32>(size, size);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new Rgba32(37, 99, 235, 255);
            }
        });
        await image.SaveAsPngAsync(path);
    }
}
