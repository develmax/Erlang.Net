namespace Erlang.NifNativeBackend;

public static class SchedulerWallTimeCaseNames
{
    public const string References = "runtime/statistics-walltime-public-nested-owner-and-exit";
    public const string Concurrent = "runtime/statistics-walltime-concurrent-owner-linearization";
    public const string Dirty = "runtime/statistics-walltime-real-dirty-inflight-and-persistent-timers";
}
