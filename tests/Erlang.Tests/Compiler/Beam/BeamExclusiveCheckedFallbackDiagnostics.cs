namespace Erlang.Tests;

internal static class BeamExclusiveCheckedFallbackDiagnostics
{
    internal const string Exclusive = "Expected distinct shape and field failure edges with one shared projection.";
    internal const string Shared = "Expected the existing failure block when its exclusive projection could not be proved.";
    internal const string Source = "The tagged source fallback changed its selected result.";
}
