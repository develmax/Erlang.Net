namespace Erlang;

public sealed record RuntimeManagedSchedulerWallTimeSample(
    int Lane,
    long ActiveTicks,
    long TotalTicks,
    long OriginTimestamp,
    bool Online
);
