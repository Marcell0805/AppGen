using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Templates;

namespace AppGen.Engine;

public sealed class UiGenerator(TemplateRenderer renderer)
{
    private static readonly (string Template, Func<SolutionSpec, EntitySpec, string> Output)[] BlazorEntityFiles =
    [
        ("Ui/Blazor/EntityListPage.scriban", (s, e) => $"src/{s.WebProject}/Pages/{NamingHelper.ToPlural(e.Name)}/Index.razor"),
        ("Ui/Blazor/EntityEditPage.scriban", (s, e) => $"src/{s.WebProject}/Pages/{NamingHelper.ToPlural(e.Name)}/Edit.razor"),
    ];

    public async Task GenerateAsync(SolutionSpec spec, EntitySpec entity, string projectDirectory, CancellationToken ct = default)
    {
        if (!spec.UiTargets.HasFlag(UiTarget.BlazorWeb))
            return;

        var model = EntityGenerator.BuildEntityModel(spec, entity);

        foreach (var (templatePath, outputPathFunc) in BlazorEntityFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = renderer.Render(TemplateProvider.Load(templatePath), model);
            var fullPath = Path.Combine(projectDirectory, outputPathFunc(spec, entity));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            await File.WriteAllTextAsync(fullPath, content, ct);
        }

        var plural = NamingHelper.ToPlural(entity.Name);
        var href = plural.ToLowerInvariant();
        var navRegion =
            "<!-- <AppGen-Nav-" + entity.Name + "> -->" + Environment.NewLine +
            "        <div class=\"nav-item\">" + Environment.NewLine +
            "            <NavLink class=\"nav-link\" href=\"" + href + "\">" + Environment.NewLine +
            "                " + plural + Environment.NewLine +
            "            </NavLink>" + Environment.NewLine +
            "        </div>" + Environment.NewLine +
            "<!-- </AppGen-Nav-" + entity.Name + "> -->";

        var navPath = Path.Combine(projectDirectory, $"src/{spec.WebProject}/Shared/NavMenu.razor");
        await RegionMergeHelper.MergeRegionAsync(navPath, navRegion,
            "<!-- <AppGen-NavItems> -->", "<!-- </AppGen-NavItems> -->", ct);
    }
}
