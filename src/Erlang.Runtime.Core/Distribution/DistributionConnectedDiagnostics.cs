namespace Erlang;

internal static class DistributionConnectedDiagnostics
{
    internal const string Kernel = "An admitted distribution kernel is required";
    internal const string Controller = "A live unassigned local controller is required";
    internal const string Pending = "Only the current pending connection can be adopted";
    internal const string Connected = "The current connection is not connected";
    internal const string Flags = "The negotiated distribution flags are unsupported";
    internal const string QueueFull = "The finalized distribution packet queue is full";
}
