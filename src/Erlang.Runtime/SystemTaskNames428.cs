namespace Erlang;

internal static class SystemTaskNames428
{
    internal const string SignalIndex = "Actual pending signal admission index is inconsistent";
    internal const string Module = "erts_internal";
    internal const string Request = "request_system_task";
    internal const string CheckDirty = "check_dirty_process_code";
    internal const string HandleSignals = "dirty_process_handle_signals";
    internal const string GarbageCollect = "garbage_collect";
    internal const string CheckCode = "check_process_code";
    internal const string Test = "system_task_test";
    internal const string HandlerModule = "erts_dirty_process_signal_handler";
    internal const string Start = "start";
    internal const string Normal = "normal";
    internal const string Busy = "busy";
    internal const string More = "more";
    internal const string NoProcess = "noproc";
    internal const string DirtyExecution = "dirty_execution";
    internal const string NotSupported = "notsup";
    internal const string System = "system";
    internal const string TaskFailure = "system_task_error";
    internal const string Foreign = "System task primitive belongs to another runtime";
    internal const string Startup = "Original dirty system handler role could not be started";
    internal const string RoleConflict = "Original dirty system handler role already exists";
    internal const string HandlerImage = "0190E2713C41B21D15BBC0B0B6072FEF67C45A5850DED12A9FFA99EBD408DDC1";
    internal const string Minor = "minor";
    internal const string Major = "major";
    internal const string Inherit = "inherit";
    internal const string Maximum = "max";
    internal const string High = "high";
    internal const string Low = "low";
    internal static readonly (string Function, int Arity)[] Signatures =
    [(Request, 3), (Request, 4), (CheckDirty, 2), (HandleSignals, 1), (GarbageCollect, 1)];
    internal const int SignalBudget = 64;
}
