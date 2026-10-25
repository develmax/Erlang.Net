namespace Erlang;

internal static class DebugDirtyNames424
{
    internal const string Module = "erts_debug";
    internal const string Cpu = "dirty_cpu";
    internal const string Io = "dirty_io";
    internal const string Dispatch = "dirty";
    internal const string Normal = "normal";
    internal const string Scheduler = "scheduler";
    internal const string Type = "type";
    internal const string Error = "error";
    internal const string Copy = "copy";
    internal const string Send = "send";
    internal const string Wait = "wait";
    internal const string Reschedule = "reschedule";
    internal const string ReadyWaitDone = "ready_wait6_done";
    internal const string AliveWait = "alive_waitexiting";
    internal const string AliveWaitOnly = "alive_waitexitingonly";
    internal const string Ready = "ready";
    internal const string Done = "done";
    internal const string Alive = "alive";
    internal const string Ok = "ok";
    internal const string NotSupported = "notsup";
    internal const string Foreign = "Dirty debug entries require their exact live runtime owner";
    internal const string Arguments = "Dirty debug native entry received an inconsistent native argument count";
    internal const string NormalScheduler = "Dirty debug normal continuation requires the actual owning actor scheduler";
    internal const string Conflict = "Dirty debug native publication conflicts with an existing entry";
    internal const int Copies = 1000;
    internal const int ReadyMilliseconds = 6000;
    internal const int ExitPollMilliseconds = 100;
    internal const int PostExitMilliseconds = 1000;
    internal const int PostExitWords = 9999;
    internal const ulong PostExitDigit = 4711;
    internal static readonly (string Function, int Arity)[] Signatures = [(Cpu, 2), (Io, 2), (Dispatch, 3)];
}
