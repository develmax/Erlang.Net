namespace Erlang;

internal sealed partial class OwnedTermInvocation506
{
    internal bool CanBorrow507 { get; }
    internal OwnedBindingKind507 BindingKind507 { get; set; }

    internal bool OwnsContext507(ProcessContext candidate) => ReferenceEquals(context, candidate);

    internal int ResultSlot507 => resultSlot;

    internal void MarkCompilerFactory507()
    {
        if (CanBorrow507)
            BindingKind507 = OwnedBindingKind507.CompilerGuest;
    }

    internal Term CompleteGuest507(Term value)
    {
        if (parent is null || !CanBorrow507)
            return Complete(value);

        return parent.Frame.SetLocalGraph507(destination, value);
    }

    internal Term HoldGuestResult507(Term value) => Frame.SetLocalGraph507(resultSlot, value);

    internal Term AdoptExpression507(Term value)
        => Frame.AdoptGuestInto507(Frame.Reserve(), value);
}
