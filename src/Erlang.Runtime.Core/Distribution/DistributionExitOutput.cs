namespace Erlang;

internal sealed record DistributionExitOutput(
    RuntimeDistributionPendingEntry Entry,
    DistributionConnectedEntry Connection,
    uint ConnectionId,
    TupleTerm Control,
    Pid Sender
);
