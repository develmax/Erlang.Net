namespace Erlang;

internal sealed class RuntimeDistributionPendingEntry(
    RuntimeDistributionPending owner,
    RuntimeReferenceResources resources,
    RuntimeDistributionEntryIdentity identity,
    uint connectionId
)
{
    internal RuntimeDistributionPending Owner { get; } = owner;
    internal RuntimeReferenceResources Resources { get; } = resources;
    internal RuntimeDistributionEntryIdentity Identity { get; } = identity;
    internal Atom Node => Identity.Node;
    internal uint ConnectionId { get; set; } = connectionId;
    internal DistributionPendingState State { get; set; } = DistributionPendingState.Idle;
    internal ProcessContext? ReservedController { get; set; }
    internal DistributionConnectedEntry? Connected { get; set; }
    internal ResourceReferenceTerm Reference { get; set; } = null!;
}
