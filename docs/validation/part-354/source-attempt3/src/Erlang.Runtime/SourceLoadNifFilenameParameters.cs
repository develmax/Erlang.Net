namespace Erlang;

internal static class SourceLoadNifFilenameParameters
{
    internal const int MinimumCodePoint = 1;
    internal const int MaximumCodePoint = 0x10ffff;
    internal const int FirstSurrogate = 0xd800;
    internal const int LastSurrogate = 0xdfff;
}
