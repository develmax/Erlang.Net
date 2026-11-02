namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal Term CompleteReviewedManagedLibrary789(Term result)
    {
        if (IsDisposed || !CanBorrow507 || BindingKind507 != OwnedBindingKind507.ReviewedManagedLibrary
            || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            throw new InvalidOperationException(OwnedBoundary507Names.PublicationChanged);
        // Adopt only fresh scaffolding; validate and retain actual local children.
        Term actual = Frame.AdoptGuestInto507(ResultSlot507, result);

        return CompleteGuest507(actual);
    }
}
