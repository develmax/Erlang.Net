namespace Erlang.Tests;

internal static class OriginalAtomics588Cases
{
    internal static bool HasSuccessfulCases(Term term)
    {
        if (term is not TupleTerm { Items.Count: 2 } outer || !outer.Items[1].Equals(Term.A(OriginalAtomics588CaseNames.Normal))
            || outer.Items[0] is not TupleTerm { Items.Count: 2 } result || !result.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.Returned))) return false;
        var fields = new Dictionary<string, Term>(StringComparer.Ordinal);
        Term remaining = result.Items[1];
        while (remaining is Cons c)
        {
            if (c.Head is not TupleTerm { Items.Count: 2 } f || f.Items[0] is not Atom a || !fields.TryAdd(a.Name, f.Items[1])) return false;
            remaining = c.Tail;
        }
        if (remaining is not Nil || !fields.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(
            new[] { OriginalAtomics588CaseNames.WordSize, OriginalAtomics588CaseNames.AtomicsAllSeven, OriginalAtomics588CaseNames.ConstructorErrors, OriginalAtomics588CaseNames.NativeErrors, OriginalAtomics588CaseNames.CountersDefaultAndAtomics })) return false;
        var all = Cons.Items(fields[OriginalAtomics588CaseNames.AtomicsAllSeven]).ToArray();
        if (all.Length != OriginalAtomics588CaseNames.ExpectedAtomicsRows) return false;
        var identities = new HashSet<(System.Numerics.BigInteger, string)>();
        foreach (var item in all)
        {
            if (item is not TupleTerm { Items.Count: 3 } row || row.Items[0] is not Integer n || row.Items[1] is not Atom signed
                || !OriginalAtomics588CaseNames.ValidLengths.Any(x => n.Value == x) || signed.Name is not (ErlangBooleanAtoms.True or ErlangBooleanAtoms.False)
                || !identities.Add((n.Value, signed.Name)) || !Returned(row.Items[2])) return false;
        }
        var counters = Cons.Items(fields[OriginalAtomics588CaseNames.CountersDefaultAndAtomics]).ToArray();
        if (counters.Length != OriginalAtomics588CaseNames.DefaultAndAtomicsRows || counters[0] is not TupleTerm { Items.Count: 2 } first || first.Items[0] is not Nil || !Returned(first.Items[1])
            || counters[1] is not TupleTerm { Items.Count: 2 } second || !second.Items[0].Equals(Term.List(Term.A(OriginalAtomics588CaseNames.AtomicsOption))) || !Returned(second.Items[1])) return false;

        return Raised(
            fields[OriginalAtomics588CaseNames.ConstructorErrors],
            new[] { OriginalAtomics588CaseNames.Zero, OriginalAtomics588CaseNames.Negative, OriginalAtomics588CaseNames.WrongSize, OriginalAtomics588CaseNames.WrongOptions, OriginalAtomics588CaseNames.ImproperOptions, OriginalAtomics588CaseNames.WrongSigned, OriginalAtomics588CaseNames.UnknownOption }
        )
            && Raised(
                fields[OriginalAtomics588CaseNames.NativeErrors],
                new[] { OriginalAtomics588CaseNames.GetIndexZero, OriginalAtomics588CaseNames.PutIndexPastEnd, OriginalAtomics588CaseNames.AddWrongIncrement, OriginalAtomics588CaseNames.AddGetWrongIndex, OriginalAtomics588CaseNames.ExchangeWrongValue, OriginalAtomics588CaseNames.CompareWrongValue, OriginalAtomics588CaseNames.InfoWrongReference }
            );
    }

    private static bool Returned(Term term) => term is TupleTerm { Items.Count: 2 } t && t.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.Returned));

    private static bool Raised(Term term, string[] expected)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        Term remaining = term;
        while (remaining is Cons c)
        {
            if (c.Head is not TupleTerm { Items.Count: 2 } row || row.Items[0] is not Atom a || !seen.Add(a.Name)
                || row.Items[1] is not TupleTerm { Items.Count: 4 } outcome || !outcome.Items[0].Equals(Term.A(OriginalAtomics588CaseNames.Raised))
                || outcome.Items[3] is not Cons) return false;
            remaining = c.Tail;
        }

        return remaining is Nil && seen.SetEquals(expected);
    }
}
