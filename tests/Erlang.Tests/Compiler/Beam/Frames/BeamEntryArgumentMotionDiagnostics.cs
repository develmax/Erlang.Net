namespace Erlang.Tests;

internal static class BeamEntryArgumentMotionDiagnostics
{
    internal const string MissingMotion = "Expected the outgoing argument to move before the pure constructor block.";
    internal const string UnsafeMotion = "The outgoing argument moved across garbage collection or an access to its destination.";
}
