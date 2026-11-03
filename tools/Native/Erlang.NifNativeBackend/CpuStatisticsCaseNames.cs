namespace Erlang.NifNativeBackend;

public static class CpuStatisticsCaseNames
{
    public const string Isolation = "runtime/statistics-cpu-two-vms-and-host-user-time-isolation";
    public const string InFlight = "runtime/statistics-cpu-active-sample-no-double-count";
    public const string Nested = "runtime/statistics-cpu-nested-vm-and-exception-restoration";
    public const string Auxiliary = "runtime/statistics-cpu-generic-auxiliary-and-no-vm-backlink";
    public const string Dirty = "runtime/statistics-cpu-real-dirty-burn-and-blocked-user-time";
}
