using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class FlutterGenerator(TemplateRenderer renderer)
{
    public const string TargetId = "mobile";

    public async Task GenerateAsync(
        SolutionSpec spec,
        EntitySpec entity,
        string projectDirectory,
        MobileTargetSpec mobile,
        CancellationToken ct = default)
    {
        var flutterRoot = Path.Combine(projectDirectory, "mobile", "flutter");
        Directory.CreateDirectory(flutterRoot);

        var entitySnake = ToSnakeCase(entity.Name);
        var model = BuildModel(spec, entity, mobile, entitySnake);

        await WriteTemplateAsync("Mobile/flutter/pubspec.yaml.scriban", flutterRoot, "pubspec.yaml", model, ct);
        await WriteTemplateAsync("Mobile/flutter/main.dart.scriban", flutterRoot, "lib/main.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/api_config.dart.scriban", flutterRoot, "lib/core/config/api_config.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/api_client.dart.scriban", flutterRoot, "lib/core/network/api_client.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/theme.dart.scriban", flutterRoot, "lib/app/theme.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/router.dart.scriban", flutterRoot, "lib/app/router.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/entity_model.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/models/{entitySnake}_model.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/entity_service.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/services/{entitySnake}_service.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/entity_provider.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/providers/{entitySnake}_provider.dart", model, ct);
        await WriteTemplateAsync("Mobile/flutter/entity_list_screen.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/screens/{entitySnake}_list_screen.dart", model, ct);

        var legacyTargets = SpecNormalizer.BuildTargetsFromLegacy(spec);
        var mobileTarget = new MobileTargetSpec
        {
            Enabled = true,
            Framework = mobile.Framework,
            PackageName = string.IsNullOrWhiteSpace(mobile.PackageName) ? legacyTargets.Mobile.PackageName : mobile.PackageName,
            ApiBaseUrl = mobile.ApiBaseUrl,
            StateManagement = mobile.StateManagement
        };

        var updated = new SolutionSpec
        {
            SchemaVersion = SolutionSpec.CurrentSchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = new ApplicationTargets
            {
                Documentation = spec.Targets?.Documentation ?? legacyTargets.Documentation,
                Web = spec.Targets?.Web ?? legacyTargets.Web,
                Mobile = mobileTarget
            },
            Generation = new GenerationMetadata
            {
                Mobile = new TargetGenerationInfo
                {
                    LastGenerated = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                    Entities = [entity.Name]
                }
            },
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };

        await ProjectSpecWriter.WriteAsync(updated, projectDirectory, ct);
    }

    private async Task WriteTemplateAsync(string template, string root, string relativePath, object model, CancellationToken ct)
    {
        var content = renderer.Render(TemplateProvider.Load(template), model);
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, ct);
    }

    internal static object BuildModel(SolutionSpec spec, EntitySpec entity, MobileTargetSpec mobile, string entitySnake)
    {
        var keyProp = entity.Properties.FirstOrDefault(p => p.IsKey);
        var dartProps = entity.Properties.Select(p => new
        {
            name = ToDartFieldName(p.Name),
            dart_type = ToDartType(p.ClrType),
            json_key = p.Name,
            is_key = p.IsKey
        }).ToList();

        return new
        {
            app_name = spec.ApplicationName,
            app_class_name = spec.ApplicationName.Replace("_", "", StringComparison.Ordinal),
            app_name_lower = spec.ApplicationName.ToLowerInvariant().Replace("_", "", StringComparison.Ordinal),
            package_name = string.IsNullOrWhiteSpace(mobile.PackageName)
                ? $"com.{NamingHelper.NormalizeAppName(spec.ApplicationName).ToLowerInvariant()}.app"
                : mobile.PackageName,
            api_base_url = mobile.ApiBaseUrl,
            entity_name = entity.Name,
            entity_snake = entitySnake,
            entity_var = ToCamelCase(entity.Name),
            key_field = keyProp is null ? "id" : ToDartFieldName(keyProp.Name),
            key_dart_type = keyProp is null ? "int" : ToDartType(keyProp.ClrType),
            properties = dartProps
        };
    }

    internal static string ToDartType(string clrType) => clrType.Trim() switch
    {
        "string" => "String",
        "bool" => "bool",
        "int" or "long" => "int",
        "decimal" or "double" or "float" => "double",
        "DateTime" => "DateTime",
        "Guid" => "String",
        _ => "String"
    };

    internal static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var chars = new List<char>();
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('_');
            chars.Add(char.ToLowerInvariant(c));
        }
        return new string(chars.ToArray());
    }

    internal static string ToCamelCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    internal static string ToDartFieldName(string propertyName)
    {
        if (propertyName.EndsWith("_Id", StringComparison.Ordinal))
            return ToCamelCase(propertyName[..^3]) + "Id";
        return ToCamelCase(propertyName);
    }
}

public sealed class MobileApplicationGenerator(FlutterGenerator flutterGenerator) : IApplicationGenerator
{
    public string TargetId => FlutterGenerator.TargetId;

    public async Task<GeneratorTargetResult> GenerateAsync(
        SolutionSpec spec,
        string projectDirectory,
        GeneratorOptions options,
        CancellationToken ct = default)
    {
        var normalized = SpecNormalizer.Normalize(spec);
        var mobile = normalized.Targets?.Mobile ?? new MobileTargetSpec
        {
            PackageName = $"com.{normalized.ApplicationName.ToLowerInvariant()}.app"
        };

        var entity = ResolveEntity(normalized, options.EntityName);
        if (entity is null)
            return GeneratorTargetResult.Fail("No entity available. Define entities in the Web tab or pass --entity.");

        try
        {
            await flutterGenerator.GenerateAsync(normalized, entity, projectDirectory, mobile, ct);
            return GeneratorTargetResult.Ok(
                Path.Combine(projectDirectory, "mobile", "flutter"),
                $"Flutter POC generated for entity '{entity.Name}'.");
        }
        catch (Exception ex)
        {
            return GeneratorTargetResult.Fail(ex.Message);
        }
    }

    private static EntitySpec? ResolveEntity(SolutionSpec spec, string? entityName)
    {
        if (!string.IsNullOrWhiteSpace(entityName))
            return spec.Entities.FirstOrDefault(e => e.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        return spec.Entities.FirstOrDefault(e => e.IncludeInUi)
            ?? spec.Entities.FirstOrDefault();
    }
}
