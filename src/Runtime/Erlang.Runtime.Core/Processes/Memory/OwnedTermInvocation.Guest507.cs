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

    // Actual resumed VM payload stays alive through a real callable import,
    // including dynamic native captures without any module-literal bundle.
    // This is passive value custody; source invocation admission remains later.
    internal Term HoldGuestResultWithLiteral801(Term value, ModuleLiteralRoots801? carriedLiteral801)
    {
        Frame.SetPreparedWithLiteral801(resultSlot, value, carriedLiteral801);

        return Frame.Read(resultSlot);
    }

    internal Term AdoptExpression507(Term value)
        => Frame.AdoptExpression511(value);
}
