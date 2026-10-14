namespace Erlang;

internal static class BeamGzipFormat
{
    internal const int SignatureFirstOffset = 0;
    internal const int SignatureSecondOffset = 1;
    internal const byte SignatureFirst = 0x1f;
    internal const byte SignatureSecond = 0x8b;
    internal const byte CompressionMethod = 8;
    internal const int MethodOffset = 2;
    internal const int FlagsOffset = 3;
    internal const byte ReservedFlags = 0xe0;
    internal const byte HeaderChecksum = 2;
    internal const byte Extra = 4;
    internal const byte Name = 8;
    internal const byte Comment = 16;
    internal const int HeaderBytes = 10;
    internal const int ExtraLengthBytes = 2;
    internal const int HeaderChecksumBytes = 2;
    internal const int TrailerBytes = 8;
    internal const byte Terminator = 0;
}
