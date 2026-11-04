namespace Erlang;

internal static class UnicodeNames
{
    internal const string Utility = "unicode_util";
    internal const string Binary = "characters_to_binary";
    internal const string List = "characters_to_list";
    internal const string BinaryTrap = "characters_to_binary_int";
    internal const string ListTrap = "characters_to_list_int";
    internal const string Bom = "bom_to_encoding";
    internal const string EncodingBom = "encoding_to_bom";
    internal const string SevenBit = "bin_is_7bit";
    internal const string Category = "category";
    internal const string Whitespace = "is_whitespace";
    internal const string Start = "is_id_start";
    internal const string Continue = "is_id_continue";
    internal const string Latin1 = "latin1";
    internal const string Utf8 = "utf8";
    internal const string Utf16 = "utf16";
    internal const string Utf32 = "utf32";
    internal const string Unicode = "unicode";
    internal const string Big = "big";
    internal const string Little = "little";
    internal const string Incomplete = "incomplete";
    internal const int Maximum = 0x10ffff;
    internal const int SurrogateStart = 0xd800;
    internal const int SurrogateEnd = 0xdfff;
    internal const int ChunkBytes = 8192;
    internal const int NormalizationRow = 200;
    internal const int WorkSlice = 1024;
    internal const int BitsPerByte = 8;
    internal const string SourceFile = "unicode.erl";
}
