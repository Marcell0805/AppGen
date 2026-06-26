# AppGen

Standalone **.NET 8** code generator that scaffolds **layered ASP.NET Core Web API** solutions from templates and a simple entity spec.

It generates a ready-to-run Visual Studio solution (Swagger enabled by default) plus CRUD scaffolding (controller → service → repository → EF Core).

## Repository description (for GitHub)

**AppGen — a standalone .NET 8 app generator that scaffolds layered ASP.NET Core Web APIs (EF Core + Swagger) with CRUD from entity definitions. Includes CLI + simple Blazor UI wizard.**

## Features

- **Standalone**: no dependency on other repos or private packages
- **CLI**: `create` a solution and `entity add` to scaffold CRUD
- **Portal generator**: static documentation sites (engineering-portal preset) with `appgen.json` manifest
- **Promote to API**: scaffold `src/` from a portal-first project when the design is validated
- **Simple UI**: Blazor Server wizard with QuickGrid entity editor and optional UI target selection
- **Optional MVC Web client**: generate `{AppName}.MVC` with per-entity list/edit pages (HttpClient → API), PR-style layering
- **Database providers**: SQL Server, PostgreSQL, and Oracle — each can emit `scripts/{provider}/` create + seed SQL when entities are added
- **Swagger-first**: generated APIs open Swagger on run; `/` redirects to `/swagger` in Development
- **Regeneration-safe**: generator inserts its changes into marked regions in shared files

## Quick start

```powershell
cd AppGen
dotnet run --project src/AppGen.CLI -- create InventorySystem --output samples --database SqlServer --ui MvcWeb
dotnet run --project src/AppGen.CLI -- entity add Product --project samples/InventorySystem
dotnet build samples/InventorySystem/InventorySystem.sln
```

## Run the UI

```powershell
cd AppGen
dotnet run --project src/AppGen.UI --urls "http://localhost:5099"
```

Open `http://localhost:5099` — start on **Project** to define entities and layers; use **?** on any tab for in-app help.

## Project metadata and READMEs

On the **Project** tab, **About this app** stores a tagline and description in `appgen.json` (schema v6 `project` section). Generated outputs include per-layer `README.md` files (hub, Documentation, Web, Mobile) with first-run steps and troubleshooting.

## Evolution roadmap

See [docs/EVOLUTION_ROADMAP.md](docs/EVOLUTION_ROADMAP.md) for the Application Factory vision (shared manifest, Flutter, UI branding, and phased delivery).

## Documentation workflow

```powershell
# Phase 1 — static portal for sharing
dotnet run --project src/AppGen.CLI -- portal create DeltaCore --output output

# Edit portal/data/*.json, then import changes back into the manifest
dotnet run --project src/AppGen.CLI -- portal import --project output/DeltaCore

# Phase 2 — promote to API (refine entities in the API tab or appgen.json first)
dotnet run --project src/AppGen.CLI -- promote --project output/DeltaCore
```

Generated layout (portal-first project):

```
DeltaCore/
├── appgen.json          # manifest (portal + entitySketches + entities)
├── portal/              # static site (GitHub Pages / Live Server)
└── src/                 # API solution (after promote)
```

## Generated layout

```
InventorySystem.sln
InventorySystem.slnLaunch       # VS multi-startup profile: API + MVC (when MVC Web included)
appgen.json
src/
  InventorySystem.API/          # Controllers, Swagger, Program.cs
  InventorySystem.Application/  # Services, repository interfaces
  InventorySystem.Domain/       # Entities
  InventorySystem.Persistence/  # EF Core, repositories, configurations
  InventorySystem.Shared/       # DTOs, Response<T>
  InventorySystem.Tests/
  InventorySystem.MVC/          # Optional MVC Web UI (when --ui MvcWeb)
```

## Commands

| Command | Description |
|---------|-------------|
| `appgen mobile create --project <path> [--entity Product]` | Generate Flutter POC under `mobile/flutter/` |
| `appgen portal create <Name> --output <path> [--preset engineering-portal]` | Scaffold static portal + appgen.json manifest |
| `appgen portal import --project <path>` | Merge portal/data edits into appgen.json |
| `appgen promote --project <path> [--force]` | Generate API solution from manifest |
| `appgen create <Name> --output <path> [--database SqlServer\|PostgreSql\|Oracle] [--ui MvcWeb]` | Scaffold solution (API-only) |
| `appgen entity add <Name> --project <path>` | Add CRUD slice for an entity |

## Sample output

See `samples/InventorySystem/` after running the commands above (generated locally; not committed by default).

Generated API projects include `Properties/launchSettings.json` with `launchUrl: swagger`, so Visual Studio and `dotnet run` open Swagger automatically. Visiting `/` in Development redirects to `/swagger`.

When MVC Web is included, the generator writes:
- `{AppName}.slnLaunch` with **API + MVC** and **API only** profiles (Visual Studio 2022 17.11+ — pick from the startup dropdown, then F5)
- `.vscode/launch.json` with an **API + MVC** compound configuration (Cursor / VS Code)

Enable **Multi Launch Profiles** under Tools → Options → Preview Features if the VS profile does not appear.

## Notes

- **App name normalization**: spaces and invalid characters are converted to `_` (e.g. `Hello World` → `Hello_World`).
- **CRUD behavior**: each entity generates Controller → Service → Repository → EF Core. Reads use `AsNoTracking()`; updates load a **tracked** entity via `GetTrackedByIdAsync` before `SaveChanges`. `CreateRequest` excludes the primary key by default.
- **Entity naming**: API routes and MVC controllers use the entity name exactly as defined (no automatic pluralization).
- **Key naming**: new entities default to `{EntityName}_Id` as the primary key (e.g. `Cart_Id`).
- **Foreign keys**: mark a property with the **FK →** dropdown (or use `{ReferencedEntity}_Id` naming for auto-detect). EF configures `HasOne`/`HasForeignKey`; MVC edit pages show dropdowns for FK fields.
- **Per-entity UI**: uncheck **UI** on an entity to generate API/EF only (no MVC pages or nav link).
- **Deferred**: entity relationships, composite keys, FluentValidation, pagination, and EF migrations generation.
