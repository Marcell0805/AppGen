namespace AppGen.UI.Models;

public static class ClrTypeCatalog
{
    public static IReadOnlyList<string> GetTypes(AppGen.Core.Models.DatabaseProvider db) =>
        AppGen.Core.ClrTypeCatalog.GetTypes(db);
}
