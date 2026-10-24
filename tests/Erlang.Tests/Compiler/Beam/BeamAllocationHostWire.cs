namespace Erlang.Tests;

internal static class BeamAllocationHostWire
{
    internal const string Mutate = "Mutation";
    internal const string AllocateHeap = "allocate_heap";
    internal const string Failure = "BEAM allocation control mismatch";
    internal const int StackOffset = 1;
    internal const int AllocationCountOffset = 3;
    internal const int FloatKindOffset = 6;
    internal const int FunKindOffset = 8;
    internal const int FunCountOffset = 9;
    internal const int LiveOffset = 10;
    internal const int SignedZero = 1;
    internal const int UnknownKind = 3 << 4;
    internal const int FourEntries = 4 << 4;
    internal const int WordsKind = 0;
}
