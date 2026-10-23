namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateConnectedDistributionHandle(RuntimeDistributionPendingEntry entry)
    {
        if (!ReferenceEquals(entry.Resources, this))
            throw new ArgumentException(DistributionPendingDiagnostics.ResourceOwner, nameof(entry));

        return Create(ReferenceResourceKind.ConnectedDistributionHandle, new DistributionConnectedHandle(entry, entry.ConnectionId));
    }

    internal ReferenceResourceLease<DistributionConnectedHandle>? AcquireConnectedDistributionHandle(ReferenceTerm reference)
        => Acquire<DistributionConnectedHandle>(reference, ReferenceResourceKind.ConnectedDistributionHandle);
}
