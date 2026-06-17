using AppGen.Core.Models;

namespace AppGen.UI.Models;

public static class ClrTypeCatalog
{
    public static IReadOnlyList<string> GetTypes(DatabaseProvider db)
    {
        string[] types = db switch
        {
            DatabaseProvider.SqlServer or DatabaseProvider.Oracle or DatabaseProvider.PostgreSql =>
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

        return types
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
