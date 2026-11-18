namespace Erlang.Tests;

internal static class BeamLeafFrameDiagnostics
{
    internal const string EntryTestFrame = "Expected entry tests to permit a frameless leaf and preserve the frame across a call.";
    internal const string ListHeapRoots = "A leaf list failed to retain its initialized X roots without a stack frame.";
    internal const string DistinctTupleFallback = "Tuple fallback lost a different record field projection.";
    internal const string CombinedLeafTail = "A leaf local tail call kept a stack frame for its list arguments.";
    internal const string TupleHeapRoots = "A leaf tuple failed to reserve the heap before allocating its field registers.";
    internal const string ListTupleOrder = "Previously canonical list field registers changed before tuple construction.";
    internal const string ArithmeticRoots = "A leaf arithmetic BIF did not retain its roots and preserve the guarded fallback arguments.";
    internal const string HeapBeforeListProjection = "A checked list projection preceded the heap reservation.";
    internal const string SelectFallbackTailTest = "compiler/beam/select-fallback-uses-frameless-external-tail";
    internal const string SelectFallbackTail = "A terminal select fallback kept a frame for its list argument.";
}
