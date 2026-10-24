namespace Erlang.Tests;

internal static class ProcessDisplayHostNames
{
    public const string Test = "runtime/process-display-real-owner-output-death-and-requester-cancellation";
    public const string Failure = "Process display output or owner RPC contract failed";
    public const string Module = "process_display_host";
    public const string Ready = "ready";
    public const string Stop = "stop";
    public const string Label = "display_label";
    public const string Held = "held_owner";
    public const string Caller = "requester_only";
    public const string CurrentInner = "Current function: process_display_host:inner/1\n";
    public const string CallerRun = "Called from: process_display_host:run/1\n";
}
