namespace Erlang.Tests;

internal static class StatisticsTrapNativeCaptureNames
{
    internal const string Case = "runtime/statistics-trap/private-installer-primitives-no-publication";
    internal const string Image = "statisticsTrapPrivateImage";
    internal const string Identity = "ExecutionIdentity";
    internal const string Primitives = "NativePrimitives";
    internal const string Capabilities = "OwnedNativeCapabilities507";
    internal const string TryGetValue = "TryGetValue";
    internal const string AtomicsNew = "atomics_new";
    internal const string RuntimeRead = "RuntimeRead535";
    internal const string Module = "erts_internal";
    internal const string Scheduler = "scheduler_wall_time";
    internal const string Failure = "Private original helper primitive capture changed publication or runtime ownership.";
    internal static readonly (string Function, int Arity)[] Required =
    [
        ("open_port", 2), ("port_command", 3), ("port_connect", 2),
        ("port_close", 1), ("port_info", 1), ("port_info", 2), (Scheduler, 1),
        ("is_system_process", 1), ("spawn_system_process", 3),
        ("ets_lookup_binary_info", 2), ("ets_super_user", 1),
        ("ets_raw_first", 1), ("ets_raw_next", 2), (AtomicsNew, 2)
    ];
}
