namespace Erlang.Otp;

internal static class ProcLibReportAtomNames
{
    public const string Latin1 = "latin1";
    public const string HexPrefix = "\\x{";
    public const string HexSuffix = "}";
    public static readonly HashSet<string> Reserved = new(StringComparer.Ordinal)
    {
        "after", "begin", "case", "try", "cond", "catch", "andalso", "orelse",
        "end", "fun", "if", "let", "of", "receive", "when", "bnot", "not",
        "div", "rem", "band", "and", "bor", "bxor", "bsl", "bsr", "or", "xor",
        "maybe", "else"
    };
}
