namespace Erlang.Tests;

internal static class SequenceTraceHostParameters
{
    public const string Test = "runtime/seq-trace-concurrent-token-snapshots-tracer-switch-and-self-isolation";
    public const string Failure = "Sequence trace protocol or token ownership contract failed";
    public const string Stop = "seq_trace_host_stop";
    public const int Senders = 8;
    public const int MessagesPerSender = 32;
    public const int LeftTracerLabel = 1000;
    public const int RightTracerLabel = 1001;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
}
