namespace Erlang.Tests;

internal static class BeamSharedStackCopyTailsDiagnostics
{
    internal const string Shared = "Expected the original tests and lines to select one shared stack copy and jump.";
    internal const string Stable = "Expected repeated final label ordering to retain the shared stack copy.";
    internal const string Excluded = "Expected different or separately attributed tails to remain distinct.";
}
