using AppGen.Core.Models;
using AppGen.Engine;
using Xunit.Abstractions;

namespace AppGen.Tests;

public class MobileIntegrationTests
{
  [Fact]
  public async Task Mobile_generate_produces_flutter_tree_for_all_ui_entities()
  {
    var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
    var outputDir = Path.Combine(tempRoot, "MobileApp");

    try
    {
      var spec = SpecLoader.CreateDefault("MobileApp", null, DatabaseProvider.SqlServer);
      spec = new SolutionSpec
      {
        SchemaVersion = spec.SchemaVersion,
        ApplicationName = spec.ApplicationName,
        RootNamespace = spec.RootNamespace,
        Database = spec.Database,
        Setup = spec.Setup,
        Entities =
        [
          new EntitySpec
          {
            Name = "Widget",
            IncludeInUi = true,
            Properties =
            [
              new PropertySpec { Name = "Widget_Id", ClrType = "long", IsKey = true },
              new PropertySpec { Name = "Name", ClrType = "string" }
            ]
          },
          new EntitySpec
          {
            Name = "Gadget",
            IncludeInUi = true,
            Properties =
            [
              new PropertySpec { Name = "Gadget_Id", ClrType = "long", IsKey = true },
              new PropertySpec { Name = "Name", ClrType = "string" }
            ]
          }
        ]
      };

      Directory.CreateDirectory(outputDir);
      await ProjectSpecWriter.WriteAsync(spec, outputDir);

      var renderer = new TemplateRenderer();
      var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
      var result = await generator.GenerateAsync(spec, outputDir, new GeneratorOptions());

      Assert.True(result.Success, result.Message);
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "models", "widget_model.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "gadget", "models", "gadget_model.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "screens", "widget_detail_screen.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "screens", "widget_form_screen.dart")));

      var widgetService = await File.ReadAllTextAsync(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "services", "widget_service.dart"));
      Assert.Contains("Future<WidgetModel> create", widgetService);
      Assert.Contains("Future<WidgetModel> update", widgetService);
      Assert.Contains("Future<void> delete", widgetService);

      var widgetModel = await File.ReadAllTextAsync(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "models", "widget_model.dart"));
      Assert.Contains("toWriteJson", widgetModel);

      var router = await File.ReadAllTextAsync(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "app", "router.dart"));
      Assert.Contains("/widget", router);
      Assert.Contains("/gadget", router);
      Assert.Contains("WidgetDetailScreen", router);
      Assert.Contains("WidgetFormScreen", router);
      Assert.Contains("path: 'new'", router);
      Assert.Contains("path: 'edit'", router);

      var reloaded = await SpecLoader.LoadAsync(outputDir);
      Assert.Equal(["Widget", "Gadget"], reloaded.Generation!.Mobile!.Entities);
    }
    finally
    {
      if (Directory.Exists(tempRoot))
        Directory.Delete(tempRoot, recursive: true);
    }
  }

  [Fact]
  public async Task Mobile_generate_produces_flutter_tree_for_entity()
  {
    var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
    var outputDir = Path.Combine(tempRoot, "MobileApp");

    try
    {
      var spec = SpecLoader.CreateDefault("MobileApp", null, DatabaseProvider.SqlServer);
      spec = new SolutionSpec
      {
        SchemaVersion = spec.SchemaVersion,
        ApplicationName = spec.ApplicationName,
        RootNamespace = spec.RootNamespace,
        Database = spec.Database,
        Setup = spec.Setup,
        Entities =
        [
          new EntitySpec
          {
            Name = "Widget",
            Properties =
            [
              new PropertySpec { Name = "Widget_Id", ClrType = "long", IsKey = true },
              new PropertySpec { Name = "Name", ClrType = "string" }
            ]
          }
        ]
      };

      Directory.CreateDirectory(outputDir);
      await ProjectSpecWriter.WriteAsync(spec, outputDir);

      var renderer = new TemplateRenderer();
      var generator = new MobileApplicationGenerator(new FlutterGenerator(renderer));
      var result = await generator.GenerateAsync(
        spec,
        outputDir,
        new GeneratorOptions { EntityName = "Widget" });

      Assert.True(result.Success, result.Message);
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "pubspec.yaml")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "main.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "models", "widget_model.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "screens", "widget_detail_screen.dart")));
      Assert.True(File.Exists(Path.Combine(FlutterProjectPaths.GetFlutterRoot(outputDir), "lib", "features", "widget", "screens", "widget_form_screen.dart")));
      Assert.Contains("Flutter CRUD", result.Message);

      var reloaded = await SpecLoader.LoadAsync(outputDir);
      Assert.Equal(SolutionSpec.CurrentSchemaVersion, reloaded.SchemaVersion);
      Assert.NotNull(reloaded.Targets);
      Assert.True(reloaded.Targets!.Mobile.Enabled);
    }
    finally
    {
      if (Directory.Exists(tempRoot))
        Directory.Delete(tempRoot, recursive: true);
    }
  }

  [Fact]
  public async Task V5_manifest_round_trips_targets()
  {
    var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));

    try
    {
      Directory.CreateDirectory(tempRoot);
      var spec = new SolutionSpec
      {
        SchemaVersion = SolutionSpec.CurrentSchemaVersion,
        ApplicationName = "TargetApp",
        RootNamespace = "TargetApp",
        Targets = new ApplicationTargets
        {
          Documentation = new DocumentationTargetSpec { Enabled = true },
          Web = new WebTargetSpec { Enabled = true },
          Mobile = new MobileTargetSpec
          {
            Enabled = true,
            PackageName = "com.target.app",
            ApiBaseUrl = "https://localhost:5001"
          }
        },
        Entities = []
      };

      await ProjectSpecWriter.WriteAsync(spec, tempRoot);
      var loaded = await SpecLoader.LoadAsync(tempRoot);
      Assert.True(loaded.Targets!.Mobile.Enabled);
      Assert.Equal("com.target.app", loaded.Targets.Mobile.PackageName);
    }
    finally
    {
      if (Directory.Exists(tempRoot))
        Directory.Delete(tempRoot, recursive: true);
    }
  }
}
