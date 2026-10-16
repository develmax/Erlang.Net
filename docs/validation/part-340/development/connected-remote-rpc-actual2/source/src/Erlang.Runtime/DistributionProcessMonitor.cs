namespace Erlang;

internal sealed class DistributionProcessMonitor(
    RuntimeDistributionPendingEntry entry,
    DistributionConnectedEntry connection,
    uint connectionId,
    ProcessContext local,
    Term remote,
    Term watched,
    ReferenceTerm reference,
    bool origin
)
{
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal DistributionConnectedEntry Connection { get; } = connection;
    internal uint ConnectionId { get; } = connectionId;
    internal ProcessContext Local { get; } = local;
    internal Term Remote { get; } = remote;
    internal Term Watched { get; } = watched;
    internal ReferenceTerm Reference { get; } = reference;
    internal bool Origin { get; } = origin;
    internal bool Established { get; set; }
    internal bool SpawnOptions { get; set; }
}
