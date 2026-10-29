namespace Erlang;

internal sealed class DistributionSpawnRequest(
    RuntimeDistributionPendingEntry entry,
    DistributionConnectedEntry connection,
    uint connectionId,
    ProcessContext owner,
    ReferenceTerm reference,
    ProcessSpawnRequestOptions options
)
{
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal DistributionConnectedEntry Connection { get; } = connection;
    internal uint ConnectionId { get; } = connectionId;
    internal ProcessContext Owner { get; } = owner;
    internal ReferenceTerm Reference { get; } = reference;
    internal ProcessSpawnRequestOptions Options { get; } = options;
    internal RuntimeProcessPayload641? MonitorCustody641 { get; init; }
    internal bool Replied { get; set; }
    internal bool Abandoned { get; set; }
    internal Term? OwnerExitReason { get; set; }
    internal RuntimeProcessPayload641? OwnerExitCustody641 { get; set; }
    internal Pid? Child { get; set; }
    internal int ReplyFlags { get; set; }
}
