using AppGen.Core.Models;

namespace AppGen.Engine;

public interface IApplicationGenerator
{
    string TargetId { get; }

    Task<GeneratorTargetResult> GenerateAsync(
        SolutionSpec spec,
        string projectDirectory,
        GeneratorOptions options,
        CancellationToken ct = default);
}

public sealed class GeneratorOptions
{
    public bool Force { get; init; }
    public string? EntityName { get; init; }
    public IReadOnlyList<string>? EntityNames { get; init; }
}

public sealed record GeneratorTargetResult(bool Success, string Message, string? OutputPath)
{
    public static GeneratorTargetResult Ok(string outputPath, string? message = null) =>
        new(true, message ?? "Generated successfully.", outputPath);

    public static GeneratorTargetResult Fail(string message) =>
        new(false, message, null);
}
