namespace Erlang.Compiler;

// These names describe bit segments, not lexer token kinds or term type categories.
internal static class BitSegmentTypes
{
    public const string Integer = "integer";
    public const string Binary = "binary";
    public const string Float = "float";
    public const string Utf8 = "utf8";
    public const string Utf16 = "utf16";
    public const string Utf32 = "utf32";
}
