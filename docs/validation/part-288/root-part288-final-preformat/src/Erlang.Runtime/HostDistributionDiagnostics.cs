namespace Erlang;

internal static class HostDistributionDiagnostics
{
    internal const string OutputCapacity = "The output buffer cannot fit within the configured queue capacity.";
    internal const string Closed = "The HOST distribution channel or lease is closed";
    internal const string Owner = "The HOST distribution controller belongs to another runtime";
    internal const string Controller = "The caller does not control this HOST distribution channel";
    internal const string InputHandler = "The caller is not the HOST distribution input handler";
    internal const string Attached = "The HOST channel or controller is already attached";
    internal const string ForeignHandle = "The HOST distribution handle belongs to another manager";
    internal const string Identity = "The HOST input handler must have a local process identity";
    internal const string BufferSegments = "The HOST distribution buffer exceeds the segment limit";
}
