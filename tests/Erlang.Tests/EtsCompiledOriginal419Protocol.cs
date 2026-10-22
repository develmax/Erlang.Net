namespace Erlang.Tests;

internal static class EtsCompiledOriginal419Protocol
{
    internal static int Compare(
        Term original,
        Term actual,
        bool profiles,
        List<object> differences,
        ref int examined
    )
    {
        Term[] left = IoPrintableOriginal404Terms.List(original, out bool lp), right = IoPrintableOriginal404Terms.List(actual, out bool rp);
        int required = profiles ? 5 : EtsCompiledOriginal419Names.OwnRows;
        if (!lp || !rp || left.Length != required || right.Length != required) differences.Add(new { Shape = false, Profiles = profiles, Original = OwnedFunctionOriginal407Terms.Typed(original), Actual = OwnedFunctionOriginal407Terms.Typed(actual) });
        int equal = 0;
        for (int i = 0; i < Math.Max(left.Length, right.Length); i++)
        {
            if (profiles)
            {
                if (i >= left.Length || i >= right.Length || left[i] is not TupleTerm { Items.Count: 3 } a || right[i] is not TupleTerm { Items.Count: 3 } c)
                {
                    differences.Add(new { Profile = i, Shape = false });
                    continue;
                }
                if (!a.Items[0].Equals(c.Items[0]) || !a.Items[1].Equals(c.Items[1])) differences.Add(new { Profile = i, Headers = false });
                equal += Compare(
                    a.Items[2],
                    c.Items[2],
                    false,
                    differences,
                    ref examined
                );
            }
            else
            {
                examined++;
                bool accepted = i < left.Length && i < right.Length && Row(left[i], right[i]);
                if (accepted) equal++;
                else differences.Add(new { Index = i, WholeProtocol = false, Original = i < left.Length ? OwnedFunctionOriginal407Terms.Typed(left[i]) : null, Actual = i < right.Length ? OwnedFunctionOriginal407Terms.Typed(right[i]) : null });
            }
        }

        return equal;
    }

    private static bool Row(Term expected, Term actual)
    {
        if (expected is not TupleTerm { Items.Count: 2 } e || actual is not TupleTerm { Items.Count: 2 } a || !e.Items[0].Equals(a.Items[0])) return false;
        if (e.Items[1] is not TupleTerm { Items.Count: 2 } eo || a.Items[1] is not TupleTerm { Items.Count: 2 } ao || !eo.Items[0].Equals(Term.A(EtsCompiledOriginal419Names.Returned)) || !eo.Items[0].Equals(ao.Items[0])) return false;
        if (eo.Items[1] is not TupleTerm ep || ao.Items[1] is not TupleTerm ap || ep.Items.Count < 4 || ep.Items.Count != ap.Items.Count || !ep.Items[0].Equals(Term.A(EtsCompiledOriginal419Names.OwnState))) return false;
        if (ep.Items[1] is not ReferenceTerm er || ap.Items[1] is not ResourceReferenceTerm { ResourceKind: ReferenceResourceKind.CompiledMatchSpecification419 } ar) return false;

        // This is an explicit relation between each VM's actual opaque resource.
        // No transformed output is created. Every other field and repeated use of
        // the exact resource (including BIF args) is checked recursively.
        return Relation(
            expected,
            actual,
            er,
            ar
        );
    }

    private static bool Relation(
        Term expected,
        Term actual,
        ReferenceTerm original,
        ReferenceTerm managed
    )
    {
        if (expected is ReferenceTerm reference && reference.Equals(original)) return actual is ReferenceTerm ar && ar.Equals(managed);
        if (expected is Cons e) return actual is Cons a && Relation(
            e.Head,
            a.Head,
            original,
            managed
        ) && Relation(
            e.Tail,
            a.Tail,
            original,
            managed
        );
        if (expected is TupleTerm et)
        {
            if (actual is not TupleTerm at || et.Items.Count != at.Items.Count) return false;
            for (int i = 0; i < et.Items.Count; i++) if (!Relation(
                et.Items[i],
                at.Items[i],
                original,
                managed
            )) return false;

            return true;
        }

        return expected.Equals(actual);
    }
}
