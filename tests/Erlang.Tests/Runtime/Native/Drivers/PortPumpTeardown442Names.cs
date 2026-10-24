namespace Erlang.Tests;

internal static class PortPumpTeardown442Names
{
    internal const string Test = "runtime/ports442/unreadable-file-reader-cancellation-and-open-fault-control";
    internal const string Options = "Erlang.PortOpenOptions", Process = "Erlang.DescriptorPortResource", Port = "Erlang.RuntimePort";
    internal const string Parse = "Parse", Stop = "Stop", Closed = "Closed", Pump = "Pump";
    internal const string Folder = "artifacts/port-pump-teardown442", Report = "observations.json", Leaf = "reader.bin", Guid = "N";
    internal const string Failure = "Port pump teardown must acknowledge only a canceled closed port";
    internal const int StreamBufferBytes = 4096;
    internal static readonly int[] PacketWidths = [0, 1];
}
