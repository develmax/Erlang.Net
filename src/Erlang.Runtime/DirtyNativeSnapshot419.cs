namespace Erlang;

internal sealed record DirtyNativeSnapshot419(
    bool Closed,
    int Pending,
    int Running,
    bool CpuAlive,
    bool IoAlive,
    int CpuThread,
    int IoThread,
    bool Joined
);
