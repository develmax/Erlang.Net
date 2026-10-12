namespace Erlang.Compiler;

internal static class LexerFormBoundary
{
    public const char Marker = '.';
    public const char CommentMarker = '%';

    public static bool Is(string source, int following) =>
        following == source.Length || source[following] == CommentMarker || LexerWhitespace.Is(source[following]);
}
