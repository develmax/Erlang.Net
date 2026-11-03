namespace Erlang;

public sealed partial class ProcessContext
{
    internal ResourceReferenceTerm AdmitAtomicsProducer586(ResourceReferenceTerm reference, AtomicsGuestRoot413 backing)
    {
        if (!ReferenceEquals(backing.Runtime586, Runtime) || !ReferenceEquals(backing.Owner, Runtime.AtomicsOwner413))
            throw new InvalidOperationException(AtomicsOwnership586Names.Foreign);
        var invocation = CurrentOwned506;
        if (invocation is { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.RuntimeRead535 }
            && ReferenceEquals(invocation, OwnedTermInvocation506.Active)
            && invocation.OwnsContext507(this)
            && RuntimeActorExecutionTurn.IsOwnedBy(this))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return AtomicsRoots586.HostCopy586(reference);
    }

    internal void RequireAtomicsReference586(ReferenceTerm reference, AtomicsGuestRoot413 backing)
    {
        if (!ReferenceEquals(backing.Runtime586, Runtime) || !ReferenceEquals(backing.Owner, Runtime.AtomicsOwner413)
            || reference is not ResourceReferenceTerm resource
            || !ReferenceEquals(AtomicsRoots586.Direct586(resource, Runtime), backing))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (resource.OwnedStorage562 is not null || resource.AtomicsHost586?.Owns586(backing) == true)
            return;

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
