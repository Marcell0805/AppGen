using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Core.Models.SpecWorkbook;
using AppGen.Engine;
using System.CommandLine;

var root = new RootCommand("AppGen — generate layered ASP.NET Core API solutions and static portals");

var createCmd = new Command("create", "Scaffold a new API solution");
var appNameArg = new Argument<string>("name", "Application name (e.g. InventorySystem)");
var outputOpt = new Option<string>("--output", () => ".", "Output directory");
var namespaceOpt = new Option<string?>("--namespace", "Root namespace (defaults to app name)");
var databaseOpt = new Option<string>("--database", () => "SqlServer", "SqlServer, Oracle, or PostgreSql");
var uiOpt = new Option<string?>("--ui", "Optional UI targets: MvcWeb (comma-separated)");
var forceOpt = new Option<bool>("--force", "Delete existing output folder and regenerate");

createCmd.AddArgument(appNameArg);
createCmd.AddOption(outputOpt);
createCmd.AddOption(namespaceOpt);
createCmd.AddOption(databaseOpt);
createCmd.AddOption(uiOpt);
createCmd.AddOption(forceOpt);
createCmd.SetHandler(async (name, output, ns, db, ui, force) =>
{
    var database = ParseDatabaseProvider(db);
    var uiTargets = ParseUiTargets(ui);
    var setup = NamingHelper.DefaultSetup(database);
    var spec = SpecLoader.CreateDefault(name, ns, database, uiTargets, setup);
    var outputDir = GenerationOutputHelper.ResolveOutputDirectory(output, spec.ApplicationName);
    if (GenerationOutputHelper.OutputDirectoryExists(outputDir))
    {
        if (!force)
        {
            Console.Error.WriteLine($"Output directory is not empty: {outputDir}");
            Console.Error.WriteLine("Use --force to delete and regenerate.");
            return;
        }

        GenerationOutputHelper.DeleteOutputDirectory(outputDir);
    }

    var renderer = new TemplateRenderer();
    var generator = new SolutionGenerator(renderer);
    await generator.GenerateAsync(spec, outputDir);
    await AppSettingsGenerator.WriteAsync(spec, outputDir);
    await ReadmeGenerator.WriteAsync(spec, outputDir);
    await ProjectSpecWriter.WriteAsync(spec, outputDir);
    Console.WriteLine($"Created solution at: {outputDir}");
    if (uiTargets.HasFlag(UiTarget.MvcWeb))
        Console.WriteLine("Included MVC Web UI project.");
    if (database == DatabaseProvider.Oracle)
        Console.WriteLine("Oracle SQL scripts are generated when you add entities.");
    else if (database == DatabaseProvider.PostgreSql)
        Console.WriteLine("PostgreSQL via Npgsql — SQL scripts generated when you add entities.");
    else if (database == DatabaseProvider.SqlServer)
        Console.WriteLine("SQL Server — SQL scripts generated when you add entities.");
    Console.WriteLine($"Next: cd \"{outputDir}\" && dotnet build");
}, appNameArg, outputOpt, namespaceOpt, databaseOpt, uiOpt, forceOpt);

var portalCmd = new Command("portal", "Static portal operations");
var portalCreateCmd = new Command("create", "Scaffold a static documentation portal");
var portalNameArg = new Argument<string>("name", "Application / portal name");
var portalOutputOpt = new Option<string>("--output", () => ".", "Output directory");
var portalPresetOpt = new Option<string>("--preset", () => "engineering-portal", "Portal preset");
var portalForceOpt = new Option<bool>("--force", "Overwrite existing portal output");

