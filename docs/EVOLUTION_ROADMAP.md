# AppGen Evolution — Feedback and Thin-Slice Roadmap

## Implementation status (2026-06-25)

Thin-slice work is **complete**. Phase 2 (shared Project tab) and Phase 3 (Flutter CRUD) are **done**. Use this section as the changelog for what landed in the repo.

| Area | Status | Notes |
|------|--------|-------|
| **0. Roadmap in repo** | Done | This file + README link |
| **A. Nav rename** | Done | Documentation (`/documentation`, `/portal` redirect), Web (`/web`), Mobile (`/mobile`), **Project** (`/`, `/project`) |
| **A2. UI branding** | Done | Inter font, slate sidebar, CSS tokens, logo/favicon, `PageHeader`, stacked-layers icon. **AppGen** is the product name; **Application factory** is the tagline under the logo (not a replacement title) |
| **B. Schema v5** | Done | `ApplicationTargets`, `GenerationMetadata`, `SpecNormalizer`, v3–v4 load compat |
| **C. Generator plugins** | Done | `IApplicationGenerator`; `DocumentationApplicationGenerator`, `MobileApplicationGenerator` |
| **D. Flutter CRUD** | Done | `FlutterGenerator` + Scriban templates — list, detail, create/edit form, delete per entity; `go_router` nested routes |
| **E. Mobile tab UI** | Done | Entity picker, package name, API URL, Generate / Load project |
| **F. CLI** | Done | `appgen mobile create --project <path> [--entity X] [--force]` |
| **G. Tests** | Done | Portal, mobile CRUD, v5 round-trip, v3 normalize, generation output paths |
| **H. Shared entity workflow** | Done | `WizardStateService` (Web ↔ Mobile session state), **Save manifest** (`appgen.json` without full API generate), **Load draft** on Mobile |
| **I. Project tab (Phase 2)** | Done | `Project.razor` hub, `ProjectWorkspace` shared entity editor, layer toggles, **Generate all**, per-layer output folders (`{AppName} Doc/Web/Mobile`) |
| **J. Project metadata + READMEs + tab help** | Done | Schema v6 `project` (tagline, description), per-layer READMEs, **?** help drawer on Project/Documentation/Web/Mobile |

### Shared entity workflow (Phase 2)

Web, Mobile, and **Project** tabs share entity definitions via `WizardStateService`:

1. Define entities on **Project** (or Web) → enable Documentation / Web / Mobile layers → **Generate all**, or
2. Click **Save manifest** → writes `appgen.json` to the hub folder (`output/{AppName}/`), or
3. **Save draft** / **Load draft** (JSON file) — available on Project, Web, and Mobile

**Layer output folders** (avoids overwrite clashes when generating multiple targets):

| Layer | Folder |
|-------|--------|
| Hub (manifest) | `output/{AppName}/` |
| Documentation | `output/{AppName} Doc/` |
| Web | `output/{AppName} Web/` |
| Mobile | `output/{AppName} Mobile/` |

**Not yet done** (see long-term phases below): per-target incremental sync, OpenAPI client gen.

**Phase 4 (done):** JWT auth scaffold (Web toggle + mobile login), optional SQLite offline cache (independent Mobile toggle), generated API/MVC xUnit integration tests with EF InMemory.

---

## Overall assessment

Your vision is **directionally right** and **already partially built**. The portal-to-API bridge you shipped (`appgen.json` v4, `entitySketches`, `ProjectPromoter`) is the seed of the shared Application Definition. The proposalâ€™s main job now is to **formalize that model** and **add generator plugins** â€” not restart from scratch.

What you have today in [`SolutionSpec`](src\AppGen.Core\Models\SolutionSpec.cs):

```mermaid
flowchart TB
    subgraph today [Today - appgen.json v4]
        Manifest[SolutionSpec]
        Manifest --> Portal[PortalSpec]
        Manifest --> Sketches[entitySketches]
        Manifest --> Entities[entities]
        Manifest --> WebCfg[database setup uiTargets]
    end

    subgraph targets [Target generators]
        DocGen[DocumentationGenerator]
        WebGen[WebGenerator]
        MobileGen[MobileGenerator]
    end

    Manifest --> DocGen
    Manifest --> WebGen
    Manifest --> MobileGen
```

