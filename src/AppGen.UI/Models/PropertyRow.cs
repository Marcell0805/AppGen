namespace AppGen.UI.Models;

public sealed class PropertyRow
{
    public string Name { get; set; } = string.Empty;
    public string ClrType { get; set; } = "string";
    public bool IsKey { get; set; }
    public bool IsNullable { get; set; }

    public static PropertyRow FromDefaults(string name, string clrType, bool isKey = false, bool isNullable = false) =>
        new() { Name = name, ClrType = clrType, IsKey = isKey, IsNullable = isNullable };
}
