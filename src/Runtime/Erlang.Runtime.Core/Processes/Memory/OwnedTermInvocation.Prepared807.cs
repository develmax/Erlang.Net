namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal static ResourceReferenceTerm AdmitRehydratedPrepared807(
        ResourceReferenceTerm reference,
        PreparedLoaderOwner807 owner,
        AtomicsHostCopyTransaction591? transaction = null
    )
    {
        var invocation = Active;
        if (invocation is { IsDisposed: false, CanBorrow507: true } && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && ReferenceEquals(invocation.Runtime, owner.Runtime807) && ReferenceEquals(invocation.context.CurrentOwned506, invocation)
            && RuntimeActorExecutionTurn.IsOwnedBy(invocation.context))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return PreparedLoaderRoots807.HostCopy807(reference, transaction);
    }
}
