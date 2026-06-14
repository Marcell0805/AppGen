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
        ("Entity/Controller.scriban", (s, e) => $"src/{s.ApiProject}/Controllers/V1/{NamingHelper.ToPlural(e.Name)}Controller.cs"),
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

        await MergePersistenceAsync(spec, entity, projectDirectory, ct);
        await MergeApplicationDiAsync(spec, entity, projectDirectory, ct);
        await AppendEntityToSpecAsync(spec, entity, projectDirectory, ct);
    }

    private async Task MergePersistenceAsync(SolutionSpec spec, EntitySpec entity, string dir, CancellationToken ct)
    {
        var dbContextPath = Path.Combine(dir, $"src/{spec.PersistenceProject}/Contexts/ApplicationDbContext.cs");
        var plural = NamingHelper.ToPlural(entity.Name);
        await RegionMergeHelper.MergeRegionAsync(dbContextPath,
            RegionMergeHelper.BuildRegion($"DbSet-{entity.Name}", $"    public DbSet<{entity.Name}> {plural} => Set<{entity.Name}>();"),
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
            Database = spec.Database,
            UiTargets = spec.UiTargets,
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
        var keys = entity.Properties.Where(p => p.IsKey).ToList();
        if (keys.Count == 0)
            keys = [new PropertySpec { Name = "Id", ClrType = "long", IsKey = true }];

        var primaryKey = keys[0];
        var pkRouteConstraint = primaryKey.ClrType switch
        {
            "int" => "int",
            "Guid" => "guid",
            _ => "long"
        };
        return new
        {
            app_name = spec.ApplicationName,
            root_namespace = spec.RootNamespace,
            entity_name = entity.Name,
            entity_plural = NamingHelper.ToPlural(entity.Name),
            entity_plural_lower = NamingHelper.ToPlural(entity.Name).ToLowerInvariant(),
            entity_camel = NamingHelper.ToCamelCase(entity.Name),
            primary_key_name = primaryKey.Name,
            primary_key_clr_type = primaryKey.ClrType,
            primary_key_route_constraint = pkRouteConstraint,
            primary_key_camel = NamingHelper.ToCamelCase(primaryKey.Name),
            table_name = entity.TableName ?? entity.Name,
            schema = entity.Schema,
            properties = entity.Properties.Select(p => new
            {
                name = p.Name,
                clr_type = p.IsNullable && p.ClrType != "string" ? p.ClrType + "?" : p.ClrType,
                type_name = p.ClrType,
                column_name = p.ColumnName ?? p.Name,
                is_key = p.IsKey,
                is_nullable = p.IsNullable
            }).ToList(),
            key_properties = keys.Select(p => new
            {
                name = p.Name,
                clr_type = p.ClrType,
                camel = NamingHelper.ToCamelCase(p.Name)
            }).ToList(),
            has_composite_key = keys.Count > 1,
            non_key_properties = entity.Properties.Where(p => !p.IsKey).Select(p => new
            {
                name = p.Name,
                clr_type = p.IsNullable && p.ClrType != "string" ? p.ClrType + "?" : p.ClrType,
                camel = NamingHelper.ToCamelCase(p.Name)
            }).ToList()
        };
    }
}
