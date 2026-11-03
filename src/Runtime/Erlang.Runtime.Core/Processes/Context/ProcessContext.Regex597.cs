namespace Erlang;

public sealed partial class ProcessContext
{
    internal ResourceReferenceTerm AdmitRegexProducer597(ResourceReferenceTerm reference, ReMagicBacking597 backing)
    {
        if (!ReferenceEquals(backing.Runtime597, Runtime)) throw new InvalidOperationException(ReMagicNames597.Invalid);
        var invocation = CurrentOwned506;
        if (invocation is { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.RuntimeRead535 }
            && ReferenceEquals(invocation, OwnedTermInvocation506.Active) && invocation.OwnsContext507(this)
            && RuntimeActorExecutionTurn.IsOwnedBy(this))
            return (ResourceReferenceTerm)invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), reference);

        return ReMagicRoots597.HostCopy597(reference);
    }

    internal void RequireRegexReference597(ReferenceTerm reference, ReMagicBacking597 backing)
    {
        if (!ReferenceEquals(backing.Runtime597, Runtime) || reference is not ResourceReferenceTerm resource
            || !ReferenceEquals(ReMagicRoots597.Direct597(resource, Runtime), backing)
            || (resource.OwnedStorage562 is null && resource.RegexHost597?.Owns(backing) != true))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
}
