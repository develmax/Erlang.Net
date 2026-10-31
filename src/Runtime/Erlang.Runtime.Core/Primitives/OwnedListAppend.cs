namespace Erlang;

internal static class OwnedListAppend
{
    internal static Term Run(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        // Identical proper-left validation and arbitrary right-tail semantics.
        Term result = Cons.From(Cons.Items(arguments[0]), arguments[1]);
        if (context.CurrentOwned506 is not
            {
                IsDisposed: false, CanBorrow507: true,
                BindingKind507: OwnedBindingKind507.LocalListAppend
            } invocation
            || !invocation.OwnsContext507(context) || !RuntimeActorExecutionTurn.IsOwnedBy(context))
            return result;

        // Existing physical adopter copies fresh prefix headers only. Already
        // owned tail/head graphs retain their actual identity and backing; all
        // foreign/stale validation and 512 publication remain mandatory.
        if (arguments[1] is Nil)
            return invocation.Frame.SetLocalGraph507(invocation.ResultSlot507, arguments[0]);

        return invocation.Frame.AdoptGuestInto507(invocation.ResultSlot507, result);
    }
}
