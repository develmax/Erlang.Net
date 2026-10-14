namespace Erlang.Tests;

internal static class HaltPlatformHostNames
{
    public const string Test = "runtime/halt-platform-trace-display-monitor-dispatch-quiescence";
    public const string Module = "halt_platform_host";
    public const string HeldCall = "held_call";
    public const string HeldOwner = "held_owner";
    public const string Trace = "trace";
    public const string Pattern = "trace_pattern";
    public const string Display = "process_display";
    public const string Backtrace = "backtrace";
    public const string Monitor = "system_monitor";
    public const string LongQueue = "long_message_queue";
    public const string SetTracer = "system_flag";
    public const string SequentialTracer = "sequential_tracer";
    public const string SequentialSet = "seq_trace";
    public const string Label = "label";
    public const string TokenLabel = "halt_held_token";
    public const string Send = "send";
    public const string Receive = "receive";
    public const string Call = "call";
    public const string Tracer = "tracer";
    public const string ReturnTrace = "return_trace";
    public const string Message = "message";
    public const string Wildcard = "_";
    public const string True = "true";
    public const string RuntimeGate = "gate";
    public const string ProcessTracing = "ProcessTracing";
    public const string SystemMonitoring = "SystemMonitoring";
    public const string TraceSettings = "settings";
    public const string TracePatterns = "patterns";
    public const string SendPattern = "sendPattern";
    public const string ReceivePattern = "receivePattern";
    public const string TraceSend = "traceSend";
    public const string TraceReceive = "traceReceive";
    public const string Notifications = "notifications";
    public const string EventGate = "eventGate";
    public const string Dispatcher = "dispatcher";
    public const string PendingDisplay = "PendingDisplayRequests";
    public const string SequentialState = "SequentialTrace";
    public const string Token = "Token";
    public const string Count = "Count";
    public const string Snapshot = "Snapshot";
    public const string TimerStart = "send_after";
    public const int ImmediateTimer = 0;
    public const string Failure = "Halt platform joint contract failed";
    public const int Status = 41;
    public const int QueueDisabled = 0;
    public const int QueueEnabled = 1;
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static string TokenResetFailure(
        Pid caller,
        object token,
        RuntimeLifecycleState state,
        bool alive
    ) => Failure + ": caller=" + caller + ", token=" + token + ", lifecycle=" + state + ", alive=" + alive;

    public static string CallerDiedBeforeElection(Pid caller, Term? reason, Exception? exception) => Failure + ": caller already dead before election: " + caller + ", reason=" + reason + ", exception=" + exception;
}