---

## Suggestions on your proposal

### 1. Naming: Documentation / Web / Mobile (your choice)

Good pick. Short, clear, and matches the â€œapplication factoryâ€ story.

| Tab | Generates | Note |
|-----|-----------|------|
| **Documentation** | `portal/` static site | Rename from Portal; keep preset system |
| **Web** | `src/` .NET solution | Rename from API; subtitle should say â€œAPI + layers + optional MVCâ€ |
| **Mobile** | `mobile/flutter/` (new) | New; framework selector comes later |

Avoid calling the middle tab just â€œAPIâ€ internally going forward â€” it undersells MVC and future SignalR/Identity additions.

### 2. Donâ€™t wait for perfection before Mobile â€” but **do** formalize the shared model first

Your Flutter section assumes relationships, validation, auth, and known endpoints. AppGen today has:

- Entities + FK properties (partial relationships)
- Swagger on generated APIs
- **Deferred**: full relationships, validation, pagination ([README](README.md))

**Recommendation:** Mobile v0 should generate from **`EntitySpec` + convention-based routes** (same pattern as MVC/API templates), not full OpenAPI parsing yet. Add OpenAPI-driven Dart clients in phase 2 once the contract is stable.

### 3. UI structure: shared definition + layer tabs

Your long-term â€œchoose layers then define entitiesâ€ workflow is right, but **entities should not live inside each tab**. Proposed UX:

```
Project (shared)
  - App name, output folder
  - Entities + properties (single editor)
  - Layer toggles: Documentation | Web | Mobile

Documentation tab  â†’ portal-only options (sections, theme, password)
Web tab            â†’ database, MVC, connection strings (todayâ€™s Index.razor)
Mobile tab         â†’ package name, Riverpod, API base URL
```

This prevents the drift you already felt between portal sketches and API entities.

### 4. Rename `SolutionSpec` gradually, not abruptly

Avoid a big-bang rename that breaks existing `appgen.json` files. Evolve schema **v5**:

```json
{
  "schemaVersion": 5,
  "applicationName": "DeltaCore",
  "targets": {
    "documentation": { "enabled": true, "preset": "engineering-portal", ... },
    "web": { "enabled": true, "database": "PostgreSql", "uiTargets": ["MvcWeb"], ... },
    "mobile": { "enabled": false, "framework": "flutter", ... }
  },
  "entities": [...],
  "entitySketches": [...]
}
```

Keep v4 deserializer mapping `portal` â†’ `targets.documentation` and top-level web fields â†’ `targets.web`.

### 5. Generator plugin architecture (small interface, big payoff)

Extract a common pattern from existing [`PortalGenerator`](src\AppGen.Engine\PortalGenerator.cs), [`SolutionGenerator`](src\AppGen.Engine\SolutionGenerator.cs), and future `FlutterGenerator`:

```csharp
interface IApplicationGenerator
{
    string TargetId { get; }           // "documentation" | "web" | "mobile"
    Task<GenerationResult> GenerateAsync(ApplicationDefinition spec, string projectDir, GeneratorOptions options, CancellationToken ct);
}
```

Each generator:
- Reads the **same** `entities` list
- Writes only its output subtree (`portal/`, `src/`, `mobile/flutter/`)
- Never deletes sibling outputs unless `--force` scoped to that target

This enables your future â€œUpdate Mobile onlyâ€ sync story.

### 6. Flutter scope â€” phase realistically

| Phase | Flutter output | Skip for now |
|-------|----------------|--------------|
| **POC (done)** | `pubspec.yaml`, folder structure, entity model, Dio service, Riverpod provider, list screen, `main.dart` + routes | — |
| **v1 (done — Phase 3)** | Full CRUD screens per entity, `go_router` routes, theme scaffold | Isar, biometrics |
| **v2** | JWT login flow, secure storage, interceptors | MAUI, React Native |

