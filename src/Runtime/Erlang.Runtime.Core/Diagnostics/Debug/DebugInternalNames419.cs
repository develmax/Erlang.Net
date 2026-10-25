namespace Erlang;

internal static class DebugInternalNames419
{
    internal const string Module = "erts_debug", Get = "get_internal_state", Set = "set_internal_state", Available = "available_internal_state", Reds = "reds_left", Processes = "processes", NextPid = "next_pid", NextPort = "next_port", Debugger = "debugger_support", ForceFragments = "force_heap_frags", CollisionBonanza = "hashmap_collision_bonanza", LockGraph = "lc_graph", NotSupported = "notsup", InternalHash = "internal_hash", ProcessStatus = "process_status";
    internal const string Foreign = "Internal debug state requires its exact live owning runtime", Unimplemented = "Recognized original internal debug partition has no implemented managed capability";
    internal const string Log = "log", Emulator = "emulator", Tag = "tag", Pid = "pid", Time = "time", GroupLeader = "gl", ErrorLogger = "error_logger", Warning = "warning", Info = "info", Error = "error", WarningMessage = "warning_msg", InfoMessage = "info_msg", System = "system", Format = "~s~n";
    internal const string ProcessPrefix = "Process ", NodePrefix = "on node ", Space = " ", EnableMessage = "enabled access to the emulator internal state.\nNOTE: This is an erts internal test feature and should *only* be used by OTP test-suites.\n";
    internal const string NoLogger = "(no logger present) unexpected logger message: ", Newline = "\n";
    internal const int NativeContextReductions = 4000;
    internal static readonly (string Function, int Arity)[] Signatures = [(Get, 1), (Set, 2)];
}
