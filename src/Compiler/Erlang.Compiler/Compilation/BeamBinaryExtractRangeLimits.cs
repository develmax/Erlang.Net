namespace Erlang.Compiler;

internal static class BeamBinaryExtractRangeLimits
{
    internal const int MaximumInferredSegmentBits = 64;
    private const int EncodedBoundMagnitudeBits = 59;
    internal const long MaximumEncodedInteger = (1L << EncodedBoundMagnitudeBits) - 1;
    internal const long MinimumEncodedInteger = -(1L << EncodedBoundMagnitudeBits);
}
