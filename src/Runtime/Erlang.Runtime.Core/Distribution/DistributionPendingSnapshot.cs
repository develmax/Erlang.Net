namespace Erlang;

public sealed record DistributionPendingSnapshot(
    Atom RemoteNode,
    uint ConnectionId,
    DistributionPendingState State,
    Pid? ReservedController,
    Pid? ReservedKernel
);
