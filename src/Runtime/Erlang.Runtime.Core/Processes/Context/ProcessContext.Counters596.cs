namespace Erlang;

public sealed partial class ProcessContext
{
    internal ResourceReferenceTerm AdmitCountersProducer596(ResourceReferenceTerm reference, CountersGuestRoot421 backing)
    {
        if (!ReferenceEquals(backing.Runtime596, Runtime) || !ReferenceEquals(backing.Owner, Runtime.CountersOwner421))
            throw new InvalidOperationException(CountersOwnership596Names.Foreign);
        var invocation = CurrentOwned506;
        if (invocation is { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.RuntimeRead535 }
            && ReferenceEquals(invocation, OwnedTermInvocation506.Active)
            && invocation.OwnsContext507(this)
            && RuntimeActorExecutionTurn.IsOwnedBy(this))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return CountersRoots596.HostCopy596(reference);
    }

    internal void RequireCountersReference596(ReferenceTerm reference, CountersGuestRoot421 backing)
    {
        if (!ReferenceEquals(backing.Runtime596, Runtime) || !ReferenceEquals(backing.Owner, Runtime.CountersOwner421)
            || reference is not ResourceReferenceTerm resource
            || !ReferenceEquals(CountersRoots596.Direct596(resource, Runtime), backing))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (resource.OwnedStorage562 is not null || resource.CountersHost596?.Owns596(backing) == true)
            return;

        throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
