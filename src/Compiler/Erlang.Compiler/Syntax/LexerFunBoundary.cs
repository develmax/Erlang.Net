namespace Erlang.Compiler;

internal static class LexerFunBoundary
{
    public static bool HasClauses(string source, int position)
    {
        position = SkipTrivia(source, position);
        if (position < source.Length && (char.IsUpper(source[position]) || source[position] == '_'))
        {
            do
            {
                position++;
            } while (position < source.Length
                && (char.IsLetterOrDigit(source[position]) || source[position] is '_' or '@'));
            position = SkipTrivia(source, position);
        }

        return position < source.Length && source[position] == ErlangSyntaxTokens.OpenParenthesis[0];
    }

    private static int SkipTrivia(string source, int position)
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

            break;
        }

        return position;
    }
}
