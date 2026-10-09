using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public static class Lexer
{
    private static readonly HashSet<string> Keywords = ["after", "begin", "case", "try", "cond", "catch", "andalso", "orelse", "end", "fun", "if", "let", "of", "receive", "when", "bnot", "not", "div", "rem", "band", "and", "bor", "bxor", "bsl", "bsr", "or", "xor", "maybe", "else"];

    public static List<Token> Scan(string text, bool blockPrefix = false)
    {
        var tokens = new List<Token>();
        int i = 0, depth = 0;
        string[] symbols = ["=:=", "=/=", "->", "==", "/=", "=<", ">=", "++", "--", "<<", ">>", "=>", ":="];
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }
            if (text[i] == '%')
            {
                while (i < text.Length && text[i] != '\n')
                    i++;
                continue;
            }
            int start = i;
            char c = text[i++];
            if (c is '\'' or '"')
            {
                var value = new StringBuilder();
                bool closed = false;
                while (i < text.Length)
                {
                    char ch = text[i++];
                    if (ch == c)
                    {
                        closed = true;
                        break;
                    }
                    if (ch == '\\')
                    {
                        if (i == text.Length)
                            break;
                        ch = text[i++];
                        if (ch is 'x' or '^' || char.IsDigit(ch))
                            throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, LexerDiagnostics.UnsupportedEscape, i - 2);
                        ch = ch switch
                        {
                            'n' => '\n',
                            'r' => '\r',
                            't' => '\t',
                            'b' => '\b',
                            'f' => '\f',
                            'v' => '\v',
                            'e' => '\x1b',
                            's' => ' ',
                            _ => ch
                        };
                    }
                    value.Append(ch);
                }
                if (!closed)
                    throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, LexerDiagnostics.UnterminatedQuotedLiteral, start);
                string literal = value.ToString();
                for (int offset = 0; offset < literal.Length;)
                {
                    if (!Rune.TryGetRuneAt(literal, offset, out var rune) || rune.Value is 0xfffe or 0xffff)
                        throw new CompileException(CompilerDiagnosticCodes.InvalidLiteral, LexerDiagnostics.IllegalQuotedUnicode, start);
                    offset += rune.Utf16SequenceLength;
                }
                tokens.Add(new(
                    c == '\'' ? LexerTokenKinds.QuotedAtom : LexerTokenKinds.String,
                    literal,
                    start,
                    i
                ));
                continue;
            }
            if (char.IsLetter(c) || c == '_')
            {
                while (i < text.Length && (char.IsLetterOrDigit(text[i]) || text[i] is '_' or '@'))
                    i++;
                string name = text[start..i];
                tokens.Add(new(
                    char.IsUpper(c) || c == '_' ? LexerTokenKinds.Variable : Keywords.Contains(name) ? LexerTokenKinds.Keyword : LexerTokenKinds.Atom,
                    name,
                    start,
                    i
                ));
                if (blockPrefix)
                {
                    if (name is "receive" or "case" or "fun")
                        depth++;
                    else if (name == "end" && --depth == 0)
                        break;
                }
                continue;
            }
            if (char.IsDigit(c))
            {
                while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_'))
                    i++;
                bool floating = false;
                if (i + 1 < text.Length && text[i] == '.' && char.IsDigit(text[i + 1]))
                {
                    floating = true;
                    i++;
                    while (i < text.Length && (char.IsDigit(text[i]) || text[i] == '_'))
                        i++;
                }
                if (floating && i < text.Length && text[i] is 'e' or 'E')
                {
                    i++;
                    if (i < text.Length && text[i] is '+' or '-')
                        i++;
                    while (i < text.Length && char.IsDigit(text[i]))
                        i++;
                }
                tokens.Add(new(
                    floating ? LexerTokenKinds.Float : LexerTokenKinds.Integer,
                    text[start..i].Replace("_", ""),
                    start,
                    i
                ));
                continue;
            }
            var symbol = symbols.FirstOrDefault(s => text.AsSpan(start).StartsWith(s, StringComparison.Ordinal));
            if (symbol is not null)
                i = start + symbol.Length;
            tokens.Add(new(
                LexerTokenKinds.Symbol,
                symbol ?? c.ToString(),
                start,
                i
            ));
        }
        tokens.Add(new(
            LexerTokenKinds.EndOfInput,
            "",
            i,
            i
        ));

        return tokens;
    }
}
