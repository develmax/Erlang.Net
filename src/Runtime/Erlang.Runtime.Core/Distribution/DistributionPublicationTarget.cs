namespace Erlang;

internal sealed record DistributionPublicationTarget(
    RuntimeDistributionPendingEntry Entry,
    DistributionConnectedEntry Connection,
    uint ConnectionId,
    ReferenceResourceLease<RuntimeDistributionPendingEntry> Resource
) : IDisposable
{
    public void Dispose() => Resource.Dispose();
}
