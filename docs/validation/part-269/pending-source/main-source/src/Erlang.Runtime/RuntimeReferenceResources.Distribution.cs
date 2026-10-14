namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateDistributionEntry(RuntimeDistributionPendingEntry entry)
    {
        if (!ReferenceEquals(entry.Resources, this))
            throw new ArgumentException(DistributionPendingDiagnostics.ResourceOwner, nameof(entry));

        return Create(ReferenceResourceKind.DistributionEntry, entry);
    }

    internal ReferenceResourceLease<RuntimeDistributionPendingEntry>? AcquireDistributionEntry(ReferenceTerm reference)
        => Acquire<RuntimeDistributionPendingEntry>(reference, ReferenceResourceKind.DistributionEntry);
}
