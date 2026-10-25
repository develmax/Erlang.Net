namespace Erlang;

internal static class DistributionHandleLayout
{
    internal const int Arity = 2;
    internal const int ConnectionIdIndex = 0;
    internal const int ReferenceIndex = 1;
    internal const int ConnectionIdBits = 24;
    internal const uint ConnectionIdMask = (1u << ConnectionIdBits) - 1;
    internal const int NativeSmallMagnitudeBits = 59;
    internal const long NativeSmallMinimum = -(1L << NativeSmallMagnitudeBits);
    internal const long NativeSmallMaximum = (1L << NativeSmallMagnitudeBits) - 1;
}
