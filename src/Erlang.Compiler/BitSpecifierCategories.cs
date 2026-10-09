namespace Erlang.Compiler;

// Category keys describe conflict detection, not the source specifier values.
internal static class BitSpecifierCategories
{
    public const string Type = "type";
    public const string Endian = "endian";
    public const string Sign = "sign";
    public const string Unit = "unit";
}
