namespace Erlang;

internal static class BeamLambdaWire
{
    internal const string Chunk = "FunT";
    internal const int EntryBytes = 24;
    internal const int NativeEntryBytes64 = 32;
    internal const int NativeAllocationSanityBytes = 1 << 30;
}
