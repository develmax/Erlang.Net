namespace Erlang;

internal static class DistributionSpawnDiagnostics
{
    internal const string TraceNotImplemented = "Connected spawn trace-token transitions have not been implemented.";
    internal const string DuplicateMonitor = "The connected spawn request reuses an active remote monitor reference.";
    internal const string SenderOptionsNotImplemented = "Connected spawn sender priority, alias and asynchronous output options require additional ownership semantics.";
}
