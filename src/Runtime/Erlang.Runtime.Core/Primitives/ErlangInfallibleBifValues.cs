namespace Erlang;

internal static class ErlangInfallibleBifValues
{
    internal static Term Get(ProcessContext context, Term key)
    {
        OwnedTermInvocation506? current = context.CurrentOwned506;
        if (current is not null && context.Dictionary.TryReadGuest507(
            current,
            key,
            false,
            out Term existing
        ))
            return existing;
        if (current is { IsDisposed: false, CanBorrow507: true, BindingKind507: OwnedBindingKind507.PreparedGuest }
            && current.OwnsContext507(context) && context.OwnedExecutionEnabled683
            && RuntimeActorExecutionTurn.IsOwnedBy(context) && ProcessDictionary.IsDirectKey507(key))
        {
            // The validated canonical opcode narrows only a fresh real child
            // operation. The parent and opaque/public facade keep their authority.
            using OwnedTermInvocation506 operation = context.EnterOwnedInvocation506([key])!;
            operation.BindingKind507 = OwnedBindingKind507.LocalDictionary;
            if (context.Dictionary.TryReadGuest507(
                operation,
                key,
                false,
                out Term value
            ))
                return operation.CompleteGuest507(value);
        }

        return context.Dictionary.GetValueOrDefault(key, Term.A(ProcessDictionaryAtoms.Undefined));
    }

    internal static Term Minimum(Term first, Term second)
        => first.CompareTo(second) > 0 ? second : first;

    internal static Term Maximum(Term first, Term second)
        => first.CompareTo(second) < 0 ? second : first;
}
