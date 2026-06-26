using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class UiGenerator(TemplateRenderer renderer)
{
    private static readonly (string Template, Func<SolutionSpec, EntitySpec, string> Output)[] MvcEntityFiles =
    [
        ("Ui/Mvc/EntityService.scriban", (s, e) => $"src/{s.MvcProject}/Services/{e.Name}Service.cs"),
        ("Ui/Mvc/EntityController.scriban", (s, e) => $"src/{s.MvcProject}/Controllers/{e.Name}Controller.cs"),
        ("Ui/Mvc/Views/EntityIndex.scriban", (s, e) => $"src/{s.MvcProject}/Views/{e.Name}/Index.cshtml"),
        ("Ui/Mvc/Views/EntityEdit.scriban", (s, e) => $"src/{s.MvcProject}/Views/{e.Name}/Edit.cshtml"),
    ];

    public async Task GenerateAsync(SolutionSpec spec, EntitySpec entity, string projectDirectory, CancellationToken ct = default)
    {
        if (!spec.UiTargets.HasFlag(UiTarget.MvcWeb) || !entity.IncludeInUi)
            return;

        var model = EntityGenerator.BuildEntityModel(spec, entity);

        foreach (var (templatePath, outputPathFunc) in MvcEntityFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = renderer.Render(TemplateProvider.Load(templatePath), model);
            var fullPath = Path.Combine(projectDirectory, outputPathFunc(spec, entity));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content, ct);
        }

        var programPath = Path.Combine(projectDirectory, $"src/{spec.MvcProject}/Program.cs");
        var serviceRegion = RegionMergeHelper.BuildRegion(
            $"WebService-{entity.Name}",
            $"builder.Services.AddScoped<I{entity.Name}Service, {entity.Name}Service>();");
        await RegionMergeHelper.MergeRegionAsync(programPath, serviceRegion,
            "// <AppGen-WebServices>", "// </AppGen-WebServices>", ct);

        var navRegion =
            "<!-- <AppGen-Nav-" + entity.Name + "> -->" + Environment.NewLine +
            "                <a class=\"nav-link\" asp-controller=\"" + entity.Name + "\" asp-action=\"Index\">" + entity.Name + "</a>" + Environment.NewLine +
            "<!-- </AppGen-Nav-" + entity.Name + "> -->";

        var layoutPath = Path.Combine(projectDirectory, $"src/{spec.MvcProject}/Views/Shared/_Layout.cshtml");
        await RegionMergeHelper.MergeRegionAsync(layoutPath, navRegion,
            "<!-- <AppGen-NavItems> -->", "<!-- </AppGen-NavItems> -->", ct);
    }
}