portalCreateCmd.AddArgument(portalNameArg);
portalCreateCmd.AddOption(portalOutputOpt);
portalCreateCmd.AddOption(portalPresetOpt);
portalCreateCmd.AddOption(portalForceOpt);
portalCreateCmd.SetHandler(async (name, output, preset, force) =>
{
    var spec = SpecLoader.CreatePortalDefault(name, null, preset: preset);
    var outputDir = GenerationOutputHelper.ResolveOutputDirectory(output, spec.ApplicationName);
    Directory.CreateDirectory(Path.GetDirectoryName(outputDir)!);

    var renderer = new TemplateRenderer();
    var generator = new PortalGenerator(renderer);
    var service = new PortalGenerationService(generator);
    var result = await service.GenerateAsync(spec, output, force);
    if (!result.Success)
    {
        Console.Error.WriteLine(result.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(result.Message);
    Console.WriteLine($"Manifest: {Path.Combine(result.OutputDirectory!, "appgen.json")}");
}, portalNameArg, portalOutputOpt, portalPresetOpt, portalForceOpt);

var portalImportCmd = new Command("import", "Import portal/data edits into appgen.json");
var portalImportProjectOpt = new Option<string>("--project", () => ".", "Path to project root");

portalImportCmd.AddOption(portalImportProjectOpt);
portalImportCmd.SetHandler(async project =>
{
    try
    {
        var spec = await PortalSpecImporter.ImportAsync(Path.GetFullPath(project));
        Console.WriteLine($"Imported portal content into {Path.Combine(Path.GetFullPath(project), "appgen.json")} ({spec.Portal?.Sections.Count ?? 0} sections).");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}, portalImportProjectOpt);

portalCmd.AddCommand(portalCreateCmd);
portalCmd.AddCommand(portalImportCmd);

var promoteCmd = new Command("promote", "Generate API solution from appgen.json (portal projects)");
var promoteProjectOpt = new Option<string>("--project", () => ".", "Path to project root");
var promoteForceOpt = new Option<bool>("--force", "Regenerate existing src/ solution");

promoteCmd.AddOption(promoteProjectOpt);
promoteCmd.AddOption(promoteForceOpt);
promoteCmd.SetHandler(async (project, force) =>
{
    var renderer = new TemplateRenderer();
    var promoter = new ProjectPromoter(
        new SolutionGenerator(renderer),
        new EntityGenerator(renderer),
        new UiGenerator(renderer));

    var result = await promoter.PromoteAsync(Path.GetFullPath(project), force);
    if (!result.Success)
    {
        Console.Error.WriteLine(result.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(result.Message);
}, promoteProjectOpt, promoteForceOpt);

var mobileCmd = new Command("mobile", "Mobile app operations");
var mobileCreateCmd = new Command("create", "Generate Flutter mobile client from appgen.json");
var mobileProjectOpt = new Option<string>("--project", () => ".", "Path to project root");
var mobileEntityOpt = new Option<string?>("--entity", "Single entity to scaffold (defaults to all UI entities)");
var mobileForceOpt = new Option<bool>("--force", "Overwrite existing mobile output");

mobileCreateCmd.AddOption(mobileProjectOpt);
mobileCreateCmd.AddOption(mobileEntityOpt);
mobileCreateCmd.AddOption(mobileForceOpt);
mobileCreateCmd.SetHandler(async (project, entity, force) =>
{
    var manifestDir = Path.GetFullPath(project);
    var spec = await SpecLoader.LoadAsync(manifestDir);
    var outputRoot = GenerationOutputHelper.ResolveOutputRootFromManifestDirectory(manifestDir, spec.ApplicationName);
    IReadOnlyList<string>? entityNames = string.IsNullOrWhiteSpace(entity) ? null : [entity.Trim()];
    var renderer = new TemplateRenderer();
    var service = new MobileGenerationService(new MobileApplicationGenerator(new FlutterGenerator(renderer)));
    var result = await service.GenerateAsync(
        spec,
        outputRoot,
        entityNames,
        forceRegenerate: force);

    if (!result.Success)
    {
        Console.Error.WriteLine(result.Message);
        Environment.ExitCode = 1;
        return;
    }

    Console.WriteLine(result.Message);
}, mobileProjectOpt, mobileEntityOpt, mobileForceOpt);

mobileCmd.AddCommand(mobileCreateCmd);

var entityCmd = new Command("entity", "Entity operations");
var addCmd = new Command("add", "Add an entity and generate CRUD artifacts");
var entityNameArg = new Argument<string>("name", "Entity name (e.g. Product)");
var projectOpt = new Option<string>("--project", () => ".", "Path to generated solution root");

addCmd.AddArgument(entityNameArg);
addCmd.AddOption(projectOpt);
addCmd.SetHandler(async (entityName, project) =>
{
    var projectDir = Path.GetFullPath(project);
    var spec = await SpecLoader.LoadAsync(projectDir);
    var keyName = NamingHelper.ToKeyPropertyName(entityName);

    var entity = new EntitySpec
    {
        Name = entityName,
        Properties =
        [
            new PropertySpec { Name = keyName, ClrType = "long", IsKey = true },
            new PropertySpec { Name = "Name", ClrType = "string", IsNullable = true }
        ]
    };

    if (entityName.Equals("Product", StringComparison.OrdinalIgnoreCase))
    {
        entity = new EntitySpec
        {
            Name = "Product",
            Properties =
            [
                new PropertySpec { Name = NamingHelper.ToKeyPropertyName("Product"), ClrType = "long", IsKey = true },
                new PropertySpec { Name = "Name", ClrType = "string" },
                new PropertySpec { Name = "Price", ClrType = "decimal" },
                new PropertySpec { Name = "SupplierId", ClrType = "long" }
            ]
        };
    }

    var renderer = new TemplateRenderer();
    var generator = new EntityGenerator(renderer);
    var uiGenerator = new UiGenerator(renderer);
    await generator.GenerateAsync(spec, entity, projectDir);
    var updated = await SpecLoader.LoadAsync(projectDir);
    await uiGenerator.GenerateAsync(updated, entity, projectDir);
    await DatabaseScriptGenerator.WriteAsync(updated, projectDir);
    await ReadmeGenerator.WriteAsync(updated, projectDir);
    Console.WriteLine($"Added entity '{entityName}' to {projectDir}");
    if (updated.Entities.Count > 0)
        Console.WriteLine($"Updated {DatabaseScriptGenerator.ScriptsFolder(updated.Database)} SQL scripts.");
    Console.WriteLine("Run: dotnet build");
}, entityNameArg, projectOpt);

entityCmd.AddCommand(addCmd);

var specCmd = new Command("spec", "Import and export AppGen spec workbooks (Excel)");
var specExportCmd = new Command("export", "Export appgen.json to an Excel workbook");
var specImportCmd = new Command("import", "Import an Excel workbook into appgen.json");
var specOutputOpt = new Option<string>("--output", "Output .xlsx path") { IsRequired = true };
var specInputOpt = new Option<string>("--input", "Input .xlsx path") { IsRequired = true };
var specProjectOpt = new Option<string>("--project", () => ".", "Path to project root (hub folder with appgen.json)");
var specTemplateOpt = new Option<bool>("--template", () => false, "Export an empty template (ignores --project)");
var specMergeOpt = new Option<bool>("--merge", () => false, "Merge imported entities with existing manifest entities");
var specValidateOnlyOpt = new Option<bool>("--validate-only", () => false, "Validate workbook without writing files");

specExportCmd.AddOption(specOutputOpt);
specExportCmd.AddOption(specProjectOpt);
specExportCmd.AddOption(specTemplateOpt);
specExportCmd.SetHandler(async (output, project, template) =>
{
    try
    {
        await SpecExportService.ExportAsync(
            Path.GetFullPath(output),
            template ? null : Path.GetFullPath(project),
            template);
        Console.WriteLine($"Exported workbook: {Path.GetFullPath(output)}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}, specOutputOpt, specProjectOpt, specTemplateOpt);

specImportCmd.AddOption(specInputOpt);
specImportCmd.AddOption(specProjectOpt);
specImportCmd.AddOption(specMergeOpt);
specImportCmd.AddOption(specValidateOnlyOpt);
specImportCmd.SetHandler(async (input, project, merge, validateOnly) =>
{
    try
    {
        var result = await SpecImportService.ImportAsync(
            Path.GetFullPath(project),
            Path.GetFullPath(input),
            new SpecImportOptions
            {
                MergeEntities = merge,
                ValidateOnly = validateOnly
            });

        if (!result.Success)
        {
            Console.Error.WriteLine(result.Message);
            Environment.ExitCode = 1;
            return;
        }

        Console.WriteLine(result.Message);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(ex.Message);
        Environment.ExitCode = 1;
    }
}, specInputOpt, specProjectOpt, specMergeOpt, specValidateOnlyOpt);

specCmd.AddCommand(specExportCmd);
specCmd.AddCommand(specImportCmd);

root.AddCommand(createCmd);
root.AddCommand(portalCmd);
root.AddCommand(promoteCmd);
root.AddCommand(mobileCmd);
root.AddCommand(entityCmd);
root.AddCommand(specCmd);

return await root.InvokeAsync(args);

static UiTarget ParseUiTargets(string? raw)
{
    if (string.IsNullOrWhiteSpace(raw))
        return UiTarget.None;

    var value = UiTarget.None;
    foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        if (string.Equals(part, "BlazorWeb", StringComparison.OrdinalIgnoreCase))
            value |= UiTarget.MvcWeb;
        else
            value |= Enum.Parse<UiTarget>(part, ignoreCase: true);
    }
    return value;
}

static DatabaseProvider ParseDatabaseProvider(string raw)
{
    if (string.Equals(raw, "Postgres", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(raw, "PostgreSQL", StringComparison.OrdinalIgnoreCase))
        return DatabaseProvider.PostgreSql;

    return Enum.Parse<DatabaseProvider>(raw, ignoreCase: true);
}