Default stack for POC (sensible, popular, matches your proposal):

- **Riverpod** + **go_router** + **dio**
- Material 3 theme stub with brand colors from portal theme (optional link)

### 7. API integration strategy

**POC:** Template-generated Dart from entity name + property types, mirroring API controller routes:

```
GET    /api/v1/{Entity}
GET    /api/v1/{Entity}/{id}
POST   /api/v1/{Entity}
PUT    /api/v1/{Entity}/{id}
DELETE /api/v1/{Entity}/{id}
```

**Later:** Emit `openapi.json` path in manifest; optional `dart run build_runner` OpenAPI client gen.

### 8. Sync / â€œUpdate Mobileâ€ â€” design now, build later

Store per-target generation metadata in manifest:

```json
"generation": {
  "mobile": {
    "lastGenerated": "2026-06-25",
    "entities": ["Product"]
  }
}
```

Enables future incremental entity add without full regen. Do **not** implement sync in the thin slice â€” just leave the manifest hook.

### 9. What NOT to do yet

- Multiple mobile frameworks in one release
- Full relationship graph UI
- Blazor/React/Angular generators
- IaC / GitHub Actions (unless Documentation deploy workflow only)
- Replacing separate tabs with one mega-wizard (until shared entity editor exists)

---

## Thin-slice implementation plan (your chosen next phase)

### 0. Roadmap document in source control (do first)

Keep the evolution plan in the **AppGen repo** so it survives outside Cursor and is versioned with the code.

| Item | Detail |
|------|--------|
| **Source** | This plan file (`.cursor/plans/appgen_evolution_roadmap_7a307fde.plan.md`) |
| **Target path** | [`AppGen/docs/EVOLUTION_ROADMAP.md`](docs\EVOLUTION_ROADMAP.md) |
| **Format** | Markdown body only â€” omit Cursor frontmatter (`todos`, `isProject`, etc.) |
| **README link** | Add a short "Evolution roadmap" section in [`AppGen/README.md`](README.md) pointing to `docs/EVOLUTION_ROADMAP.md` |
| **Git** | Commit to `AppGen` on `dev-main` (or your working branch) as a standalone docs commit before or alongside phase 1 work |

**Maintenance rule:** When the roadmap changes, update `docs/EVOLUTION_ROADMAP.md` in the repo â€” treat it as the canonical copy; the Cursor plan file is optional working notes.

Optional later: add `docs/ARCHITECTURE.md` for generator plugin design once `IApplicationGenerator` lands.

### A. Navigation rename ✅

Update [`NavMenu.razor`](src/AppGen.UI/Shared/NavMenu.razor):

- `Portal` → **Documentation** (routes `/documentation`; `/portal` redirects for bookmarks)
- `API` → **Web** (routes `/` and `/web`)
- **Mobile** nav item → `/mobile`

Update page titles/subtitles in [`Portal.razor`](src/AppGen.UI/Pages/Portal.razor) and [`Index.razor`](src/AppGen.UI/Pages/Index.razor).

### A2. UI branding and professional styling ✅

Today the UI is still the **default Blazor Server template**: generic purple gradient sidebar ([`MainLayout.razor.css`](src\AppGen.UI\Shared\MainLayout.razor.css)), stock Bootstrap blues, Open Iconic glyphs, no favicon. For a custom internal tool that will grow into an "Application Factory", a cohesive brand pays off â€” especially when demoing to others.

**Design direction:** clean developer-tool aesthetic â€” not flashy, not corporate-bland. Think VS Code / Linear / Vercel dashboard: dark sidebar, light content area, one strong accent, generous whitespace.

**Proposed AppGen palette** (distinct from DeltaCore portal, but similarly professional):

