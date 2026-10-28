namespace Erlang.Tests;

// Finite protocol law only: the six native_errors.Ref argument occurrences.
// No ETF is rewritten, re-encoded, sorted or stripped by this comparator.
internal static class OriginalAtomics590Identity
{
    internal static OriginalAtomics590Result Compare(byte[] original, byte[] managed)
    {
        try
        {
            ReferenceTerm source = Role(ExternalTermFormat.Decode(original));
            ReferenceTerm target = Role(ExternalTermFormat.Decode(managed));
            var a = new OriginalAtomics590Wire(original, source).Read();
            var b = new OriginalAtomics590Wire(managed, target).Read();
            Require(a.Count == OriginalAtomics590Names.ExpectedOwnOccurrences && b.Count == OriginalAtomics590Names.ExpectedOwnOccurrences);
            int ax = 0, bx = 0;
            for (int i = 0; i < a.Count; i++)
            {
                Require(original.AsSpan(ax, a[i] - ax).SequenceEqual(managed.AsSpan(bx, b[i] - bx)));
                ax = a[i] + OriginalAtomics590WireNames.OpaqueReferenceBytes;
                bx = b[i] + OriginalAtomics590WireNames.OpaqueReferenceBytes;
            }
            Require(original.AsSpan(ax).SequenceEqual(managed.AsSpan(bx)));

            return new(
                true,
                a.Count,
                b.Count,
                Describe(source),
                Describe(target),
                null
            );
        }
        catch (Exception error)
        {
            return new(
                false,
                0,
                0,
                null,
                null,
                error.ToString()
            );
        }
    }

    internal static bool SameBinding(OriginalAtomics590Result a, OriginalAtomics590Result b)
        => a.Passed && b.Passed && Same(a.OriginalRole, b.OriginalRole) && Same(a.ManagedRole, b.ManagedRole);

    private static bool Same(OriginalAtomics590Role? a, OriginalAtomics590Role? b)
        => a is not null && b is not null && a.Node == b.Node && a.Creation == b.Creation && a.Words.SequenceEqual(b.Words);

    private static OriginalAtomics590Role Describe(ReferenceTerm r) => new(r.Node, r.Creation, r.Words.ToArray());

    private static ReferenceTerm Role(Term term)
    {
        Require(term is TupleTerm { Items.Count: 2 });
        var outer = (TupleTerm)term;
        Require(outer.Items[1].Equals(Term.A(OriginalAtomics588CaseNames.Normal)) && outer.Items[0] is TupleTerm { Items.Count: 2 });
        var result = (TupleTerm)outer.Items[0];
        Require(result.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.Returned)));
        var fields = Cons.Items(result.Items[1]).OfType<TupleTerm>().Where(f => f.Items.Count == 2 && f.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.NativeErrors))).ToArray();
        Require(fields.Length == 1);
        var errors = Cons.Items(fields[0].Items[1]).ToArray();
        Require(errors.Length == OriginalAtomics590Names.ExpectedNativeErrors);
        string[] names = [OriginalAtomics588CaseNames.GetIndexZero, OriginalAtomics588CaseNames.PutIndexPastEnd, OriginalAtomics588CaseNames.AddWrongIncrement, OriginalAtomics588CaseNames.AddGetWrongIndex, OriginalAtomics588CaseNames.ExchangeWrongValue, OriginalAtomics588CaseNames.CompareWrongValue];
        string[] functions = [OriginalAtomics588Names.Get, OriginalAtomics588Names.Put, OriginalAtomics588Names.Add, OriginalAtomics588Names.AddGet, OriginalAtomics588Names.Exchange, OriginalAtomics588Names.CompareExchange];
        Term[][] extras = [[Term.I(OriginalAtomics590Names.InvalidZeroIndex)],[Term.I(OriginalAtomics590Names.PastEndIndex),Term.I(OriginalAtomics590Names.ZeroReplacement)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom)],
            [Term.A(OriginalAtomics590Names.BadArgumentAtom),Term.I(OriginalAtomics590Names.ZeroReplacement)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom)],[Term.I(OriginalAtomics590Names.ValidIndex),Term.A(OriginalAtomics590Names.BadArgumentAtom),Term.I(OriginalAtomics590Names.ZeroReplacement)]];
        ReferenceTerm? role = null;
        for (int i = 0; i < OriginalAtomics590Names.ExpectedOwnOccurrences; i++)
        {
            Require(errors[i] is TupleTerm { Items.Count: 2 });
            var entry = (TupleTerm)errors[i];
            Require(entry.Items[0].Equals(Term.A(names[i])) && entry.Items[1] is TupleTerm { Items.Count: 4 });
            var outcome = (TupleTerm)entry.Items[1];
            Require(outcome.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.Raised)) && outcome.Items[1].Equals(Term.A(ErlangExceptionClasses.Error))
                && outcome.Items[2].Equals(Term.A(ErlangErrorReasons.BadArgument)) && outcome.Items[3] is Cons);
            var stack = (Cons)outcome.Items[3];
            Require(stack.Head is TupleTerm { Items.Count: 4 });
            var frame = (TupleTerm)stack.Head;
            Require(frame.Items[0].Equals(Term.A(OriginalAtomics588Names.Atomics)) && frame.Items[1].Equals(Term.A(functions[i])));
            var args = Cons.Items(frame.Items[2]).ToArray();
            Require(args.Length == extras[i].Length + 1 && args[0] is ReferenceTerm);
            var current = (ReferenceTerm)args[0];
            ValidateDescriptor(current);
            for (int j = 0; j < extras[i].Length; j++) Require(args[j + 1].Equals(extras[i][j]));
            role ??= current;
            Require(Same(Describe(role), Describe(current)));
        }

        return role!;
    }

    internal static void ValidateDescriptor(ReferenceTerm reference)
    {
        // This job is launched without distribution; NEWER_REFERENCE_EXT emits
        // the actual local three-word magic identity in that own-VM domain.
        Require(reference.Node == OriginalAtomics590Names.LocalNode && reference.Creation == OriginalAtomics590Names.LocalCreation && reference.Words.Count == OriginalAtomics590Names.RoleWordCount
            && reference.Words[0] <= OriginalAtomics590Names.FirstWordMask && (reference.Words[1] & OriginalAtomics590Names.MagicMarker) != 0 && (reference.Words[1] & OriginalAtomics590Names.PidMarker) == 0);
    }

    internal static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(OriginalAtomics590Names.Failure);
    }
}
