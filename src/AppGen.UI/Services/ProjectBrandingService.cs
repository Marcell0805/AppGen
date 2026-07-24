using AppGen.Core.Branding;
using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.UI.Models;

namespace AppGen.UI.Services;

public static class ProjectBrandingService
{
    public static async Task<(bool Success, string? Error)> ValidateAndPrepareUploadAsync(
        byte[] bytes,
        CancellationToken ct = default)
    {
        var error = await IconValidation.ValidatePngAsync(bytes, ct);
        return error is null ? (true, null) : (false, error);
    }

    public static string CreatePreviewDataUrl(byte[] bytes) =>
        $"data:image/png;base64,{Convert.ToBase64String(bytes)}";

    public static string EncodeForDraft(byte[] bytes) =>
        Convert.ToBase64String(bytes);

    public static byte[]? DecodeFromDraft(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(base64);
            return bytes.Length <= ProjectBrandingConstants.MaxDraftIconBase64Bytes ? bytes : null;
        }
        catch
        {
            return null;
        }
    }

    public static async Task SaveToHubAsync(byte[] bytes, string hubDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(hubDirectory);
        await ProjectBrandingEmitter.SaveHubIconAsync(bytes, hubDirectory, ct);
    }

    public static async Task FlushDraftIconToHubAsync(WizardDraft draft, string hubDirectory, CancellationToken ct = default)
    {
        var bytes = DecodeFromDraft(draft.IconBase64);
        if (bytes is null)
            return;

        var (success, error) = await ValidateAndPrepareUploadAsync(bytes, ct);
        if (!success)
            return;

        await SaveToHubAsync(bytes, hubDirectory, ct);
    }

    public static async Task<(string IconPath, string IconOriginalFileName, string IconBase64)> ApplyUploadAsync(
        byte[] pngBytes,
        string originalFileName,
        string? hubDirectory,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(hubDirectory))
            await SaveToHubAsync(pngBytes, hubDirectory, ct);

        return (
            ProjectBrandingConstants.DefaultIconRelativePath,
            originalFileName,
            EncodeForDraft(pngBytes));
    }

    public static string? TryGetPreviewFromHub(string? hubDirectory, SolutionSpec spec)
    {
        if (string.IsNullOrWhiteSpace(hubDirectory))
            return null;

        var path = ProjectBrandingPaths.TryResolveHubIconPath(hubDirectory, spec);
        if (path is null)
            return null;

        var bytes = File.ReadAllBytes(path);
        return CreatePreviewDataUrl(bytes);
    }

    public static void ClearIconState(
        string? hubDirectory,
        SolutionSpec? spec)
    {
        if (hubDirectory is null || spec?.Project?.Branding?.IconPath is null)
            return;

        var fullPath = ProjectBrandingPaths.GetHubIconFullPath(hubDirectory, spec);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }
}
