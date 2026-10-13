namespace Erlang.Tests;

internal static class PortDrainHostParameters
{
    public const string Test = "runtime/port-protocol-close-drains-pending-bytes";
    public const string ProbeMode = "drain-file";
    public const int PayloadBytes = 1024 * 1024;
    public const int HighWatermark = 4 * 1024 * 1024;
    public const int LowWatermark = 2 * 1024 * 1024;
    public const int HeaderBytes = 4;
    public const string Filename = "port-drain.bin";
    public const string Failure = "Port protocol close discarded accepted pipe output";
}
