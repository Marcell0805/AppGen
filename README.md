# AppGen

Standalone **.NET 8** code generator that scaffolds **layered ASP.NET Core Web API** solutions from templates and a simple entity spec.

It generates a ready-to-run Visual Studio solution (Swagger enabled by default) plus CRUD scaffolding (controller → service → repository → EF Core).

## Repository description (for GitHub)

**AppGen — a standalone .NET 8 app generator that scaffolds layered ASP.NET Core Web APIs (EF Core + Swagger) with CRUD from entity definitions. Includes CLI + simple Blazor UI wizard.**

## Features

- **Standalone**: no dependency on other repos or private packages
- **CLI**: `create` a solution and `entity add` to scaffold CRUD
- **Simple UI**: Blazor Server wizard with QuickGrid entity editor and optional UI target selection
- **Optional MVC Web client**: generate `{AppName}.MVC` with per-entity list/edit pages (HttpClient → API), PR-style layering
- **Database providers**: SQL Server + Oracle (template switch)
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

Open `http://localhost:5099` and click **Generate**. By default, solutions are written to `AppGen/output/{AppName}/` (same repo as the generator).

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
| `appgen create <Name> --output <path> [--database SqlServer\|Oracle] [--ui MvcWeb]` | Scaffold solution (optional MVC Web UI) |
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
