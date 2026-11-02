namespace Erlang;

internal static class RuntimeMicrostateNames
{
    internal const string Scheduler = "scheduler";
    internal const string DirtyCpu = "dirty_cpu_scheduler";
    internal const string DirtyIo = "dirty_io_scheduler";
    internal const string Auxiliary = "aux";
    internal const string Reset = "reset";
    internal const string Type = "type";
    internal const string Identity = "id";
    internal const string Counters = "counters";
    internal const string Unavailable = "Actual OS thread identity unavailable for VM microstate accounting";
    internal const string ThreadMismatch = "Microstate scope must leave on its actual physical thread in reverse order";
    internal static readonly string[] States = ["aux", "check_io", "emulator", "gc", "other", "port", "sleep"];
}
