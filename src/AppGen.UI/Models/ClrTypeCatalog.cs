using AppGen.Core.Models;

namespace AppGen.UI.Models;

public static class ClrTypeCatalog
{
    public static IReadOnlyList<string> GetTypes(DatabaseProvider db) =>
        db switch
        {
            DatabaseProvider.SqlServer or DatabaseProvider.Oracle =>
            [
                "string",
                "long",
                "int",
                "decimal",
                "double",
                "bool",
                "DateTime",
                "Guid"
            ],
            _ => ["string", "long", "int", "decimal", "bool", "DateTime"]
        };
}
