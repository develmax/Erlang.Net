namespace Erlang;

internal static class ErlangIntegerParseParameters
{
    internal const int NativeSmallMagnitudeBits = 59;
    internal const long MinimumNativeSmall = -(1L << NativeSmallMagnitudeBits);
    internal const long MaximumNativeSmall = (1L << NativeSmallMagnitudeBits) - 1;

    internal static ReadOnlySpan<byte> DigitsPerSmall =>
    [
        59, 37, 29, 25, 22, 21, 19, 18, 17, 17, 16, 15, 15, 15, 14, 14, 14, 13,
        13, 13, 13, 13, 12, 12, 12, 12, 12, 12, 12, 11, 11, 11, 11, 11, 11
    ];
}
