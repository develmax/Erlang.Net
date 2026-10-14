namespace Erlang.Tests;

internal static class BeamGzipHostNames
{
    internal const string Failure = "Gzip loader wrapper source contract failed";
    internal const string Type = "Erlang.BeamLoaderApi";
    internal const string Method = "Decompress";
    internal const int TrailerBytes = 8;
    internal const int IsizeOffsetFromEnd = 4;
    internal const int HeaderBytes = 10;
    internal const int HeaderFlagsOffset = 3;
    internal const byte ReservedFlag = 0x20;
    internal const byte BadChecksumDelta = 1;
}
