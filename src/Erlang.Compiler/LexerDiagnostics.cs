

namespace Erlang.Compiler;

internal static class LexerDiagnostics
{
    public const string UnsupportedEscape = "Hex, control and octal escapes are not implemented yet";
    public const string UnterminatedQuotedLiteral = "Unterminated quoted literal";
    public const string IllegalQuotedUnicode = "Illegal Unicode character in quoted literal";
}
