using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public static class Lexer
{
    private static readonly HashSet<string> Keywords = [ErlangKeywords.After, ErlangKeywords.Begin, ErlangKeywords.Case, ErlangKeywords.Try, ErlangKeywords.Cond, ErlangKeywords.Catch, ErlangOperators.AndAlso, ErlangOperators.OrElse, ErlangKeywords.End, ErlangKeywords.Fun, ErlangKeywords.If, ErlangKeywords.Let, ErlangKeywords.Of, ErlangKeywords.Receive, ErlangKeywords.When, ErlangOperators.BitwiseNot, ErlangOperators.Not, ErlangOperators.IntegerDivide, ErlangOperators.Remainder, ErlangOperators.BitwiseAnd, ErlangOperators.And, ErlangOperators.BitwiseOr, ErlangOperators.BitwiseXor, ErlangOperators.ShiftLeft, ErlangOperators.ShiftRight, ErlangOperators.Or, ErlangOperators.Xor, ErlangKeywords.Maybe, ErlangKeywords.Else];

    public static List<Token> Scan(string text, bool blockPrefix = false)
    {
        var tokens = new List<Token>();
        int i = 0, depth = 0;
        string[] symbols = [ErlangSyntaxTokens.StrictBinaryGenerator, ErlangSyntaxTokens.BinaryGenerator, ErlangSyntaxTokens.StrictListGenerator, ErlangOperators.ExactEqual, ErlangOperators.ExactNotEqual, ErlangSyntaxTokens.ComprehensionSeparator, ErlangSyntaxTokens.ZipGeneratorSeparator, ErlangSyntaxTokens.ListGenerator, ErlangSyntaxTokens.FunctionArrow, ErlangOperators.NumericEqual, ErlangOperators.NumericNotEqual, ErlangOperators.LessOrEqual, ErlangOperators.GreaterOrEqual, ErlangOperators.Append, ErlangOperators.SubtractList, ErlangSyntaxTokens.BinaryOpen, ErlangSyntaxTokens.BinaryClose, ErlangSyntaxTokens.MapAssociation, ErlangSyntaxTokens.MapExactField, ErlangSyntaxTokens.ConditionalMatch];
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
            if (blockPrefix && depth == 0 && c == '.')
                break;
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
                    if (name is ErlangKeywords.Receive or ErlangKeywords.Case or ErlangKeywords.Fun or ErlangKeywords.If or ErlangKeywords.Begin or ErlangKeywords.Try or ErlangKeywords.Maybe)
                        depth++;
                    else if (name == ErlangKeywords.End && --depth == 0)
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
                    text[start..i].Replace(NumericLiteralSyntax.DigitSeparator, string.Empty),
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
            string.Empty,
            i,
            i
        ));

        return tokens;
    }
}
