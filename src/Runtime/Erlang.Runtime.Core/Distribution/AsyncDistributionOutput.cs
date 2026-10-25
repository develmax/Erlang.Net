namespace Erlang;

internal sealed class AsyncDistributionOutput(
    RuntimeDistributionPendingEntry entry,
    DistributionConnectedEntry connected,
    uint connectionId,
    DistributionOutputTicket ticket,
    ReferenceResourceLease<RuntimeDistributionPendingEntry> resource,
    HostDistributionStagedOutput staged
)
{
    internal RuntimeDistributionPendingEntry Entry { get; } = entry;
    internal DistributionConnectedEntry Connected { get; } = connected;
    internal uint ConnectionId { get; } = connectionId;
    internal DistributionOutputTicket Ticket { get; } = ticket;
    internal ReferenceResourceLease<RuntimeDistributionPendingEntry> Resource { get; } = resource;
    internal HostDistributionStagedOutput Staged { get; } = staged;
    internal TaskCompletionSource<bool> Acceptance { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}
