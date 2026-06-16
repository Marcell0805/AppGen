using AppGen.Core;
using AppGen.Core.Models;

namespace AppGen.Engine;

internal static class SqlScriptHelpers
{
    public static string TableName(EntitySpec entity) => entity.TableName ?? entity.Name;

    public static string SchemaOrDefault(EntitySpec entity, DatabaseProvider database) =>
        entity.Schema ?? database switch
        {
            DatabaseProvider.SqlServer => "dbo",
            DatabaseProvider.PostgreSql => "public",
            _ => "dbo"
        };

    public static string ColumnName(PropertySpec property, DatabaseProvider database) =>
        database == DatabaseProvider.Oracle
            ? NamingHelper.ToOracleColumnName(property.ColumnName ?? property.Name)
            : property.ColumnName ?? property.Name;

    public static EntitySpec? ResolveForeignKeyEntity(SolutionSpec spec, PropertySpec property) =>
        string.IsNullOrWhiteSpace(property.ForeignKeyEntity)
            ? null
            : spec.Entities.FirstOrDefault(e =>
                e.Name.Equals(property.ForeignKeyEntity, StringComparison.OrdinalIgnoreCase));
}
