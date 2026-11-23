namespace Erlang.Tests;

internal static class BeamInPlaceTupleHeapDiagnostics
{
    internal const string HeapOrderMismatch =
        "A heap check was not moved before an in-place tuple projection or was moved across an ordinary call.";
}
