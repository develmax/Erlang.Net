namespace Erlang.Tests;

public static class SystemMonitorHostNames
{
    public const string Test = "system-monitor-concurrent-queue-hysteresis";
    public const string Failure = "System monitor queue or unsupported-effect contract failed.";
    public const int ConcurrentQueues = 32;
    public const int QueueMessages = 4;
    public const int DisableThreshold = 2;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
}
