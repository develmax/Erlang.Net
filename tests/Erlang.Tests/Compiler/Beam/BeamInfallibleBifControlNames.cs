namespace Erlang.Tests;

internal static class BeamInfallibleBifControlNames
{
    internal const string Failure = "Non-GC infallible BIF contract control failed";
    internal const string Get = "get";
    internal const string Min = "min";
    internal const string Max = "max";
    internal const string Length = "length";
    internal const string TupleSize = "tuple_size";
    internal const string OverrideFailure = "untrusted_bif_override394";
    internal const int UnaryIdentityArity = 1;
    internal const int SameArity = 2;
    internal const int IdentityImportCount = 2;
    internal const int BuiltinImport = 0;
    internal const int SameImport = 1;
    internal const int ErlangAtom = 5;
    internal const int BuiltinAtom = 6;
    internal const int SameModuleAtom = 7;
    internal const int SameFunctionAtom = 8;
    internal const string HeapProperty = "TermHeap506";
    internal const string HeapGateProperty = "Gate";
    internal const string HeapSnapshotProperty = "Snapshot";
    internal const string HeapWordsProperty = "LivePayloadWords";
    internal const string HeapFramesProperty = "RootFrames";
    internal const string HeapBlocksProperty = "Blocks";
    internal const string GenerationsProperty = "GenerationSnapshot508";
    internal const string CollectionsProperty = "CompletedCollections";
    internal const byte Allocate = 12;
    internal const byte Unary = 10;
    internal const byte Binary = 11;
    internal const byte UnaryGc = 124;
    internal const byte Catch = 62;
    internal const byte CatchEnd = 63;
    internal const byte Move = 64;
    internal const byte Label = 1;
    internal const byte Deallocate = 18;
    internal const byte Return = 19;
    internal const byte ExternalTail = 78;
}
