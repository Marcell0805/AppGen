using AppGen.Core.Capabilities;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class FlutterCapabilityEmitter(TemplateRenderer renderer)
{
    public async Task<FlutterCapabilityEmitResult> EmitAsync(
        SolutionSpec spec,
        string flutterRoot,
        object templateModel,
        CancellationToken ct = default)
    {
        var resolved = MobileCapabilityResolver.Resolve(spec);
        var packages = MergePackages(resolved);
        var services = resolved
            .Where(c => c.IsImplemented && !string.IsNullOrWhiteSpace(c.ServiceTemplate))
            .ToList();

        var servicesDir = Path.Combine(flutterRoot, "lib", "core", "services");
        Directory.CreateDirectory(servicesDir);

        foreach (var capability in services)
        {
            var templatePath = $"Mobile/flutter/services/{capability.ServiceTemplate}";
            var content = renderer.Render(TemplateProvider.Load(templatePath), templateModel);
            var fullPath = Path.Combine(servicesDir, capability.ServiceFileName!);
            await File.WriteAllTextAsync(fullPath, content, ct);
        }

        if (services.Count > 0)
        {
            var providerModel = BuildProviderModel(services);
            var providerContent = renderer.Render(
                TemplateProvider.Load("Mobile/flutter/capabilities_provider.dart.scriban"),
                providerModel);
            await File.WriteAllTextAsync(
                Path.Combine(servicesDir, "capabilities_provider.dart"),
                providerContent,
                ct);
        }

        return new FlutterCapabilityEmitResult(resolved, packages, services);
    }

    public static IReadOnlyList<MobileCapabilityDefinition.PubspecPackage> MergePackages(
        IReadOnlyList<MobileCapabilityDefinition> resolved)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in resolved)
        {
            foreach (var pkg in capability.PubspecPackages)
                map[pkg.Name] = pkg.Version;
        }

        return map
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new MobileCapabilityDefinition.PubspecPackage { Name = kv.Key, Version = kv.Value })
            .ToList();
    }

    private static object BuildProviderModel(IReadOnlyList<MobileCapabilityDefinition> services) => new
    {
        services = services.Select(s => new
        {
            id = s.Id,
            file = s.ServiceFileName!,
            provider_name = ToProviderName(s.ServiceFileName!),
            service_class = ToServiceClass(s.ServiceFileName!)
        }).ToList()
    };

    private static string ToServiceClass(string fileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var parts = baseName.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p =>
            char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static string ToProviderName(string fileName) =>
        Path.GetFileNameWithoutExtension(fileName) + "Provider";
}

public sealed record FlutterCapabilityEmitResult(
    IReadOnlyList<MobileCapabilityDefinition> Resolved,
    IReadOnlyList<MobileCapabilityDefinition.PubspecPackage> Packages,
    IReadOnlyList<MobileCapabilityDefinition> EmittedServices);
