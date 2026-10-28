namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal static ResourceReferenceTerm AdmitRehydratedAtomics586(ResourceReferenceTerm reference, AtomicsGuestRoot413 backing)
        => AdmitRehydratedAtomicsSelected594(reference, backing, null);

    internal static ResourceReferenceTerm AdmitRehydratedAtomicsSelected594(
        ResourceReferenceTerm reference,
        AtomicsGuestRoot413 backing,
        AtomicsHostCopyTransaction591? transaction
    )
    {
        var invocation = Active;
        if (invocation is { IsDisposed: false, CanBorrow507: true }
            && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && ReferenceEquals(invocation.Runtime, backing.Runtime586)
            && ReferenceEquals(invocation.context.CurrentOwned506, invocation)
            && RuntimeActorExecutionTurn.IsOwnedBy(invocation.context))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return AtomicsRoots586.HostCopy586(reference, transaction);
    }
}
