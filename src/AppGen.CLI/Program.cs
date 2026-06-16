using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Engine;
using System.CommandLine;

var root = new RootCommand("AppGen — generate layered ASP.NET Core API solutions");

var createCmd = new Command("create", "Scaffold a new API solution");
var appNameArg = new Argument<string>("name", "Application name (e.g. InventorySystem)");
var outputOpt = new Option<string>("--output", () => ".", "Output directory");
var namespaceOpt = new Option<string?>("--namespace", "Root namespace (defaults to app name)");
var databaseOpt = new Option<string>("--database", () => "SqlServer", "SqlServer, Oracle, or PostgreSql");
var uiOpt = new Option<string?>("--ui", "Optional UI targets: MvcWeb (comma-separated)");

createCmd.AddArgument(appNameArg);
createCmd.AddOption(outputOpt);
createCmd.AddOption(namespaceOpt);
createCmd.AddOption(databaseOpt);
createCmd.AddOption(uiOpt);
createCmd.SetHandler(async (name, output, ns, db, ui) =>
{
    var database = ParseDatabaseProvider(db);
    var uiTargets = ParseUiTargets(ui);
    var setup = NamingHelper.DefaultSetup(database);
    var spec = SpecLoader.CreateDefault(name, ns, database, uiTargets, setup);
    var outputDir = Path.GetFullPath(Path.Combine(output, spec.ApplicationName));
    if (Directory.Exists(outputDir) && Directory.EnumerateFileSystemEntries(outputDir).Any())
    {
        Console.Error.WriteLine($"Output directory is not empty: {outputDir}");
        return;
    }

    var renderer = new TemplateRenderer();
    var generator = new SolutionGenerator(renderer);
    await generator.GenerateAsync(spec, outputDir);
    await AppSettingsGenerator.WriteAsync(spec, outputDir);
    await ReadmeGenerator.WriteAsync(spec, outputDir);
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
}, appNameArg, outputOpt, namespaceOpt, databaseOpt, uiOpt);

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
root.AddCommand(createCmd);
root.AddCommand(entityCmd);

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
