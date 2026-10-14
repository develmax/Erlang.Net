namespace Erlang.Tests;

internal static class BeamGzipHostNames
{
    internal const string Stored = "stored";
    internal const string Fixed = "fixed";
    internal const string Dynamic = "dynamic";
    internal const string GzipExtension = ".gzip";
    internal const string RawExtension = ".raw";
    internal const string OptionalHeader = "optional-header.gzip";
    internal const string ValidHeaderChecksum = "fhcrc-valid.gzip";
    internal const string InvalidHeaderChecksum = "fhcrc-invalid.gzip";
    internal const string ControlPrefix = ".Resources.BeamGzipControls.";
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
