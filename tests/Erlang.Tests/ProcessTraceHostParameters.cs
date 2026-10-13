namespace Erlang.Tests;

internal static class ProcessTraceHostParameters
{
    public const string Test = "runtime/process-trace-concurrent-admission-barrier-sequence-composition";
    public const int Senders = 8;
    public const int MessagesPerSender = 32;
    public const int CollectorLabel = 97;
    public const int EventsPerMessage = 2;
    public const string Module = "process_trace_host";
    public const string Function = "delayed";
    public const string Snapshot = "snapshot";
    public const string Failure = "trace_host_failure";
    public const string Stop = "stop";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
