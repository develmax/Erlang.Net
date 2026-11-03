namespace Erlang;

// Only the authenticated copy body creates this destination. Publish completed
// children in its actual registered invocation frame before binding parents or
// canonical function environments. Existing copy traversals decide sharing;
// this destination never memoizes distinct source occurrences.
internal sealed class DebugCopyDestination391
{
    private readonly OwnedTermInvocation506 invocation;
    private readonly HashSet<Term> published = new(ReferenceEqualityComparer.Instance);

    internal DebugCopyDestination391(ProcessContext context, OwnedTermInvocation506 invocation)
    {
        if (!ReferenceEquals(context.CurrentOwned506, invocation) || invocation.IsDisposed
            || invocation.BindingKind507 != OwnedBindingKind507.LocalDebugCopy377
            || !invocation.CanBorrow507 || !invocation.OwnsContext507(context)
            || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);
        this.invocation = invocation;
    }

    internal Term Adopt(Term value)
    {
        if (published.Contains(value)) return value;
        Term owned = invocation.Frame.AdoptGuestInto507(invocation.Frame.Reserve(), value);
        published.Add(owned);

        return owned;
    }
}
