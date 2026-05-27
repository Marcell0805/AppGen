# AppGen

Standalone **.NET 8** code generator that scaffolds **layered ASP.NET Core Web API** solutions from templates and a simple entity spec.

It generates a ready-to-run Visual Studio solution (Swagger enabled by default) plus CRUD scaffolding (controller → service → repository → EF Core).

## Repository description (for GitHub)

**AppGen — a standalone .NET 8 app generator that scaffolds layered ASP.NET Core Web APIs (EF Core + Swagger) with CRUD from entity definitions. Includes CLI + simple Blazor UI.**

## Features

- **Standalone**: no dependency on other repos or private packages
- **CLI**: `create` a solution and `entity add` to scaffold CRUD
- **Simple UI**: Blazor Server wizard that calls the same generator engine
- **Database providers**: SQL Server + Oracle (template switch)
- **Swagger-first**: generated APIs open Swagger on run; `/` redirects to `/swagger` in Development
- **Regeneration-safe**: generator inserts its changes into marked regions in shared files

## Quick start

```powershell
cd AppGen
dotnet run --project src/AppGen.CLI -- create InventorySystem --output samples --database SqlServer
dotnet run --project src/AppGen.CLI -- entity add Product --project samples/InventorySystem
dotnet run --project src/AppGen.CLI -- entity add Supplier --project samples/InventorySystem
dotnet build samples/InventorySystem/InventorySystem.sln
```

## Run the UI

```powershell
cd AppGen
dotnet run --project src/AppGen.UI --urls "http://localhost:5099"
```

Open `http://localhost:5099` and click **Generate**.

## Generated layout

```
InventorySystem.sln
appgen.json
src/
  InventorySystem.API/          # Controllers, Swagger, Program.cs
  InventorySystem.Application/  # Services, repository interfaces
  InventorySystem.Domain/       # Entities
  InventorySystem.Persistence/  # EF Core, repositories, configurations
  InventorySystem.Shared/       # DTOs, Response<T>
  InventorySystem.Tests/
```

## Commands

| Command | Description |
|---------|-------------|
| `appgen create <Name> --output <path> [--database SqlServer\|Oracle]` | Scaffold solution |
| `appgen entity add <Name> --project <path>` | Add CRUD slice for an entity |

## Sample output

See `samples/InventorySystem/` after running the commands above (generated locally; not committed by default).

Generated API projects include `Properties/launchSettings.json` with `launchUrl: swagger`, so Visual Studio and `dotnet run` open Swagger automatically. Visiting `/` in Development redirects to `/swagger`.

## Notes

- **App name normalization**: spaces and invalid characters are converted to `_` (e.g. `Hello World` → `Hello_World`).
