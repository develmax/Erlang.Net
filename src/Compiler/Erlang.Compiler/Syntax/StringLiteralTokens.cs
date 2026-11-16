using System.Text;

namespace Erlang.Compiler;

internal static class StringLiteralTokens
{
    internal static string Read(IReadOnlyList<Token> tokens, ref int cursor)
    {
        Token token = tokens[cursor++];
        string first = token.Text;
        if (token.IsStringSigil || cursor == tokens.Count || tokens[cursor] is not { Kind: LexerTokenKinds.String, IsStringSigil: false })
        {
            return first;
        }

        var value = new StringBuilder(first);
        while (cursor < tokens.Count && tokens[cursor] is { Kind: LexerTokenKinds.String, IsStringSigil: false })
        {
            value.Append(tokens[cursor++].Text);
        }

        return value.ToString();
    }
}
