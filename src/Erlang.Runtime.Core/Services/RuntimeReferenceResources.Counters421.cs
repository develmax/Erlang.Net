namespace Erlang;

public sealed partial class RuntimeReferenceResources
{
    internal ResourceReferenceTerm CreateCounters421(CountersGuestRoot421 root)
    {
        ResourceReferenceTerm reference = Create(ReferenceResourceKind.Counters421, root);
        root.Array.BindIdentity(reference.Words);

        return reference;
    }

    internal ReferenceResourceLease<CountersGuestRoot421>? AcquireCounters421(ReferenceTerm reference)
        => Acquire<CountersGuestRoot421>(reference, ReferenceResourceKind.Counters421);
}
