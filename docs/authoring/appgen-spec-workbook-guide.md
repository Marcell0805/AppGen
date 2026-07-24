# AppGen Spec Workbook — User Guide

Use an Excel workbook to describe your application structure, portal sections, and sample data. Import it into AppGen to populate `appgen.json` and seed SQL scripts.

## Quick start

1. **Download** the template from the Project tab (**Download template**) or use `docs/templates/appgen-spec-template.xlsx`.
2. **Edit** the workbook offline in Excel or LibreOffice.
3. **Import** via the Project tab (**Import workbook**) or the CLI.
4. **Generate** your web, mobile, or documentation layers as usual.

## Workbook sheets

| Sheet | Purpose |
|-------|---------|
| `Instructions` | Quick start and link to this guide |
| `Application` | App name, namespace, database, layer toggles |
| `Sections` | Documentation portal sections and nav |
| `Entities` | Entity names and UI/audit options |
| `Properties` | Columns per entity (`PropertyName`, `ClrType`, keys, FKs) |
| `Data_<Entity>` | Sample rows for seed SQL (e.g. `Data_User`) |
| `_Lists` | Hidden — dropdown source lists (do not edit) |

## Dropdowns and layout

The template uses **Excel data validation** so you pick values instead of typing them:

| Where | Dropdown values |
|-------|-----------------|
| Application → `Database` | SqlServer, Oracle, PostgreSql |
| Application → `Enable*` flags | true, false |
| Application → `MobileThemePreset` | appgen, portal, cookbook |
| Application → `DocumentationPreset` | engineering-portal |
| Sections → `Status` | planned, active, draft |
| Entities → `IncludeInUi`, `IncludeAuditColumns` | true, false |
| Properties → `EntityName`, `ForeignKeyEntity` | Names from **Entities** sheet |
| Properties → `ClrType` | string, int, long, decimal, double, bool, DateTime, DateOnly, Guid |
| Properties → `IsKey`, `IsNullable` | true, false |

**UX helpers:**

- **Application** column A (field names) is grey and **sheet-protected** — edit values in column B only.
- **Header row frozen** on table sheets (scroll without losing column titles).
- **Row 2** on table sheets is highlighted as an **example row** — copy the pattern for new entries.
- Dropdowns apply to rows 2–500 on Sections, Entities, and Properties.

Free-text fields: app name, descriptions, entity/property names, section titles, and all `Data_*` sample values.

### Application fields

Key fields on the **Application** sheet: `ApplicationName`, `RootNamespace`, `Database` (`SqlServer`, `PostgreSql`, `Oracle`), `EnableDocumentation`, `EnableWeb`, `EnableMobile`, and mobile theme/API settings.

### Properties

Each property row needs `EntityName`, `PropertyName`, and `ClrType`. Supported types include `string`, `int`, `long`, `decimal`, `bool`, `DateTime`, `DateOnly`, and `Guid`.

### Data sheets

Name sheets `Data_<EntityName>` (e.g. `Data_User`). Header row columns must match property names. Imported rows become `scripts/<provider>/002-seed-data.sql`.

## CLI

```powershell
# Export empty template
dotnet run --project src/AppGen.CLI -- spec export --template --output MyApp-spec.xlsx

# Export from existing project
dotnet run --project src/AppGen.CLI -- spec export --project output/MyApp --output MyApp-spec.xlsx

# Import workbook
dotnet run --project src/AppGen.CLI -- spec import --project output/MyApp --input MyApp-spec.xlsx

# Validate only
dotnet run --project src/AppGen.CLI -- spec import --project output/MyApp --input MyApp-spec.xlsx --validate-only
```

Use `--merge` to keep existing entities not listed in the workbook.

## Validation

Import reports errors with **sheet name and row number**, for example:

- Unknown `ClrType`
- Property referencing a missing entity
- Invalid values in `Data_*` sheets (e.g. text in an `int` column)
- Duplicate primary keys in data rows

Warnings (non-blocking) include extra data columns and entities without properties (a key is added automatically).

## Tips

- Export the current project before large edits so you start from live `appgen.json` data.
- Workbook **ApplicationName** drives the hub folder on import — keep it aligned with your project name.
- After import, the UI reloads from the hub `appgen.json`; Documentation sections appear on the Documentation tab (block content still edited there).
- Keep one key property per entity (`IsKey` = true), or let import add `{Entity}_Id` automatically.
