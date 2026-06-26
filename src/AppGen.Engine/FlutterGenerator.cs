using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class FlutterGenerator(TemplateRenderer renderer)
{
    public const string TargetId = "mobile";
    private readonly FlutterCapabilityEmitter _capabilityEmitter = new(renderer);

    public async Task GenerateAllAsync(
        SolutionSpec spec,
        IReadOnlyList<EntitySpec> entities,
        string projectDirectory,
        MobileTargetSpec mobile,
        CancellationToken ct = default)
    {
        if (entities.Count == 0)
            throw new ArgumentException("At least one entity is required.", nameof(entities));

        var flutterRoot = Path.Combine(projectDirectory, "mobile", "flutter");
        Directory.CreateDirectory(flutterRoot);

        var firstEntity = entities[0];
        var appModel = BuildAppModel(spec, mobile, entities);
        var firstModel = BuildModel(spec, firstEntity, mobile, ToSnakeCase(firstEntity.Name));

        await WriteTemplateAsync("Mobile/flutter/pubspec.yaml.scriban", flutterRoot, "pubspec.yaml", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/main.dart.scriban", flutterRoot, "lib/main.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/api_config.dart.scriban", flutterRoot, "lib/core/config/api_config.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/api_client.dart.scriban", flutterRoot, "lib/core/network/api_client.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/theme.dart.scriban", flutterRoot, "lib/app/theme.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_colors.dart.scriban", flutterRoot, "lib/app/app_colors.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_theme_config.dart.scriban", flutterRoot, "lib/app/app_theme_config.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_drawer.dart.scriban", flutterRoot, "lib/app/app_drawer.dart", appModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_page_header.dart.scriban", flutterRoot, "lib/core/widgets/app_page_header.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_shell.dart.scriban", flutterRoot, "lib/app/app_shell.dart", appModel, ct);
        await WriteTemplateAsync("Mobile/flutter/app_widgets.dart.scriban", flutterRoot, "lib/core/widgets/app_widgets.dart", firstModel, ct);
        await WriteTemplateAsync("Mobile/flutter/router.dart.scriban", flutterRoot, "lib/app/router.dart", appModel, ct);

        if (TargetFlags.AuthEnabled(spec))
        {
            await WriteTemplateAsync("Mobile/flutter/token_storage.dart.scriban", flutterRoot, "lib/core/auth/token_storage.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/auth_service.dart.scriban", flutterRoot, "lib/core/auth/auth_service.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/auth_provider.dart.scriban", flutterRoot, "lib/core/auth/auth_provider.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/auth_interceptor.dart.scriban", flutterRoot, "lib/core/network/auth_interceptor.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/login_screen.dart.scriban", flutterRoot, "lib/features/auth/screens/login_screen.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/api_client.auth.scriban", flutterRoot, "lib/core/network/api_client.dart", firstModel, ct);
        }

        if (TargetFlags.OfflineEnabled(spec))
        {
            await WriteTemplateAsync("Mobile/flutter/offline_cache.dart.scriban", flutterRoot, "lib/core/offline/offline_cache.dart", firstModel, ct);
            await WriteTemplateAsync("Mobile/flutter/offline_banner.dart.scriban", flutterRoot, "lib/core/widgets/offline_banner.dart", firstModel, ct);
        }

        foreach (var entity in entities)
        {
            var entitySnake = ToSnakeCase(entity.Name);
            var model = BuildModel(spec, entity, mobile, entitySnake);
            await WriteTemplateAsync("Mobile/flutter/entity_model.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/models/{entitySnake}_model.dart", model, ct);
            await WriteTemplateAsync("Mobile/flutter/entity_service.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/services/{entitySnake}_service.dart", model, ct);
            var providerTemplate = TargetFlags.OfflineEnabled(spec)
                ? "Mobile/flutter/entity_provider.offline.scriban"
                : "Mobile/flutter/entity_provider.dart.scriban";
            await WriteTemplateAsync(providerTemplate, flutterRoot, $"lib/features/{entitySnake}/providers/{entitySnake}_provider.dart", model, ct);
            await WriteTemplateAsync("Mobile/flutter/entity_list_screen.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/screens/{entitySnake}_list_screen.dart", model, ct);
            await WriteTemplateAsync("Mobile/flutter/entity_detail_screen.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/screens/{entitySnake}_detail_screen.dart", model, ct);
            await WriteTemplateAsync("Mobile/flutter/entity_form_screen.dart.scriban", flutterRoot, $"lib/features/{entitySnake}/screens/{entitySnake}_form_screen.dart", model, ct);
        }

        await _capabilityEmitter.EmitAsync(spec, flutterRoot, firstModel, ct);

        var legacyTargets = SpecNormalizer.BuildTargetsFromLegacy(spec);
        var mobileTarget = new MobileTargetSpec
        {
            Enabled = true,
            Framework = mobile.Framework,
            PackageName = string.IsNullOrWhiteSpace(mobile.PackageName) ? legacyTargets.Mobile.PackageName : mobile.PackageName,
            ApiBaseUrl = mobile.ApiBaseUrl,
            StateManagement = mobile.StateManagement,
            Theme = mobile.Theme,
            Offline = mobile.Offline,
            Capabilities = spec.Targets?.Mobile.Capabilities ?? mobile.Capabilities
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
                    Entities = entities.Select(e => e.Name).ToList()
                }
            },
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };

        await ProjectSpecWriter.WriteAsync(updated, projectDirectory, ct);
    }

    public Task GenerateAsync(
        SolutionSpec spec,
        EntitySpec entity,
        string projectDirectory,
        MobileTargetSpec mobile,
        CancellationToken ct = default) =>
        GenerateAllAsync(spec, [entity], projectDirectory, mobile, ct);

    private async Task WriteTemplateAsync(string template, string root, string relativePath, object model, CancellationToken ct)
    {
        var content = renderer.Render(TemplateProvider.Load(template), model);
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, ct);
    }

    internal static object BuildAppModel(SolutionSpec spec, MobileTargetSpec mobile, IReadOnlyList<EntitySpec> entities)
    {
        var firstEntity = entities[0];
        var firstSnake = ToSnakeCase(firstEntity.Name);
        var theme = FlutterThemeResolver.Resolve(spec, mobile);
        var tagline = ResolveTagline(spec);
        var version = spec.Portal?.Settings.Version ?? "1.0.0";

        return new
        {
            app_name = spec.ApplicationName,
            app_class_name = spec.ApplicationName.Replace("_", "", StringComparison.Ordinal),
            app_name_lower = spec.ApplicationName.ToLowerInvariant().Replace("_", "", StringComparison.Ordinal),
            package_name = string.IsNullOrWhiteSpace(mobile.PackageName)
                ? $"com.{NamingHelper.NormalizeAppName(spec.ApplicationName).ToLowerInvariant()}.app"
                : mobile.PackageName,
            api_base_url = mobile.ApiBaseUrl,
            first_entity_snake = firstSnake,
            project_tagline = tagline,
            app_version = version,
            theme_preset = theme.Preset,
            theme_sidebar = theme.Sidebar,
            theme_sidebar_accent = theme.SidebarAccent,
            theme_accent = theme.Accent,
            theme_accent_muted = theme.AccentMuted,
            theme_background = theme.Background,
            theme_surface = theme.Surface,
            theme_border = theme.Border,
            theme_text = theme.Text,
            theme_text_muted = theme.TextMuted,
            theme_highlight = theme.Highlight,
            theme_success = theme.Success,
            theme_error = theme.Error,
            auth_enabled = TargetFlags.AuthEnabled(spec),
            offline_enabled = TargetFlags.OfflineEnabled(spec),
            capability_packages = BuildCapabilityPackages(spec),
            entities = entities.Select((e, index) =>
            {
                var keyProp = e.Properties.FirstOrDefault(p => p.IsKey);
                return new
                {
                    entity_name = e.Name,
                    entity_snake = ToSnakeCase(e.Name),
                    entity_number = index + 1,
                    entity_description = ResolveEntityDescription(spec, e.Name),
                    key_dart_type = keyProp is null ? "int" : ToDartType(keyProp.ClrType)
                };
            }).ToList()
        };
    }

    internal static object BuildModel(SolutionSpec spec, EntitySpec entity, MobileTargetSpec mobile, string entitySnake)
    {
        var keyProp = entity.Properties.FirstOrDefault(p => p.IsKey);
        var displayProp = entity.Properties.FirstOrDefault(p => !p.IsKey);
        var theme = FlutterThemeResolver.Resolve(spec, mobile);
        var tagline = ResolveTagline(spec);
        var dartProps = entity.Properties.Select(p => new
        {
            name = ToDartFieldName(p.Name),
            label = p.Name,
            dart_type = ToDartType(p.ClrType),
            json_key = ToJsonKey(p.Name),
            is_key = p.IsKey
        }).ToList();

        var editableProps = entity.Properties.Where(p => !p.IsKey).Select(p => new
        {
            name = ToDartFieldName(p.Name),
            label = p.Name,
            dart_type = ToDartType(p.ClrType),
            json_key = ToJsonKey(p.Name)
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
            project_tagline = tagline,
            theme_preset = theme.Preset,
            theme_sidebar = theme.Sidebar,
            theme_sidebar_accent = theme.SidebarAccent,
            theme_accent = theme.Accent,
            theme_accent_muted = theme.AccentMuted,
            theme_background = theme.Background,
            theme_surface = theme.Surface,
            theme_border = theme.Border,
            theme_text = theme.Text,
            theme_text_muted = theme.TextMuted,
            theme_highlight = theme.Highlight,
            theme_success = theme.Success,
            theme_error = theme.Error,
            auth_enabled = TargetFlags.AuthEnabled(spec),
            offline_enabled = TargetFlags.OfflineEnabled(spec),
            capability_packages = BuildCapabilityPackages(spec),
            entity_name = entity.Name,
            entity_snake = entitySnake,
            entity_description = ResolveEntityDescription(spec, entity.Name),
            entity_var = ToCamelCase(entity.Name),
            key_field = keyProp is null ? "id" : ToDartFieldName(keyProp.Name),
            key_dart_type = keyProp is null ? "int" : ToDartType(keyProp.ClrType),
            has_display_field = displayProp is not null,
            display_field = displayProp is null ? "" : ToDartFieldName(displayProp.Name),
            key_json_key = keyProp is null ? "id" : ToJsonKey(keyProp.Name),
            has_editable_properties = editableProps.Count > 0,
            editable_properties = editableProps,
            properties = dartProps
        };
    }

    private static List<object> BuildCapabilityPackages(SolutionSpec spec) =>
        FlutterCapabilityEmitter.MergePackages(MobileCapabilityResolver.Resolve(spec))
            .Select(p => (object)new { name = p.Name, version = p.Version })
            .ToList();

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

    /// <summary>Matches System.Text.Json default camelCase (first character only).</summary>
    internal static string ToJsonKey(string propertyName) => ToCamelCase(propertyName);

    internal static string ToDartFieldName(string propertyName)
    {
        if (propertyName.EndsWith("_Id", StringComparison.Ordinal))
            return ToCamelCase(propertyName[..^3]) + "Id";
        return ToCamelCase(propertyName);
    }

    private static string ResolveTagline(SolutionSpec spec)
    {
        string tagline;
        if (!string.IsNullOrWhiteSpace(spec.Project?.Tagline))
            tagline = spec.Project.Tagline.Trim();
        else if (!string.IsNullOrWhiteSpace(spec.Portal?.Settings.Tagline))
            tagline = spec.Portal.Settings.Tagline!.Trim();
        else
            tagline = "Generated by AppGen";

        return SanitizeDartString(tagline);
    }

    private static string ResolveEntityDescription(SolutionSpec spec, string entityName)
    {
        var sketch = spec.EntitySketches.FirstOrDefault(s =>
            s.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(sketch?.Description))
            return SanitizeDartString(sketch.Description.Trim());

        return SanitizeDartString($"Browse and manage {entityName} records.");
    }

    private static string SanitizeDartString(string value) =>
        value.Replace('\\', '/').Replace("'", "\\'").Replace('\n', ' ').Replace('\r', ' ');
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

        var entities = ResolveEntities(normalized, options);
        if (entities.Count == 0)
            return GeneratorTargetResult.Fail("No entity available. Define entities in the Web tab or pass --entity.");

        try
        {
            await flutterGenerator.GenerateAllAsync(normalized, entities, projectDirectory, mobile, ct);

            var flutterRoot = Path.Combine(projectDirectory, "mobile", "flutter");
            var projectName = normalized.ApplicationName.ToLowerInvariant().Replace("_", "", StringComparison.Ordinal);
            var scaffold = await FlutterPlatformScaffolder.ScaffoldAsync(
                flutterRoot,
                projectName,
                FlutterPlatformScaffolder.ResolveAndroidOrg(mobile.PackageName),
                normalized.ApplicationName,
                projectDirectory,
                ct);

            var capabilities = MobileCapabilityResolver.Resolve(normalized);
            var patch = await FlutterPlatformConfigPatcher.PatchAsync(flutterRoot, capabilities, ct);

            var names = string.Join(", ", entities.Select(e => e.Name));
            var message = entities.Count == 1
                ? $"Flutter CRUD generated for entity '{entities[0].Name}'."
                : $"Flutter CRUD generated for {entities.Count} entities: {names}.";

            if (!string.IsNullOrWhiteSpace(scaffold.Message))
                message += " " + scaffold.Message;

            if (!string.IsNullOrWhiteSpace(patch.Message))
                message += " " + patch.Message;

            return GeneratorTargetResult.Ok(flutterRoot, message);
        }
        catch (Exception ex)
        {
            return GeneratorTargetResult.Fail(ex.Message);
        }
    }

    private static List<EntitySpec> ResolveEntities(SolutionSpec spec, GeneratorOptions options)
    {
        if (options.EntityNames is { Count: > 0 })
        {
            return options.EntityNames
                .Select(name => spec.Entities.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(entity => entity is not null)
                .Cast<EntitySpec>()
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(options.EntityName))
        {
            var entity = spec.Entities.FirstOrDefault(e => e.Name.Equals(options.EntityName, StringComparison.OrdinalIgnoreCase));
            return entity is null ? [] : [entity];
        }

        var uiEntities = spec.Entities.Where(e => e.IncludeInUi).ToList();
        return uiEntities.Count > 0 ? uiEntities : spec.Entities.ToList();
    }
}
