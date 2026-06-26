using System.Text.Json;
using System.Text.Json.Serialization;
using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class EntityGenerator(TemplateRenderer renderer)
{
    private static readonly (string Template, Func<SolutionSpec, EntitySpec, string> Output)[] Files =
    [
        ("Entity/Entity.scriban", (s, e) => $"src/{s.DomainProject}/Entities/{e.Name}.cs"),
        ("Entity/EntityConfiguration.scriban", (s, e) => $"src/{s.PersistenceProject}/Configurations/{e.Name}Configuration.cs"),
        ("Entity/IRepository.scriban", (s, e) => $"src/{s.ApplicationProject}/Interfaces/I{e.Name}Repository.cs"),
        ("Entity/Repository.scriban", (s, e) => $"src/{s.PersistenceProject}/Repositories/{e.Name}Repository.cs"),
        ("Entity/IService.scriban", (s, e) => $"src/{s.ApplicationProject}/Services/I{e.Name}Service.cs"),
        ("Entity/Service.scriban", (s, e) => $"src/{s.ApplicationProject}/Services/{e.Name}Service.cs"),
        ("Entity/CreateRequest.scriban", (s, e) => $"src/{s.SharedProject}/Requests/Create{e.Name}Request.cs"),
        ("Entity/UpdateRequest.scriban", (s, e) => $"src/{s.SharedProject}/Requests/Update{e.Name}Request.cs"),
        ("Entity/Response.scriban", (s, e) => $"src/{s.SharedProject}/Responses/{e.Name}Response.cs"),
        ("Entity/Controller.scriban", (s, e) => $"src/{s.ApiProject}/Controllers/V1/{e.Name}Controller.cs"),
        ("Solution/tests/EntityApiTests.scriban", (s, e) => $"src/{s.TestsProject}/Api/{e.Name}ApiTests.cs"),
    ];

    public async Task GenerateAsync(SolutionSpec spec, EntitySpec entity, string projectDirectory, CancellationToken ct = default)
    {
        if (spec.Entities.Any(e => e.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Entity '{entity.Name}' already exists in appgen.json.");

        var model = BuildEntityModel(spec, entity);

        foreach (var (templatePath, outputPathFunc) in Files)
        {
            ct.ThrowIfCancellationRequested();
            var content = renderer.Render(TemplateProvider.Load(templatePath), model);
            var fullPath = Path.Combine(projectDirectory, outputPathFunc(spec, entity));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content, ct);
        }

        if (spec.UiTargets.HasFlag(UiTarget.MvcWeb))
        {
            ct.ThrowIfCancellationRequested();
            var mvcTestContent = renderer.Render(TemplateProvider.Load("Solution/tests/EntityMvcTests.scriban"), model);
            var mvcTestPath = Path.Combine(projectDirectory, $"src/{spec.TestsProject}/Mvc/{entity.Name}MvcTests.cs");
            Directory.CreateDirectory(Path.GetDirectoryName(mvcTestPath)!);
            await File.WriteAllTextAsync(mvcTestPath, mvcTestContent, ct);
        }

        await MergePersistenceAsync(spec, entity, projectDirectory, ct);
        await MergeApplicationDiAsync(spec, entity, projectDirectory, ct);
        await AppendEntityToSpecAsync(spec, entity, projectDirectory, ct);
    }

    private async Task MergePersistenceAsync(SolutionSpec spec, EntitySpec entity, string dir, CancellationToken ct)
    {
        var dbContextPath = Path.Combine(dir, $"src/{spec.PersistenceProject}/Contexts/ApplicationDbContext.cs");
        await RegionMergeHelper.MergeRegionAsync(dbContextPath,
            RegionMergeHelper.BuildRegion($"DbSet-{entity.Name}", $"    public DbSet<{entity.Name}> {entity.Name} => Set<{entity.Name}>();"),
            "// <AppGen-DbSets>", "// </AppGen-DbSets>", ct);
        await RegionMergeHelper.MergeRegionAsync(dbContextPath,
            RegionMergeHelper.BuildRegion($"Config-{entity.Name}", $"        modelBuilder.ApplyConfiguration(new {entity.Name}Configuration());"),
            "// <AppGen-Configurations>", "// </AppGen-Configurations>", ct);

        var diPath = Path.Combine(dir, $"src/{spec.PersistenceProject}/DependencyInjection.cs");
        var diRegion = RegionMergeHelper.BuildRegion(
            $"Entity-{entity.Name}",
            $"        services.AddScoped<I{entity.Name}Repository, {entity.Name}Repository>();");
        await RegionMergeHelper.MergeRegionAsync(diPath, diRegion,
            "// <AppGen-Repositories>", "// </AppGen-Repositories>", ct);
    }

    private async Task MergeApplicationDiAsync(SolutionSpec spec, EntitySpec entity, string dir, CancellationToken ct)
    {
        var diPath = Path.Combine(dir, $"src/{spec.ApplicationProject}/DependencyInjection.cs");
        var region = RegionMergeHelper.BuildRegion(
            $"Entity-{entity.Name}",
            $"        services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        await RegionMergeHelper.MergeRegionAsync(diPath, region,
            "// <AppGen-ApplicationServices>", "// </AppGen-ApplicationServices>", ct);
    }

    private static async Task AppendEntityToSpecAsync(SolutionSpec spec, EntitySpec entity, string dir, CancellationToken ct)
    {
        var updated = new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = spec.Project,
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = spec.Targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities.Concat([entity]).ToList()
        };

        var jsonPath = Path.Combine(dir, "appgen.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), new UiTargetJsonConverter() }
        };
        await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(updated, options), ct);
    }

    internal static object BuildEntityModel(SolutionSpec spec, EntitySpec entity)
    {
        var expandedProperties = ExpandEntityProperties(entity);
        var keys = expandedProperties.Where(p => p.IsKey).ToList();
        if (keys.Count == 0)
            keys = [new PropertySpec { Name = NamingHelper.ToKeyPropertyName(entity.Name), ClrType = "long", IsKey = true }];

        var primaryKey = keys[0];
        var pkRouteConstraint = primaryKey.ClrType switch
        {
            "int" => "int",
            "Guid" => "guid",
            _ => "long"
        };

        var oracleSchema = spec.Database == DatabaseProvider.Oracle && !string.IsNullOrWhiteSpace(spec.Setup.OracleSchemaPrefix)
            ? NamingHelper.ToOracleSchema(spec.Setup.OracleSchemaPrefix)
            : null;
        var oracleTableOnly = oracleSchema is not null
            ? NamingHelper.ToOracleTableNameOnly(spec.Setup.OracleSchemaPrefix!, entity.Name)
            : null;

        var foreignKeyProperties = BuildForeignKeyProperties(spec, entity);
        var foreignKeyServices = foreignKeyProperties
            .DistinctBy(fk => fk.ReferencedEntity)
            .ToList();

        return new
        {
            app_name = spec.ApplicationName,
            root_namespace = spec.RootNamespace,
            use_oracle = spec.Database == DatabaseProvider.Oracle,
            use_postgresql = spec.Database == DatabaseProvider.PostgreSql,
            use_sqlserver = spec.Database == DatabaseProvider.SqlServer,
            oracle_schema = oracleSchema,
            oracle_table_name_only = oracleTableOnly,
            entity_name = entity.Name,
            entity_plural = entity.Name,
            entity_plural_lower = entity.Name.ToLowerInvariant(),
            entity_camel = NamingHelper.ToCamelCase(entity.Name),
            primary_key_name = primaryKey.Name,
            primary_key_clr_type = primaryKey.ClrType,
            primary_key_route_constraint = pkRouteConstraint,
            primary_key_camel = NamingHelper.ToCamelCase(primaryKey.Name),
            table_name = entity.TableName ?? entity.Name,
            schema = entity.Schema,
            include_audit_columns = entity.IncludeAuditColumns,
            properties = expandedProperties.Select(p => new
            {
                name = p.Name,
                clr_type = p.IsNullable && p.ClrType != "string" ? p.ClrType + "?" : p.ClrType,
                type_name = p.ClrType,
                column_name = spec.Database == DatabaseProvider.Oracle
                    ? NamingHelper.ToOracleColumnName(p.ColumnName ?? p.Name)
                    : (p.ColumnName ?? p.Name),
            is_key = p.IsKey,
            is_nullable = p.IsNullable,
            is_identity_type = keys.Count == 1 && p.IsKey && (p.ClrType == "int" || p.ClrType == "long"),
                has_foreign_key = !p.IsKey && !string.IsNullOrWhiteSpace(p.ForeignKeyEntity),
                foreign_key_entity = p.ForeignKeyEntity
            }).ToList(),
            key_properties = keys.Select(p => new
            {
                name = p.Name,
                clr_type = p.ClrType,
                camel = NamingHelper.ToCamelCase(p.Name)
            }).ToList(),
            has_composite_key = keys.Count > 1,
            non_key_properties = expandedProperties.Where(p => !p.IsKey).Select(p => new
            {
                name = p.Name,
                clr_type = p.IsNullable && p.ClrType != "string" ? p.ClrType + "?" : p.ClrType,
                camel = NamingHelper.ToCamelCase(p.Name),
                test_update_value = ToTestValue(p.ClrType, p.Name, forUpdate: true)
            }).ToList(),
            create_properties = expandedProperties.Where(p => !p.IsKey && !IsAuditProperty(p.Name)).Select(p => new
            {
                name = p.Name,
                clr_type = p.IsNullable && p.ClrType != "string" ? p.ClrType + "?" : p.ClrType,
                camel = NamingHelper.ToCamelCase(p.Name),
                test_value = ToTestValue(p.ClrType, p.Name, forUpdate: false)
            }).ToList(),
            foreign_key_properties = foreignKeyProperties.Select(fk => new
            {
                name = fk.Name,
                referenced_entity = fk.ReferencedEntity,
                referenced_key_name = fk.ReferencedKeyName,
                display_property = fk.DisplayProperty,
                service_name = fk.ServiceName
            }).ToList(),
            foreign_key_services = foreignKeyServices.Select(fk => new
            {
                referenced_entity = fk.ReferencedEntity,
                service_name = fk.ServiceName
            }).ToList(),
            has_foreign_keys = foreignKeyProperties.Count > 0,
            auth_enabled = TargetFlags.AuthEnabled(spec),
            include_mvc_web = spec.UiTargets.HasFlag(UiTarget.MvcWeb)
        };
    }

    private static string ToTestValue(string clrType, string propertyName, bool forUpdate)
    {
        var baseType = clrType.TrimEnd('?');
        return baseType switch
        {
            "string" => forUpdate ? "\"Updated value\"" : "\"Test value\"",
            "bool" => forUpdate ? "false" : "true",
            "int" => forUpdate ? "2" : "1",
            "long" => forUpdate ? "2L" : "1L",
            "decimal" => forUpdate ? "2.0m" : "1.0m",
            "double" => forUpdate ? "2.0" : "1.0",
            "float" => forUpdate ? "2.0f" : "1.0f",
            "DateTime" => "DateTime.UtcNow",
            "Guid" => forUpdate
                ? "Guid.Parse(\"22222222-2222-2222-2222-222222222222\")"
                : "Guid.Parse(\"11111111-1111-1111-1111-111111111111\")",
            _ => forUpdate ? "\"Updated\"" : "\"Test\""
        };
    }

    internal static List<PropertySpec> ExpandEntityProperties(EntitySpec entity)
    {
        var list = entity.Properties.ToList();
        if (entity.IncludeAuditColumns)
        {
            if (list.All(p => !p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase)))
                list.Add(new PropertySpec { Name = "CreatedAt", ClrType = "DateTime" });
            if (list.All(p => !p.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase)))
                list.Add(new PropertySpec { Name = "UpdatedAt", ClrType = "DateTime", IsNullable = true });
        }
        return list;
    }

    private static bool IsAuditProperty(string name) =>
        name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase);

    private static List<ForeignKeyModel> BuildForeignKeyProperties(SolutionSpec spec, EntitySpec entity)
    {
        var result = new List<ForeignKeyModel>();
        foreach (var property in entity.Properties.Where(p => !p.IsKey && !string.IsNullOrWhiteSpace(p.ForeignKeyEntity)))
        {
            var referenced = spec.Entities.FirstOrDefault(e =>
                e.Name.Equals(property.ForeignKeyEntity, StringComparison.OrdinalIgnoreCase));
            if (referenced is null)
                continue;

            var refKey = referenced.Properties.FirstOrDefault(p => p.IsKey)
                ?? new PropertySpec
                {
                    Name = NamingHelper.ToKeyPropertyName(referenced.Name),
                    ClrType = "long",
                    IsKey = true
                };

            var displayProperty = referenced.Properties
                .FirstOrDefault(p => !p.IsKey && p.ClrType == "string")?.Name ?? refKey.Name;

            result.Add(new ForeignKeyModel(
                property.Name,
                referenced.Name,
                refKey.Name,
                displayProperty,
                NamingHelper.ToCamelCase(referenced.Name) + "Service"));
        }

        return result;
    }

    private sealed record ForeignKeyModel(
        string Name,
        string ReferencedEntity,
        string ReferencedKeyName,
        string DisplayProperty,
        string ServiceName);
}
