namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal static ResourceReferenceTerm AdmitRehydratedRegex597(ResourceReferenceTerm reference, ReMagicBacking597 backing,
        AtomicsHostCopyTransaction591? transaction = null)
    {
        var invocation = Active;
        if (invocation is { IsDisposed: false, CanBorrow507: true } && invocation.BindingKind507 != OwnedBindingKind507.Opaque
            && ReferenceEquals(invocation.Runtime, backing.Runtime597) && ReferenceEquals(invocation.context.CurrentOwned506, invocation)
            && RuntimeActorExecutionTurn.IsOwnedBy(invocation.context))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return ReMagicRoots597.HostCopy597(reference, transaction);
    }
}
