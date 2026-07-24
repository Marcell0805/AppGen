using AppGen.Core.Models;

namespace AppGen.Core;

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
                "Guid",
                "DateOnly"
            ],
            _ => ["string", "long", "int", "decimal", "bool", "DateTime"]
        };

        return types
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsAllowed(string clrType, DatabaseProvider db) =>
        GetTypes(db).Any(t => t.Equals(clrType, StringComparison.OrdinalIgnoreCase));
}
