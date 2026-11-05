namespace Erlang;

internal static class StringUtf8Parameters
{
    internal const int MaximumCodepointBytes = 4;
    internal const int BitsPerByte = 8;
    internal const int MaximumCodepointBits = MaximumCodepointBytes * BitsPerByte;
}
