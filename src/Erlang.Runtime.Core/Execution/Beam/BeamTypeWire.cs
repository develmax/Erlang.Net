namespace Erlang;

internal static class BeamTypeWire
{
    internal const string Chunk = "Type";
    internal const int CurrentVersion = 4;
    internal const int PreviousVersion = 3;
    internal const int Atom = 1 << 0;
    internal const int BitString = 1 << 1;
    internal const int Float = 1 << 3;
    internal const int Integer = 1 << 5;
    internal const int Tuple = 1 << 11;
    internal const int Any = (1 << 13) - 1;
    internal const int LowerBound = 1 << 13;
    internal const int UpperBound = 1 << 14;
    internal const int Unit = 1 << 15;
    internal const int MetadataMask = LowerBound | UpperBound | Unit;
    internal const int PreviousUpperUnionMask = 15;
    internal const int PreviousUpperAny = 31;
    internal const int MetadataUpperByteMask = 224;
    internal const int MetadataVersionShift = 1;
    internal const int ByteBits = 8;
    internal const int TypeFlagBytes = 2;
    internal const int BoundBytes = 8;
    internal const int NativeEntryBytes64 = 32;
    internal const int NativeAllocationSanityBytes = 1 << 30;
    internal const int NativeSmallMagnitudeBits64 = 59;
    internal const long DefaultMinimum = 1L << NativeSmallMagnitudeBits64;
    internal const long DefaultMaximum = -(1L << NativeSmallMagnitudeBits64) - 1;
}