| Token | Value | Use |
|-------|-------|-----|
| `--ag-sidebar` | `#0f172a` | Sidebar background (slate) |
| `--ag-sidebar-accent` | `#1e293b` | Hover / active nav |
| `--ag-accent` | `#3b82f6` | Primary buttons, active links |
| `--ag-accent-muted` | `#60a5fa` | Focus rings, subtle highlights |
| `--ag-surface` | `#ffffff` | Main content |
| `--ag-bg` | `#f8fafc` | Page background |
| `--ag-border` | `#e2e8f0` | Panels, inputs |
| `--ag-text` | `#0f172a` | Headings |
| `--ag-text-muted` | `#64748b` | Subtitles, hints |

**Typography:** switch from Helvetica to **Inter** (already used in generated documentation portals) via Google Fonts in [`_Layout.cshtml`](src\AppGen.UI\Pages\_Layout.cshtml).

**Custom icon concept** â€” "stacked application layers" (matches Application Factory story):

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚  Doc    â”‚  â† top layer (documentation)
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚  Web    â”‚  â† middle layer (backend/MVC)
â”œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”¤
â”‚ Mobile  â”‚  â† bottom layer (Flutter)
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

Simplified as a compact SVG mark: three offset rounded rectangles in accent blue, or a single hexagon with three horizontal bars. Works at 16px (favicon) and 32px (sidebar).

**Assets to add** under `wwwroot/`:

| File | Purpose |
|------|---------|
| `assets/logo.svg` | Sidebar brand mark + wordmark "AppGen" |
| `assets/logo-icon.svg` | Icon only (collapsed sidebar future) |
| `favicon.svg` | Modern browsers |
| `favicon.ico` | Fallback |

**Layout changes:**

```mermaid
flowchart LR
    subgraph shell [AppGen Shell]
        Sidebar[Sidebar_logo_plus_nav]
        Main[Main_content]
    end
    Sidebar --> DocNav[Documentation]
    Sidebar --> WebNav[Web]
    Sidebar --> MobileNav[Mobile]
    Main --> PageHeader[Page_title_and_subtitle]
    Main --> Wizard[Generator_wizard]
```

- [`NavMenu.razor`](src/AppGen.UI/Shared/NavMenu.razor) — logo + **AppGen** wordmark with **Application factory** tagline beneath; nav items with inline SVG icons
- [`MainLayout.razor`](src\AppGen.UI\Shared\MainLayout.razor) â€” optional slim top bar on main area showing current generator tab
- New shared component `PageHeader.razor` â€” title, subtitle, optional status badge (e.g. "Portal phase" when loaded from manifest)
- [`site.css`](src\AppGen.UI\wwwroot\css\site.css) â€” CSS variables, refined `.panel` (subtle shadow, 8px radius), button hierarchy, improved status panels
- Remove purple gradient from [`MainLayout.razor.css`](src\AppGen.UI\Shared\MainLayout.razor.css) and [`NavMenu.razor.css`](src\AppGen.UI\Shared\NavMenu.razor.css)

**What to avoid:**

- Heavy custom CSS framework â€” stay on Bootstrap grid/utilities, override tokens only
- Dark mode for v1 (defer; design tokens make it easy later)
- Animated backgrounds or gradients â€” keep it calm and tool-like

**Scope:** branding ships in the same PR as nav rename so users see Documentation / Web / Mobile in a polished shell from day one. No functional dependency on schema v5 or Flutter.

### B. Shared Application Definition (schema v5 prep) ✅

In [`AppGen.Core`](src\AppGen.Core):

- Add `ApplicationTargets` with `DocumentationTarget`, `WebTarget`, `MobileTarget`
- Add `MobileTargetSpec` (framework, packageName, apiBaseUrl, stateManagement, enabled)
- Extend [`SpecLoader`](src\AppGen.Engine\SpecLoader.cs) for v4 â†” v5 compat
- Refactor [`ProjectManifestMapper`](src\AppGen.UI\Services\ProjectManifestMapper.cs) to write unified targets

**UI change shipped:** `WizardStateService` + **Save manifest** on Web and Mobile; Mobile shows shared entities from Web session. Full shared entity editor component deferred to Phase 2.

### C. Flutter generator ✅

New in [`AppGen.Engine`](src\AppGen.Engine):

