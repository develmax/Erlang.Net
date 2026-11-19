namespace Erlang.Tests;

internal static class BeamMatchedCallProjectionDiagnostics
{
    internal const string ProjectionMismatch =
        "A guarded call result projection used the wrong GC roots or cleared a frame slot with a later use.";
}
