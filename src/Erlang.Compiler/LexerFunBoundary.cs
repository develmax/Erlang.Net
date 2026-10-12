namespace Erlang.Compiler;

internal static class LexerFunBoundary
{
    public static bool HasClauses(string source, int position)
    {
        while (position < source.Length)
        {
            if (LexerWhitespace.Is(source[position]))
            {
                position++;
                continue;
            }
            if (source[position] == LexerFormBoundary.CommentMarker)
            {
                while (position < source.Length && source[position] != '\n')
                    position++;
                continue;
            }

            return source[position] == ErlangSyntaxTokens.OpenParenthesis[0];
        }

        return false;
    }
}
