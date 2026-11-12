using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

internal static class BeamMacroStringification
{
    internal static string Render(IEnumerable<Token> argument, int offset) =>
        string.Join(' ', argument.Select(token => RenderToken(token, offset)));

    private static string RenderToken(Token token, int offset) => token.Kind switch
    {
        LexerTokenKinds.Symbol or LexerTokenKinds.Keyword or LexerTokenKinds.Variable => token.Text,
        LexerTokenKinds.Integer when token.IsCharacterLiteral => Character(int.Parse(token.Text, CultureInfo.InvariantCulture)),
        LexerTokenKinds.Integer => BigInteger.Parse(token.Text, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
        LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom when IsPlainAtom(token.Text) => token.Text,
        LexerTokenKinds.Atom or LexerTokenKinds.QuotedAtom => Quote(token.Text, '\''),
        LexerTokenKinds.String => Quote(token.Text, '"'),
        _ => throw new CompileException(CompilerDiagnosticCodes.UnsupportedSyntax, BeamMacroDiagnostics.UnsupportedStringification, offset)
    };

    private static bool IsPlainAtom(string value) =>
        value.Length != 0 && IsLowercase(value[0]) && value.All(IsNameCharacter)
        && !Lexer.IsKeyword(value);

    private static bool IsLowercase(char value) =>
        value is >= 'a' and <= 'z' or >= '\u00df' and <= '\u00ff' and not '\u00f7';

    private static bool IsNameCharacter(char value) =>
        IsLowercase(value) || value is >= 'A' and <= 'Z' or >= '\u00c0' and <= '\u00de' and not '\u00d7' or >= '0' and <= '9' or '_' or '@';

    internal static string Character(int value) => value == ' ' ? "$\\s" : "$" + Escape(value, -1);

    internal static string Quote(string text, char delimiter)
    {
        var result = new StringBuilder().Append(delimiter);
        foreach (var rune in text.EnumerateRunes())
        {
            result.Append(Escape(rune.Value, delimiter));
        }

        return result.Append(delimiter).ToString();
    }

    private static string Escape(int value, int delimiter) => value switch
    {
        _ when value == delimiter || value == '\\' => "\\" + (char)value,
        >= ' ' and <= '~' or >= '\u00a0' => char.ConvertFromUtf32(value),
        '\n' => "\\n",
        '\r' => "\\r",
        '\t' => "\\t",
        '\v' => "\\v",
        '\b' => "\\b",
        '\f' => "\\f",
        '\u001b' => "\\e",
        '\u007f' => "\\d",
        _ => "\\" + Convert.ToString(value, 8).PadLeft(3, '0')
    };
}
