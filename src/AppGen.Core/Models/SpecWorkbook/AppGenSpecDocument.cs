namespace AppGen.Core.Models.SpecWorkbook;

using AppGen.Core.Models;

public sealed class AppGenSpecDocument
{
    public ApplicationSheetData Application { get; init; } = new();
    public List<SectionSheetRow> Sections { get; init; } = [];
    public List<EntitySheetRow> Entities { get; init; } = [];
    public List<PropertySheetRow> Properties { get; init; } = [];
    public Dictionary<string, List<Dictionary<string, string>>> EntityData { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ApplicationSheetData
{
    public string? ApplicationName { get; set; }
    public string? RootNamespace { get; set; }
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    public string? Database { get; set; }
    public bool? EnableDocumentation { get; set; }
    public bool? EnableWeb { get; set; }
    public bool? EnableMobile { get; set; }
    public bool? EnableWebAuth { get; set; }
    public bool? EnableMobileOffline { get; set; }
    public string? MobileThemePreset { get; set; }
    public string? MobileApiBaseUrl { get; set; }
    public string? DocumentationPreset { get; set; }
}

public sealed class SectionSheetRow
{
    public int RowNumber { get; init; }
    public string SectionId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = "planned";
    public string? Summary { get; init; }
    public string? Tags { get; init; }
    public int? NavNum { get; init; }
    public string? NavLabel { get; init; }
}

public sealed class EntitySheetRow
{
    public int RowNumber { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string? TableName { get; init; }
    public bool IncludeInUi { get; init; } = true;
    public bool IncludeAuditColumns { get; init; }
}

public sealed class PropertySheetRow
{
    public int RowNumber { get; init; }
    public string EntityName { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    public string ClrType { get; init; } = "string";
    public bool IsKey { get; init; }
    public bool IsNullable { get; init; }
    public string? ForeignKeyEntity { get; init; }
    public string? ColumnName { get; init; }
}

public sealed class SpecValidationIssue
{
    public required string Sheet { get; init; }
    public int? Row { get; init; }
    public required string Message { get; init; }
    public bool IsError { get; init; } = true;
}

public sealed class SpecImportOptions
{
    public bool MergeEntities { get; init; }
    public bool ValidateOnly { get; init; }
    public bool WriteSeedScripts { get; init; } = true;
}

public sealed class SpecImportResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public SolutionSpec? Spec { get; init; }
    public IReadOnlyList<SpecValidationIssue> Issues { get; init; } = [];
}
