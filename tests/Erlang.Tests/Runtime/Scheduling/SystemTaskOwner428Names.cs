namespace Erlang.Tests;

internal static class SystemTaskOwner428Names
{
    internal const string BindingTest = "runtime/internal-system-task428-binding-role-and-default-preservation";
    internal const string PriorityTest = "runtime/internal-system-task428-real-priority-queues-no-early-reply";
    internal const string DeathTest = "runtime/internal-system-task428-pending-false-before-down";
    internal const string DirtyTest = "runtime/internal-system-task428-real-source-handler-drains-physical-dirty-target";
    internal const string HandlerPrefix = "Erlang.Tests.SystemTaskOwner428.", HandlerBeam = "erts_dirty_process_signal_handler.beam";
    internal const string HandlerHash = "67CF9A522039DD3807C09BCA1603DD06DF04158127EE4FD4EC2AC5D4D60AD58C";
    internal const string Cpu = "Cpu", Alive = "is_process_alive";
    internal const string Failure = "Actual system-task owner protocol failed";
    internal const string Module = "erts_internal", Request = "request_system_task", Check = "check_dirty_process_code", Handle = "dirty_process_handle_signals";
    internal const string Runtime = "erlang", Flag = "process_flag", Scheduler = "scheduler", Priority = "priority", Info = "process_info";
    internal const string Test = "system_task_test", Gc = "garbage_collect", Minor = "minor", Normal = "normal", High = "high", Maximum = "max", Low = "low";
    internal const string Ok = "ok", True = "true", False = "false", NotSupported = "notsup", Kill = "kill", Down = "DOWN", Killed = "killed";
    internal const string Policy = "GarbageCollectionPolicy", MinorField = "minorCollections";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(12);
}
