using AppGen.Core.Branding;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace AppGen.Engine;

public static class IconRasterizer
{
    public static async Task SaveResizedAsync(
        string sourcePath,
        string destinationPath,
        int sizePx,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        await using var input = File.OpenRead(sourcePath);
        using var image = await Image.LoadAsync(input, ct);
        using var output = CropSquare(image);
        output.Mutate(ctx => ctx.Resize(sizePx, sizePx));
        await output.SaveAsPngAsync(destinationPath, new PngEncoder(), ct);
    }

    public static async Task SaveResizedFromBytesAsync(
        byte[] pngBytes,
        string destinationPath,
        int sizePx,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        using var image = Image.Load(pngBytes);
        using var output = CropSquare(image);
        output.Mutate(ctx => ctx.Resize(sizePx, sizePx));
        await output.SaveAsPngAsync(destinationPath, new PngEncoder(), ct);
    }

    public static async Task<(int Width, int Height)> GetDimensionsAsync(byte[] pngBytes, CancellationToken ct = default)
    {
        var info = await Image.IdentifyAsync(new MemoryStream(pngBytes), ct);
        return (info.Width, info.Height);
    }

    internal static Image CropSquare(Image source)
    {
        var side = Math.Min(source.Width, source.Height);
        var x = (source.Width - side) / 2;
        var y = (source.Height - side) / 2;
        var clone = source.Clone(ctx => ctx.Crop(new Rectangle(x, y, side, side)));
        return clone;
    }
}

public static class IconValidation
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsPngSignature(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= PngSignature.Length && bytes[..PngSignature.Length].SequenceEqual(PngSignature);

    public static async Task<string?> ValidatePngAsync(byte[] bytes, CancellationToken ct = default)
    {
        if (bytes.Length == 0)
            return "Icon file is empty.";

        if (bytes.Length > ProjectBrandingConstants.MaxIconBytes)
            return $"Icon must be {ProjectBrandingConstants.MaxIconBytes / (1024 * 1024)} MB or smaller.";

        if (!IsPngSignature(bytes))
            return "Icon must be a PNG file.";

        var (width, height) = await IconRasterizer.GetDimensionsAsync(bytes, ct);
        if (width != height)
            return "Icon must be square (equal width and height).";

        if (width < ProjectBrandingConstants.MinIconSizePx)
            return $"Icon must be at least {ProjectBrandingConstants.MinIconSizePx}×{ProjectBrandingConstants.MinIconSizePx} pixels.";

        return null;
    }
}
