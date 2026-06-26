using System.Text.Json;
using System.Text.Json.Nodes;
using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

public static class AppSettingsGenerator
{
    public static async Task WriteAsync(SolutionSpec spec, string outputDirectory, CancellationToken ct = default)
    {
        var apiDir = Path.Combine(outputDirectory, $"src/{spec.ApiProject}");
        Directory.CreateDirectory(apiDir);

        var baseJson = BuildBaseAppSettings(spec);
        await File.WriteAllTextAsync(
            Path.Combine(apiDir, "appsettings.json"),
            baseJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            ct);

        var devJson = BuildDevelopmentAppSettings(spec);
        await File.WriteAllTextAsync(
            Path.Combine(apiDir, "appsettings.Development.json"),
            devJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
            ct);

        if (spec.UiTargets.HasFlag(UiTarget.MvcWeb))
        {
            var mvcDir = Path.Combine(outputDirectory, $"src/{spec.MvcProject}");
            Directory.CreateDirectory(mvcDir);
            var mvcJson = new JsonObject
            {
                ["Api"] = new JsonObject { ["BaseUrl"] = "https://localhost:5001" },
                ["Logging"] = new JsonObject
                {
                    ["LogLevel"] = new JsonObject
                    {
                        ["Default"] = "Information",
                        ["Microsoft.AspNetCore"] = "Warning"
                    }
                },
                ["AllowedHosts"] = "*"
            };
            await File.WriteAllTextAsync(
                Path.Combine(mvcDir, "appsettings.json"),
                mvcJson.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                ct);
        }
    }

    private static JsonObject BuildBaseAppSettings(SolutionSpec spec)
    {
        var root = new JsonObject
        {
            ["ConnectionStrings"] = new JsonObject(),
            ["Database"] = new JsonObject
            {
                ["ActiveConnection"] = spec.Setup.ActiveConnectionName
            },
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject
                {
                    ["Default"] = "Information",
                    ["Microsoft.AspNetCore"] = "Warning"
                }
            },
            ["AllowedHosts"] = "*"
        };

        var connStrings = (JsonObject)root["ConnectionStrings"]!;
        foreach (var entry in spec.Setup.ConfigEntries.Where(e => e.Kind == ConfigEntryKind.ConnectionString))
        {
            connStrings[entry.Name] = entry.Value ?? string.Empty;
        }

        if (connStrings.Count == 0)
            connStrings["Dev"] = NamingHelper.DefaultDevConnection(spec.Database).Value ?? string.Empty;

        ApplyAppSettings(root, spec);
        return root;
    }

    private static JsonObject BuildDevelopmentAppSettings(SolutionSpec spec)
    {
        var root = new JsonObject
        {
            ["Database"] = new JsonObject
            {
                ["ActiveConnection"] = spec.Setup.ActiveConnectionName,
                ["EnsureCreated"] = spec.Setup.EnsureCreatedInDevelopment
            },
            ["Logging"] = new JsonObject
            {
                ["LogLevel"] = new JsonObject
                {
                    ["Default"] = "Debug",
                    ["Microsoft.AspNetCore"] = "Information"
                }
            }
        };

        ApplyAppSettings(root, spec);
        return root;
    }

    private static void ApplyAppSettings(JsonObject root, SolutionSpec spec)
    {
        foreach (var entry in spec.Setup.ConfigEntries.Where(e => e.Kind == ConfigEntryKind.AppSetting))
        {
            var key = string.IsNullOrWhiteSpace(entry.Key) ? entry.Name : entry.Key;
            SetNestedValue(root, key, entry.Value ?? string.Empty);
        }
    }

    private static void SetNestedValue(JsonObject root, string key, string value)
    {
        var parts = key.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return;

        JsonObject current = root;
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (current[parts[i]] is not JsonObject child)
            {
                child = new JsonObject();
                current[parts[i]] = child;
            }
            current = child;
        }
        current[parts[^1]] = value;
    }
}
