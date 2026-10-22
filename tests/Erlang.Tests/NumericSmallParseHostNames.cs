namespace Erlang.Tests;

internal static class NumericSmallParseHostNames
{
    internal const string Test = "runtime/list-to-integer-small-byte-narrowing-and-fallback";
    internal const string Failure = "SMALL list integer parsing/source error arguments contract failed.";
    internal const string Erlang = "erlang";
    internal const string Function = "list_to_integer";
    internal const string Tail = "integer_parse_tail";
    internal const string InvalidBase = "integer_parse_invalid_base";
    internal const string ErrorInfo = "error_info";
    internal const string ModuleKey = "module";
    internal const string Formatter = "erl_erts_errors";
    internal const int NativeSmallBits = 59;
    internal const int ByteRange = 256;
    internal const int DecimalBase = 10;
    internal const int HexadecimalBase = 16;
    internal const int MinimumBase = 2;
    internal const int MaximumBase = 36;
    internal const int DecimalSmallDigits = 17;
    internal const int HexadecimalSmallDigits = 14;
    internal const int LongZeroPrefix = 1000;
    internal const long DecimalSmallOnes = 11111111111111111;
    internal const long DecimalFallbackOnes = 111111111111111111;
    internal const long HexadecimalSmallOnes = 0x11111111111111;
    internal const long HexadecimalFallbackOnes = 0x111111111111111;
}
