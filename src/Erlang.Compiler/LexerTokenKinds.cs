namespace Erlang.Compiler;

// Lexer categories are independent of bit segment types with the same spelling.
internal static class LexerTokenKinds
{
    public const string QuotedAtom = "quoted_atom";
    public const string String = "string";
    public const string Variable = "variable";
    public const string Keyword = "keyword";
    public const string Atom = "atom";
    public const string Float = "float";
    public const string Integer = "integer";
    public const string Symbol = "symbol";
    public const string EndOfInput = "eof";
}
