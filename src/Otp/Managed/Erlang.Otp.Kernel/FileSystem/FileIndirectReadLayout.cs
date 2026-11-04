namespace Erlang;

internal static class FileIndirectReadLayout
{
    internal const int WordSizeBytes = sizeof(uint);
    internal const int HeaderWordCount = 2;
    internal const int HeaderSizeBytes = HeaderWordCount * WordSizeBytes;
    internal const int MaximumSizeBits = 31;
    internal const long SizeExclusiveLimit = 1L << MaximumSizeBits;
    internal const long MaximumReadSize = SizeExclusiveLimit - 1;
}