- `FlutterGenerator` implementing `IApplicationGenerator`
- Scriban templates under `AppGen.Templates/Templates/Mobile/flutter/`

**Output** (`mobile/flutter/`):

```
pubspec.yaml
lib/main.dart
lib/core/config/api_config.dart
lib/core/network/api_client.dart
lib/features/{entity}/models/{entity}_model.dart
lib/features/{entity}/services/{entity}_service.dart
lib/features/{entity}/providers/{entity}_provider.dart
lib/features/{entity}/screens/{entity}_list_screen.dart
lib/features/{entity}/screens/{entity}_detail_screen.dart
lib/features/{entity}/screens/{entity}_form_screen.dart
lib/app/router.dart
lib/app/theme.dart
```

Generate for **all entities with `IncludeInUi`** (or user-selected entity on Mobile tab).

**Phase 3 CRUD** (current): per entity — list with pull-to-refresh and FAB, detail with delete, create/edit form, Dio `getById` / `create` / `update` / `delete`, `go_router` routes (`/{entity}`, `/{entity}/new`, `/{entity}/:id`, `/{entity}/:id/edit`).

### D. Mobile tab UI ✅

New [`Pages/Mobile.razor`](src\AppGen.UI\Pages\Mobile.razor):

- Package name (default: `com.{appname}.app`)
- API base URL (default: `https://localhost:5001`)
- State management: Riverpod (fixed for POC)
- Entity picker (dropdown from loaded manifest entities)
- **Save manifest** / **Load draft** (shared with Web workflow)
- **Generate Mobile** button
- **Load project** (reads `appgen.json` from output folder)

### E. CLI ✅

```powershell
appgen mobile create --project output/MyApp [--entity Product] [--force]
```

### F. Tests ✅

- Flutter generate produces expected tree for one entity
- v4/v3 manifest still loads (normalized to v5 in memory)
- v5 manifest round-trips targets
- Existing portal/promote tests pass (12 total)

### G. Shared entity workflow ✅

New in [`AppGen.UI`](src/AppGen.UI):

- [`WizardStateService`](src/AppGen.UI/Services/WizardStateService.cs) — scoped per-session state; Web pushes on navigate away, Mobile subscribes on load
- [`ManifestSaveService`](src/AppGen.UI/Services/ManifestSaveService.cs) — writes `appgen.json` without generating `src/`
- Web **Save manifest** button; Mobile **Load draft**, **Save manifest**, shared-entity status banner

---

## Suggested long-term phases (after thin slice)

```mermaid
flowchart LR
    P1[Phase1_ThinSlice] --> P2[Phase2_SharedProjectTab]
    P2 --> P3[Phase3_FlutterCRUD]
    P3 --> P4[Phase4_AuthAndOffline]
    P4 --> P5[Phase5_SyncUpdates]
    P5 --> P6[Phase6_MoreGenerators]
```

| Phase | Focus |
|-------|--------|
| 1 | ~~Rename nav, UI branding, targets model, Flutter POC~~ **Done** |
| 2 | ~~Dedicated **Project** tab with single entity editor; layer checkboxes; one **Generate All**~~ **Done** |
| 3 | ~~Full Flutter CRUD + navigation per entity~~ **Done** |
| 4 | JWT auth scaffold + optional SQLite cache |
| 5 | Per-target incremental sync (`entity add` → update mobile) |
| 6 | MAUI, CI/CD templates, OpenAPI client gen |

---

## Bottom line

Your proposal is **coherent and achievable** because AppGen already generates multiple layers from one manifest. The highest-leverage moves are:

1. **One entity model, many generators** (formalize v5 targets)
2. **Generator plugin interface** (keeps Documentation/Web/Mobile independent)
3. **Flutter POC with conventions, not OpenAPI** (ship fast, harden later)
4. **Rename UI now** to match the story (Documentation / Web / Mobile)
5. **Rebrand the shell** so AppGen feels like a product, not a Blazor starter template

The thin slice gives you a demoable â€œapplication factoryâ€ narrative without committing to auth, offline, or multi-framework mobile yet.
