namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal static ResourceReferenceTerm AdmitRehydratedBinary598(ResourceReferenceTerm reference, BinaryProgramBacking598 backing,
        AtomicsHostCopyTransaction591? transaction = null)
    {
        var invocation = Active;
        if (invocation is { IsDisposed: false, CanBorrow507: true } && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && ReferenceEquals(invocation.Runtime, backing.Runtime598) && ReferenceEquals(invocation.context.CurrentOwned506, invocation)
            && RuntimeActorExecutionTurn.IsOwnedBy(invocation.context))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return BinaryProgramRoots598.HostCopy598(reference, transaction);
    }
}
