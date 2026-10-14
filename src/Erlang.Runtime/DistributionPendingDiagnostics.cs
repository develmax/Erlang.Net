namespace Erlang;

internal static class DistributionPendingDiagnostics
{
    internal const string Closed = "The runtime pending distribution table is closed";
    internal const string Handle = "The pending distribution handle is foreign, malformed or stale";
    internal const string Node = "A pending distribution node must have a valid remote node identity";
    internal const string Capacity = "The runtime pending distribution node budget is exhausted";
    internal const string AlreadyInitialized = "The runtime pending distribution table is already initialized";
    internal const string Process = "The setup process is not a live owner in this runtime";
    internal const string Kernel = "A live registered kernel setup reservation is required";
    internal const string Controller = "The process or pending entry already has a setup role";
    internal const string ResourceOwner = "The distribution entry belongs to another reference resource owner";
    internal const string Lease = "The pending distribution lease is disposed";
}
