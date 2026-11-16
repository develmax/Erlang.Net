namespace Erlang.Compiler;

internal static class LexerDiagnostics
{
    public const string UnterminatedQuotedLiteral = "Unterminated quoted literal";
    public const string IllegalQuotedUnicode = "Illegal Unicode character in quoted literal";
    public const string IllegalAtom = "Illegal atom name";
    public const string IllegalVariable = "Illegal variable name";
    internal const string IllegalSigilSuffix = "Illegal string sigil suffix.";
    internal const string StringSeparatorRequired = "Adjacent quoted strings require a separator.";
    internal const string TripleQuotedOpeningLine = "Only whitespace is allowed after a triple-quoted string's opening delimiter.";
    internal const string TripleQuotedIndentation = "Triple-quoted string line does not match the closing delimiter's indentation.";
}
