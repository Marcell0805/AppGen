using AppGen.Core;
using AppGen.Core.Branding;
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

        Directory.CreateDirectory(hubDir);
        await ProjectBrandingService.FlushDraftIconToHubAsync(draft, hubDir, ct);
        spec = ReloadSpecWithBranding(spec, hubDir);

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
        string? docDir = null;
        string? webDir = null;
        string? mobileDir = null;

        if (draft.EnableDocumentation)
        {
            ct.ThrowIfCancellationRequested();
            docDir = ProjectOutputPaths.DocumentationDirectory(outputRoot, appName);
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

            docSpec = MobileTargetMerger.ApplyAppThemePreset(
                docSpec,
                spec.Targets?.Mobile.Theme?.Preset ?? draft.MobileThemePreset);
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
            webDir = ProjectOutputPaths.WebDirectory(outputRoot, appName);
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

            var webSpec = new SolutionSpec
            {
                SchemaVersion = spec.SchemaVersion,
                ApplicationName = spec.ApplicationName,
                RootNamespace = string.IsNullOrWhiteSpace(draft.RootNamespace)
                    ? spec.RootNamespace
                    : NamingHelper.NormalizeAppName(draft.RootNamespace.Trim()),
                Project = spec.Project,
                Phase = ProjectPhase.Solution,
                EntitySketches = spec.EntitySketches,
                Targets = spec.Targets,
                Generation = spec.Generation,
                Database = draft.Database,
                UiTargets = uiTargets,
                Setup = setup,
                Entities = entitySpecs
            };

            var overwriteWeb = forceOverwrite || GenerationOutputHelper.OutputDirectoryExists(webDir);
            var webResult = overwriteWeb
                ? await webGenerator.RegenerateFromSpecAsync(webSpec, outputRoot, ct)
                : await webGenerator.GenerateFromSpecAsync(webSpec, outputRoot, ct);

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
            mobileDir = ProjectOutputPaths.MobileDirectory(outputRoot, appName);
            await manifestSave.SaveAsync(spec, mobileDir, ct);

            var loaded = await SpecLoader.LoadAsync(mobileDir, ct);
            loaded = MobileTargetMerger.ApplyWizardMobileSettings(loaded, new MobileTargetSpec
            {
                Enabled = true,
                PackageName = draft.MobilePackageName,
                ApiBaseUrl = draft.MobileApiBaseUrl,
                Theme = new MobileThemeSpec { Preset = draft.MobileThemePreset },
                Offline = draft.BuildMobileOfflineSpec(),
                Capabilities = new MobileCapabilitiesSpec { Enabled = draft.MobileCapabilities.ToList() },
                Publish = draft.BuildMobilePublishSpec()
            });

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
                forceRegenerate: forceOverwrite,
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

        if (ProjectBrandingPaths.HasCustomIcon(hubDir, spec))
        {
            var brandingResult = await ProjectBrandingEmitter.EmitAllAsync(
                spec,
                hubDir,
                new ProjectBrandingTargets
                {
                    DocumentationDirectory = docDir,
                    WebDirectory = webDir,
                    MobileDirectory = mobileDir
                },
                ct);
            if (brandingResult.Emitted)
                messages.Add($"Branding → {brandingResult.Message}");
        }

        var summary = string.Join(" | ", messages);
        return success
            ? ProjectGenerationResult.Ok(hubDir, summary)
            : ProjectGenerationResult.Fail(summary, hubDir);
    }

    public async Task<ProjectGenerationResult> ApplyThemeAsync(
        WizardDraft draft,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(draft.ApplicationName))
            return ProjectGenerationResult.Fail("Application name is required.");

        if (string.IsNullOrWhiteSpace(draft.OutputRoot))
            return ProjectGenerationResult.Fail("Output folder is required.");

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

        if (!File.Exists(Path.Combine(hubDir, "appgen.json")))
        {
            var existing = ProjectOutputPaths.FindManifestDirectory(outputRoot, appName, ProjectOutputLayer.Hub);
            if (existing is null)
                return ProjectGenerationResult.Fail("Generate the project first, then apply a theme.");
            hubDir = existing;
        }

        var wizardState = new WizardStateService();
        wizardState.Update(draft);
        var spec = ProjectInfoSeeder.ApplyToPortalSpec(wizardState.ToSolutionSpec());
        spec = MobileTargetMerger.ApplyAppThemePreset(spec, draft.MobileThemePreset);

        Directory.CreateDirectory(hubDir);
        var saveResult = await manifestSave.SaveAsync(spec, hubDir, ct);
        if (!saveResult.Success)
            return ProjectGenerationResult.Fail(saveResult.Message);

        string? docDir = null;
        string? webDir = null;
        string? mobileDir = null;

        var candidateDoc = ProjectOutputPaths.DocumentationDirectory(outputRoot, appName);
        if (Directory.Exists(Path.Combine(candidateDoc, "portal"))
            || File.Exists(Path.Combine(candidateDoc, "portal", "css", "theme-overrides.css")))
        {
            docDir = candidateDoc;
            await manifestSave.SaveAsync(spec, docDir, ct);
        }

        var candidateWeb = ProjectOutputPaths.WebDirectory(outputRoot, appName);
        if (GenerationOutputHelper.OutputDirectoryExists(candidateWeb))
        {
            webDir = candidateWeb;
            await manifestSave.SaveAsync(spec, webDir, ct);
        }

        var candidateMobile = ProjectOutputPaths.MobileDirectory(outputRoot, appName);
        if (Directory.Exists(Path.Combine(candidateMobile, "lib"))
            || File.Exists(Path.Combine(candidateMobile, "lib", "app", "app_theme_config.dart")))
        {
            mobileDir = candidateMobile;
            await manifestSave.SaveAsync(spec, mobileDir, ct);
        }

        if (docDir is null && webDir is null && mobileDir is null)
            return ProjectGenerationResult.Fail("No generated outputs found yet. Generate Documentation, Web, or Mobile first.", hubDir);

        // Load entities from hub/layer so Flutter theme model has real keys if present.
        try
        {
            var loaded = await SpecLoader.LoadAsync(hubDir, ct);
            if (loaded.Entities.Count > 0)
            {
                spec = new SolutionSpec
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
                    Database = loaded.Database,
                    UiTargets = loaded.UiTargets | spec.UiTargets,
                    Setup = loaded.Setup.ConfigEntries.Count > 0 ? loaded.Setup : spec.Setup,
                    Entities = loaded.Entities
                };
                spec = MobileTargetMerger.ApplyAppThemePreset(spec, draft.MobileThemePreset);
            }
        }
        catch
        {
            // Hub may be incomplete; ThemeEmitter falls back to a placeholder entity.
        }

        var emit = await ThemeEmitter.ApplyAsync(
            spec,
            new ThemeEmitTargets(docDir, webDir, mobileDir),
            ct: ct);

        return emit.Success
            ? ProjectGenerationResult.Ok(hubDir, emit.Message)
            : ProjectGenerationResult.Fail(emit.Message, hubDir);
    }

    private static bool DocumentationOutputExists(string docDir) =>
        File.Exists(Path.Combine(docDir, "portal", "index.html"));

    private static SolutionSpec ReloadSpecWithBranding(
        SolutionSpec spec,
        string hubDir)
    {
        if (!ProjectBrandingPaths.HasCustomIcon(hubDir, spec))
            return spec;

        var project = spec.Project ?? new ProjectInfoSpec();
        return new SolutionSpec
        {
            SchemaVersion = spec.SchemaVersion,
            ApplicationName = spec.ApplicationName,
            RootNamespace = spec.RootNamespace,
            Project = new ProjectInfoSpec
            {
                Tagline = project.Tagline,
                Description = project.Description,
                Branding = new ProjectBrandingSpec
                {
                    IconPath = ProjectBrandingConstants.DefaultIconRelativePath,
                    OriginalFileName = project.Branding?.OriginalFileName
                }
            },
            Phase = spec.Phase,
            Portal = spec.Portal,
            EntitySketches = spec.EntitySketches,
            Targets = spec.Targets,
            Generation = spec.Generation,
            Database = spec.Database,
            UiTargets = spec.UiTargets,
            Setup = spec.Setup,
            Entities = spec.Entities
        };
    }
}

public sealed record ProjectGenerationResult(bool Success, string Message, string? OutputDirectory)
{
    public static ProjectGenerationResult Ok(string outputDirectory, string? message = null) =>
        new(true, message ?? "Generated successfully.", outputDirectory);

    public static ProjectGenerationResult Fail(string message, string? outputDirectory = null) =>
        new(false, message, outputDirectory);
}
