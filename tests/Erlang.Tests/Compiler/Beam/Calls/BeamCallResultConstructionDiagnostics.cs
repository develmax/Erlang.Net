namespace Erlang.Tests;

internal static class BeamCallResultConstructionDiagnostics
{
    internal const string StoreOrderMismatch =
        "A call result was not rooted through an independent constructor or stored before a dependent constructor.";

    internal const string NullaryArgumentOrderMismatch =
        "A saved call result was not kept in X0 while preparing a nullary argument.";
}
