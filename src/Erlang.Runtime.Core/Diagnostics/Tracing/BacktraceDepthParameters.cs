namespace Erlang;

internal static class BacktraceDepthParameters
{
    internal const int MaximumDepth = 64;
    internal const long MaximumNativeSmall = (1L << 59) - 1;
}
