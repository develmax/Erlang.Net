namespace Erlang.Tests;

internal static class BusyPortMonitorHostNames
{
    internal const string ConfigurationTest = "runtime/system-monitor-busy-port-configuration-and-rollback";
    internal const string SuspensionTest = "runtime/system-monitor-real-busy-port-wait-notification";
    internal const string Failure = "Busy port monitor contract failed";
    internal const string HeldDrain = "held-drain-file";
    internal const string CaptureLeaf = "capture.bin";
    internal const string ReleaseLeaf = "release";
    internal const string Ok = "ok";
    internal const string NoSuspend = "nosuspend";
    internal const int HeaderBytes = 4;
    internal const int PayloadBytes = 4 * 1024 * 1024;
    internal const int LowWatermark = 1;
    internal const int HighWatermark = 2;
    internal const int Marker = 7;
    internal const byte ReadyMarker = 1;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
