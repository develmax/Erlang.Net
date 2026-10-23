namespace Erlang;

internal static class BinaryPrimitiveParameters
{
    internal const int BitsPerByte = 8;
    internal const int CopyChunkBytes = 4096;
    internal const int CopyBytesPerReduction = 64;
    internal const int ListCheckpointItems = 64;
    internal const int MaximumBinaryBytes = int.MaxValue / BitsPerByte;
    internal const int MaximumCommonBinaries = 0x00ffffff;
    internal const string BigEndian = "big";
    internal const string LittleEndian = "little";
}
