namespace Erlang;

internal sealed record DirtyNativePoolSnapshot522(
    bool Closed,
    int CpuWorkers,
    int IoWorkers,
    int Pending,
    int Running,
    bool Joined,
    IReadOnlyList<DirtyNativeWorkerSnapshot522> Workers
);
