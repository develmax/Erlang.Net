namespace Erlang.Tests;

internal static class BeamCallResultConstructionDiagnostics
{
    internal const string StoreOrderMismatch =
        "A call result was not rooted through an independent constructor or stored before a dependent constructor.";
}
