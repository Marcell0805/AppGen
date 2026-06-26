using AppGen.Core;
using AppGen.Core.Models;
using AppGen.Engine;
using AppGen.UI.Models;

namespace AppGen.UI.Services;

public sealed class ProjectGenerationService(
    ManifestSaveService manifestSave,
    DocumentationApplicationGenerator documentationGenerator,
    AppGenerationService webGenerator,
    MobileGenerationService mobileGenerator)
{
    public async Task<ProjectGenerationResult> GenerateAllAsync(
        WizardDraft draft,
        bool forceOverwrite = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draft.ApplicationName))
            return ProjectGenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(draft.OutputRoot))
            return ProjectGenerationResult.Fail("Output folder is required.");

        if (!draft.EnableDocumentation && !draft.EnableWeb && !draft.EnableMobile)
            return ProjectGenerationResult.Fail("Enable at least one layer: Documentation, Web, or Mobile.");

        var entities = WizardEntityHelper.ToEntityDrafts(draft.Entities);
        var validationError = EntityValidation.ValidateEntities(entities);
        if (validationError is not null)
            return ProjectGenerationResult.Fail(validationError);

        if (entities.Count == 0)
            return ProjectGenerationResult.Fail("Add at least one entity before generating.");

        var appName = draft.ApplicationName.Trim();
        var outputRoot = draft.OutputRoot.Trim();
        string hubDir;
        try
        {
            hubDir = ProjectOutputPaths.HubDirectory(outputRoot, appName);
        }
        catch
        {
            return ProjectGenerationResult.Fail("Output folder is invalid.");
        }

        var wizardState = new WizardStateService();
        wizardState.Update(draft);
        var spec = ProjectInfoSeeder.ApplyToPortalSpec(wizardState.ToSolutionSpec());

        var saveResult = await manifestSave.SaveAsync(spec, hubDir, ct);
        if (!saveResult.Success)
            return ProjectGenerationResult.Fail(saveResult.Message);

        await ReadmeGenerator.WriteHubAsync(new ReadmeContext(
            spec,
            hubDir,
            outputRoot,
            draft.MobileApiBaseUrl,
            draft.EnableDocumentation,
            draft.EnableWeb,
            draft.EnableMobile), ct);

        var messages = new List<string> { $"Manifest → {hubDir}" };
        var success = true;

        if (draft.EnableDocumentation)
        {
            ct.ThrowIfCancellationRequested();
            var docDir = ProjectOutputPaths.DocumentationDirectory(outputRoot, appName);
            await manifestSave.SaveAsync(spec, docDir, ct);

            var loaded = await SpecLoader.LoadAsync(docDir, ct);
            var docSpec = loaded.Portal is not null
                ? loaded
                : new SolutionSpec
                {
                    SchemaVersion = loaded.SchemaVersion,
                    ApplicationName = loaded.ApplicationName,
                    RootNamespace = loaded.RootNamespace,
                    Project = loaded.Project ?? spec.Project,
                    Phase = loaded.Phase,
                    Portal = spec.Portal,
                    EntitySketches = loaded.EntitySketches.Count > 0 ? loaded.EntitySketches : spec.EntitySketches,
                    Targets = loaded.Targets ?? spec.Targets,
                    Generation = loaded.Generation ?? spec.Generation,
                    Database = loaded.Database,
                    UiTargets = loaded.UiTargets,
                    Setup = loaded.Setup,
                    Entities = loaded.Entities
                };

            docSpec = ProjectInfoSeeder.ApplyToPortalSpec(docSpec);

            if (docSpec.Portal is null)
            {
                success = false;
                messages.Add("Documentation: portal configuration is missing.");
            }
            else
            {
                var overwriteDoc = forceOverwrite || DocumentationOutputExists(docDir);
                var docResult = await documentationGenerator.GenerateAsync(
                    docSpec,
                    docDir,
                    new GeneratorOptions { Force = overwriteDoc },
                    ct);
                if (docResult.Success)
                    await ReadmeGenerator.WriteDocumentationAsync(new ReadmeContext(docSpec, docDir), ct);
                messages.Add(docResult.Success
                    ? $"Documentation → {docDir}"
                    : $"Documentation failed: {docResult.Message}");
                success &= docResult.Success;
            }
        }

        if (draft.EnableWeb)
        {
            ct.ThrowIfCancellationRequested();
            var webDir = ProjectOutputPaths.WebDirectory(outputRoot, appName);
            await manifestSave.SaveAsync(spec, webDir, ct);

            var uiTargets = draft.IncludeMvcWeb ? UiTarget.MvcWeb : UiTarget.None;
            var entitySpecs = entities.Select(e => e.ToSpec(entities.Select(x => x.Name).ToList())).ToList();
            var setup = new ProjectSetupSpec
            {
                ActiveConnectionName = draft.ActiveConnectionName,
                EnsureCreatedInDevelopment = draft.EnsureCreatedInDevelopment,
                OracleSchemaPrefix = draft.OracleSchemaPrefix,
                ConfigEntries = draft.ConfigEntries
                    .Select(c => new ConfigEntrySpec
                    {
                        Name = c.Name,
                        Kind = c.Kind,
                        Key = c.Key,
                        Value = c.Value
                    })
                    .ToList()
            };

            var overwriteWeb = forceOverwrite || GenerationOutputHelper.OutputDirectoryExists(webDir);
            var webResult = overwriteWeb
                ? await webGenerator.RegenerateAsync(
                    appName,
                    string.IsNullOrWhiteSpace(draft.RootNamespace) ? null : draft.RootNamespace.Trim(),
                    draft.Database,
                    uiTargets,
                    outputRoot,
                    setup,
                    entitySpecs,
                    ct)
                : await webGenerator.GenerateAsync(
                    appName,
                    string.IsNullOrWhiteSpace(draft.RootNamespace) ? null : draft.RootNamespace.Trim(),
                    draft.Database,
                    uiTargets,
                    outputRoot,
                    setup,
                    entitySpecs,
                    ct: ct);

            messages.Add(webResult.Success
                ? $"Web → {webDir}"
                : $"Web failed: {webResult.Message}");
            success &= webResult.Success;
            if (webResult.Success)
            {
                var webLoaded = await SpecLoader.LoadAsync(webDir, ct);
                await ReadmeGenerator.WriteWebAsync(new ReadmeContext(webLoaded, webDir, EnableWeb: true), ct);
            }
        }

        if (draft.EnableMobile)
        {
            ct.ThrowIfCancellationRequested();
            var mobileDir = ProjectOutputPaths.MobileDirectory(outputRoot, appName);
            await manifestSave.SaveAsync(spec, mobileDir, ct);

            var loaded = await SpecLoader.LoadAsync(mobileDir, ct);
            var packageName = string.IsNullOrWhiteSpace(draft.MobilePackageName)
                ? $"com.{loaded.ApplicationName.ToLowerInvariant()}.app"
                : draft.MobilePackageName.Trim();
            var entityNames = entities.Select(e => e.Name).ToList();

            var mobileResult = await mobileGenerator.GenerateAsync(
                loaded,
                outputRoot,
                entityNames,
                packageName,
                draft.MobileApiBaseUrl,
                ct);

            messages.Add(mobileResult.Success
                ? $"Mobile → {mobileDir}"
                : $"Mobile failed: {mobileResult.Message}");
            success &= mobileResult.Success;
            if (mobileResult.Success)
            {
                var mobileLoaded = await SpecLoader.LoadAsync(mobileDir, ct);
                await ReadmeGenerator.WriteMobileAsync(new ReadmeContext(
                    mobileLoaded,
                    mobileDir,
                    ApiBaseUrl: draft.MobileApiBaseUrl,
                    EnableMobile: true), ct);
            }
        }

        var summary = string.Join(" | ", messages);
        return success
            ? ProjectGenerationResult.Ok(hubDir, summary)
            : ProjectGenerationResult.Fail(summary, hubDir);
    }

    private static bool DocumentationOutputExists(string docDir) =>
        File.Exists(Path.Combine(docDir, "portal", "index.html"));
}

public sealed record ProjectGenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static ProjectGenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Generated successfully.", outputDirectory);

    public static ProjectGenerationResult Fail(string message, string? outputDirectory = null) =>
        new(false, message, outputDirectory);
}
