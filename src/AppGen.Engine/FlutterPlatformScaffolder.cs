using System.Diagnostics;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed record FlutterScaffoldResult(bool Succeeded, string Message)
{
    public static FlutterScaffoldResult Skipped(string message) => new(false, message);

    public static FlutterScaffoldResult Ok(string message) => new(true, message);
}

public static class FlutterPlatformScaffolder
{
    private static readonly string[] DefaultPlatforms = ["web", "android", "windows"];

    public static async Task<FlutterScaffoldResult> ScaffoldAsync(
        string flutterProjectDirectory,
        string projectName,
        string? androidOrg = null,
        string? appDisplayName = null,
        string? projectRootDirectory = null,
        bool preferAndroidLaunch = false,
        CancellationToken ct = default)
    {
        if (!Directory.Exists(flutterProjectDirectory))
            return FlutterScaffoldResult.Skipped("Flutter project folder was not created.");

        WriteVsCodeConfig(flutterProjectDirectory, appDisplayName ?? projectName, projectRootDirectory, preferAndroidLaunch);

        var flutter = ResolveFlutterExecutable();
        if (flutter is null)
        {
            return FlutterScaffoldResult.Skipped(
                "Flutter SDK not found on PATH. Install Flutter, then run: " +
                $"flutter create . --project-name {projectName} --platforms={string.Join(",", DefaultPlatforms)}");
        }

        var normalizedProjectName = NormalizeProjectName(projectName);
        var missingPlatforms = DefaultPlatforms
            .Where(platform => !Directory.Exists(Path.Combine(flutterProjectDirectory, platform)))
            .ToList();

        if (missingPlatforms.Count > 0)
        {
            var createArgs = new List<string>
            {
                "create",
                ".",
                "--project-name",
                normalizedProjectName,
                "--platforms",
                string.Join(",", missingPlatforms)
            };

            if (!string.IsNullOrWhiteSpace(androidOrg))
            {
                createArgs.Add("--org");
                createArgs.Add(androidOrg);
            }

            var createResult = await RunFlutterAsync(flutter, flutterProjectDirectory, createArgs, ct);
            if (!createResult.Succeeded)
                return createResult;
        }

        var pubGetResult = await RunFlutterAsync(flutter, flutterProjectDirectory, ["pub", "get"], ct);
        if (!pubGetResult.Succeeded)
            return pubGetResult;

        if (missingPlatforms.Count == 0)
            return FlutterScaffoldResult.Ok("Flutter dependencies restored (pub get).");

        return FlutterScaffoldResult.Ok(
            $"Flutter platforms scaffolded ({string.Join(", ", missingPlatforms)}) and dependencies restored.");
    }

    internal static void WriteVsCodeConfig(
        string flutterProjectDirectory,
        string appDisplayName,
        string? projectRootDirectory = null,
        bool preferAndroidLaunch = false)
    {
        var model = new { app_name = appDisplayName, prefer_android_launch = preferAndroidLaunch };
        var renderer = new TemplateRenderer();
        var extensions = renderer.Render(TemplateProvider.Load("Mobile/flutter/vscode-extensions.json.scriban"), model);

        var flutterVsCodeDir = Path.Combine(flutterProjectDirectory, ".vscode");
        Directory.CreateDirectory(flutterVsCodeDir);
        var flutterLaunch = renderer.Render(TemplateProvider.Load("Mobile/flutter/vscode-launch.json.scriban"), model);
        File.WriteAllText(Path.Combine(flutterVsCodeDir, "launch.json"), flutterLaunch);
        File.WriteAllText(Path.Combine(flutterVsCodeDir, "extensions.json"), extensions);

        if (string.IsNullOrWhiteSpace(projectRootDirectory))
            return;

        var projectVsCodeDir = Path.Combine(projectRootDirectory, ".vscode");
        Directory.CreateDirectory(projectVsCodeDir);
        var workspaceLaunch = renderer.Render(TemplateProvider.Load("Mobile/flutter/vscode-launch-workspace.json.scriban"), model);
        File.WriteAllText(Path.Combine(projectVsCodeDir, "launch.json"), workspaceLaunch);
        File.WriteAllText(Path.Combine(projectVsCodeDir, "extensions.json"), extensions);
    }

    internal static string? ResolveFlutterExecutable()
    {
        var candidates = OperatingSystem.IsWindows()
            ? new[] { "flutter.bat", "flutter.cmd", "flutter" }
            : new[] { "flutter" };

        foreach (var candidate in candidates)
        {
            var path = FindOnPath(candidate);
            if (path is not null)
                return NormalizeFlutterExecutable(path);
        }

        return null;
    }

    private static string NormalizeFlutterExecutable(string path)
    {
        if (!OperatingSystem.IsWindows())
            return path;

        if (path.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase))
            return path;

        var bat = path + ".bat";
        if (File.Exists(bat))
            return bat;

        var cmd = path + ".cmd";
        if (File.Exists(cmd))
            return cmd;

        return path;
    }

    private static string? FindOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
            return null;

        foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var fullPath = Path.Combine(directory.Trim(), fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }

        return null;
    }

    private static string NormalizeProjectName(string projectName)
    {
        var normalized = projectName.ToLowerInvariant().Replace("_", "", StringComparison.Ordinal);
        if (string.IsNullOrWhiteSpace(normalized))
            return "appgen_mobile";

        if (!char.IsLetter(normalized[0]))
            normalized = "app" + normalized;

        return normalized;
    }

    internal static string? ResolveAndroidOrg(string? packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
            return null;

        var trimmed = packageName.Trim();
        var lastDot = trimmed.LastIndexOf('.');
        return lastDot > 0 ? trimmed[..lastDot] : null;
    }

    private static async Task<FlutterScaffoldResult> RunFlutterAsync(
        string flutterExecutable,
        string workingDirectory,
        IReadOnlyList<string> args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = flutterExecutable,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi);
        if (process is null)
            return FlutterScaffoldResult.Skipped("Failed to start Flutter CLI.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var command = "flutter " + string.Join(' ', args);

        if (process.ExitCode != 0)
        {
            var details = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            return FlutterScaffoldResult.Skipped(
                $"{command} failed: {details.Trim()}");
        }

        return FlutterScaffoldResult.Ok($"{command} completed.");
    }
}
