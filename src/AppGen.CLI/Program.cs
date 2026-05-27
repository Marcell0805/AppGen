using AppGen.Core.Models;
using AppGen.Engine;
using System.CommandLine;

var root = new RootCommand("AppGen — generate layered ASP.NET Core API solutions");

var createCmd = new Command("create", "Scaffold a new API solution");
var appNameArg = new Argument<string>("name", "Application name (e.g. InventorySystem)");
var outputOpt = new Option<string>("--output", () => ".", "Output directory");
var namespaceOpt = new Option<string?>("--namespace", "Root namespace (defaults to app name)");
var databaseOpt = new Option<string>("--database", () => "SqlServer", "SqlServer or Oracle");

createCmd.AddArgument(appNameArg);
createCmd.AddOption(outputOpt);
createCmd.AddOption(namespaceOpt);
createCmd.AddOption(databaseOpt);
createCmd.SetHandler(async (name, output, ns, db) =>
{
    var database = Enum.Parse<DatabaseProvider>(db, ignoreCase: true);
    var spec = SpecLoader.CreateDefault(name, ns, database);
    var outputDir = Path.GetFullPath(Path.Combine(output, spec.ApplicationName));
    if (Directory.Exists(outputDir) && Directory.EnumerateFileSystemEntries(outputDir).Any())
    {
        Console.Error.WriteLine($"Output directory is not empty: {outputDir}");
        return;
    }

    var generator = new SolutionGenerator(new TemplateRenderer());
    await generator.GenerateAsync(spec, outputDir);
    Console.WriteLine($"Created solution at: {outputDir}");
    Console.WriteLine($"Next: cd \"{outputDir}\" && dotnet build");
}, appNameArg, outputOpt, namespaceOpt, databaseOpt);

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

    var entity = new EntitySpec
    {
        Name = entityName,
        Properties =
        [
            new PropertySpec { Name = "Id", ClrType = "long", IsKey = true },
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
                new PropertySpec { Name = "Id", ClrType = "long", IsKey = true },
                new PropertySpec { Name = "Name", ClrType = "string" },
                new PropertySpec { Name = "Price", ClrType = "decimal" },
                new PropertySpec { Name = "SupplierId", ClrType = "long" }
            ]
        };
    }

    var generator = new EntityGenerator(new TemplateRenderer());
    await generator.GenerateAsync(spec, entity, projectDir);
    Console.WriteLine($"Added entity '{entityName}' to {projectDir}");
    Console.WriteLine("Run: dotnet build");
}, entityNameArg, projectOpt);

entityCmd.AddCommand(addCmd);
root.AddCommand(createCmd);
root.AddCommand(entityCmd);

return await root.InvokeAsync(args);
