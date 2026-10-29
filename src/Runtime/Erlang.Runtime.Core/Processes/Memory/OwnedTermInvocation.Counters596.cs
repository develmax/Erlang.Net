namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal static ResourceReferenceTerm AdmitRehydratedCounters596(ResourceReferenceTerm reference, CountersGuestRoot421 backing)
        => AdmitRehydratedCountersSelected596(reference, backing, null);

    internal static ResourceReferenceTerm AdmitRehydratedCountersSelected596(
        ResourceReferenceTerm reference,
        CountersGuestRoot421 backing,
        AtomicsHostCopyTransaction591? transaction
    )
    {
        var invocation = Active;
        if (invocation is { IsDisposed: false, CanBorrow507: true }
            && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && ReferenceEquals(invocation.Runtime, backing.Runtime596)
            && ReferenceEquals(invocation.context.CurrentOwned506, invocation)
            && RuntimeActorExecutionTurn.IsOwnedBy(invocation.context))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return CountersRoots596.HostCopy596(reference, transaction);
    }
}
