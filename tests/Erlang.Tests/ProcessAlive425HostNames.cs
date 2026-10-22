namespace Erlang.Tests;

internal static class ProcessAlive425HostNames
{
    internal const string BindingTest = "runtime/internal-is-alive425-binding-ref-validation";
    internal const string OrderingTest = "runtime/internal-is-alive425-pending-signal-death-order";
    internal const string Runtime = "erlang", ProcessFlag = "process_flag", Scheduler = "scheduler";
    internal const string Module = "erts_internal", Function = "is_process_alive";
    internal const string True = "true", False = "false", Ok = "ok", Kill = "kill", Killed = "killed", BadArgument = "badarg";
    internal const string Failure = "Ordered process-alive native protocol differs";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
