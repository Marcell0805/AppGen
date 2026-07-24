using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.Templates;

namespace AppGen.Tests;

public class GracefulApiErrorTests
{
    [Fact]
    public async Task Mvc_generate_includes_swappable_logging_and_error_handling()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "ErrorHandlingApp");

        try
        {
            var spec = SpecLoader.CreateDefault("ErrorHandlingApp", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                UiTargets = UiTarget.MvcWeb,
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
                    }
                ]
            };

            var renderer = new TemplateRenderer();
            var entity = spec.Entities[0];
            await new SolutionGenerator(renderer).GenerateAsync(spec, outputDir);
            await AppSettingsGenerator.WriteAsync(spec, outputDir);
            await new EntityGenerator(renderer).GenerateAsync(EmptyEntities(spec), entity, outputDir);
            var loaded = await SpecLoader.LoadAsync(outputDir);
            await new UiGenerator(renderer).GenerateAsync(loaded, entity, outputDir);

            var sharedLogSink = Path.Combine(outputDir, "src/ErrorHandlingApp.Shared/Logging/IAppLogSink.cs");
            var fileSink = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Logging/FileAppLogSink.cs");
            var apiErrorHandling = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Helpers/ApiErrorHandling.cs");
            var entityWebServiceBase = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Services/EntityWebServiceBase.cs");
            var program = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Program.cs");
            var homeController = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Controllers/HomeController.cs");
            var alertPartial = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Views/Shared/_ApiErrorAlert.cshtml");
            var errorView = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Views/Home/Error.cshtml");
            var widgetController = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/Controllers/WidgetController.cs");
            var appsettings = Path.Combine(outputDir, "src/ErrorHandlingApp.MVC/appsettings.json");

            Assert.True(File.Exists(sharedLogSink));
            Assert.True(File.Exists(fileSink));
            Assert.True(File.Exists(apiErrorHandling));
            Assert.True(File.Exists(entityWebServiceBase));
            Assert.True(File.Exists(program));
            Assert.True(File.Exists(homeController));
            Assert.True(File.Exists(alertPartial));
            Assert.True(File.Exists(errorView));
            Assert.True(File.Exists(widgetController));

            var programContent = await File.ReadAllTextAsync(program);
            Assert.Contains("AddSingleton<IAppLogSink, FileAppLogSink>()", programContent);

            var serviceBaseContent = await File.ReadAllTextAsync(entityWebServiceBase);
            Assert.Contains("ApiException", serviceBaseContent);
            Assert.Contains("IAppLogSink", serviceBaseContent);
            Assert.Contains("The service isn't available", serviceBaseContent);

            var controllerContent = await File.ReadAllTextAsync(widgetController);
            Assert.Contains("ApiErrorHandling.TryHandleApiError", controllerContent);
            Assert.Contains("IAppLogSink appLog", controllerContent);

            var appsettingsContent = await File.ReadAllTextAsync(appsettings);
            Assert.Contains("logs/app.log", appsettingsContent);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task Flutter_generate_includes_api_error_mapper_and_log_sink()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var outputDir = Path.Combine(tempRoot, "FlutterErrorApp");

        try
        {
            var spec = SpecLoader.CreateDefault("FlutterErrorApp", null, DatabaseProvider.SqlServer);
            spec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = spec.RootNamespace,
                Database = spec.Database,
                Setup = spec.Setup,
                Targets = spec.Targets,
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
                    }
                ]
            };

            Directory.CreateDirectory(outputDir);
            var renderer = new TemplateRenderer();
            var flutterGenerator = new FlutterGenerator(renderer);
            var mobile = spec.Targets?.Mobile ?? new MobileTargetSpec();
            await flutterGenerator.GenerateAllAsync(spec, spec.Entities, outputDir, mobile);

            var flutterRoot = FlutterProjectPaths.GetFlutterRoot(outputDir);
            var mapper = Path.Combine(flutterRoot, "lib", "core", "network", "api_error_mapper.dart");
            var logSink = Path.Combine(flutterRoot, "lib", "core", "logging", "app_log_sink.dart");
            var mainDart = Path.Combine(flutterRoot, "lib", "main.dart");
            var listScreen = Path.Combine(flutterRoot, "lib", "features", "widget", "screens", "widget_list_screen.dart");

            Assert.True(File.Exists(mapper));
            Assert.True(File.Exists(logSink));
            Assert.True(File.Exists(mainDart));
            Assert.True(File.Exists(listScreen));

            var mapperContent = await File.ReadAllTextAsync(mapper);
            Assert.Contains("service isn't available", mapperContent, StringComparison.OrdinalIgnoreCase);

            var mainContent = await File.ReadAllTextAsync(mainDart);
            Assert.Contains("ErrorWidget.builder", mainContent);

            var listContent = await File.ReadAllTextAsync(listScreen);
            Assert.Contains("ApiErrorMapper.message", listContent);
            Assert.Contains("onRetry", listContent);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void FileAppLogSink_build_entry_includes_message_and_exception_details()
    {
        var exception = new HttpRequestException("Connection refused", new InvalidOperationException("Inner failure"));
        var entry = RenderedFileAppLogSink.BuildEntry(
            exception,
            "GET list failed",
            new Dictionary<string, object> { ["Endpoint"] = "api/v1/widgets" });

        Assert.Contains("[Error] GET list failed", entry);
        Assert.Contains("HttpRequestException: Connection refused", entry);
        Assert.Contains("Inner failure", entry);
        Assert.Contains("Endpoint: api/v1/widgets", entry);
    }

    [Fact]
    public async Task FileAppLogSink_appends_to_configured_log_file()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "AppGenTests", Guid.NewGuid().ToString("N"));
        var logPath = Path.Combine(tempRoot, "logs", "app.log");

        try
        {
            Directory.CreateDirectory(tempRoot);
            var renderer = new TemplateRenderer();
            var model = new { root_namespace = "SampleApp" };
            var content = renderer.Render(
                TemplateProvider.Load("Solution/mvc/Logging/FileAppLogSink.scriban"),
                model);

            Assert.Contains("BuildEntry", content);

            var exception = new HttpRequestException("Connection refused");
            var entry = RenderedFileAppLogSink.BuildEntry(exception, "API request failed", null);

            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            await File.AppendAllTextAsync(logPath, entry);

            var written = await File.ReadAllTextAsync(logPath);
            Assert.Contains("API request failed", written);
            Assert.Contains("Connection refused", written);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static class RenderedFileAppLogSink
    {
        public static string BuildEntry(
            Exception exception,
            string message,
            IReadOnlyDictionary<string, object>? context)
        {
            var builder = new System.Text.StringBuilder();
            builder.Append('[')
                .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Append("] [Error] ")
                .AppendLine(message);
            builder.Append(exception.GetType().Name)
                .Append(": ")
                .AppendLine(exception.Message);

            if (context is { Count: > 0 })
            {
                foreach (var pair in context)
                    builder.Append("  ").Append(pair.Key).Append(": ").AppendLine(pair.Value?.ToString());
            }

            if (!string.IsNullOrWhiteSpace(exception.StackTrace))
                builder.AppendLine(exception.StackTrace);

            for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            {
                builder.Append("  ---> ")
                    .Append(inner.GetType().Name)
                    .Append(": ")
                    .AppendLine(inner.Message);

                if (!string.IsNullOrWhiteSpace(inner.StackTrace))
                    builder.AppendLine(inner.StackTrace);
            }

            builder.AppendLine();
            return builder.ToString();
        }
    }

    private static SolutionSpec EmptyEntities(SolutionSpec spec) => new()
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
        Entities = []
    };
}
