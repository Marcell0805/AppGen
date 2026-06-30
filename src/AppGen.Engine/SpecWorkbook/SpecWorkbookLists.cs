using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

internal static class SpecWorkbookLists
{
    public const string ListsSheetName = "_Lists";
    public const string InstructionsSheetName = "Instructions";
    public const int DataEntryMaxRow = 500;

    public static readonly string[] Databases =
    [
        nameof(DatabaseProvider.SqlServer),
        nameof(DatabaseProvider.Oracle),
        nameof(DatabaseProvider.PostgreSql)
    ];

    public static readonly string[] Booleans = ["true", "false"];

    public static readonly string[] MobileThemePresets = ["appgen", "portal", "cookbook"];

    public static readonly string[] DocumentationPresets = ["engineering-portal"];

    public static readonly string[] SectionStatuses = ["planned", "active", "draft"];

    public static string[] ClrTypes => ClrTypeCatalog.GetTypes(DatabaseProvider.SqlServer).ToArray();

    public const string GuidePath = "docs/authoring/appgen-spec-workbook-guide.md";
}
