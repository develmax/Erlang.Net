using Erlang;

internal static class StringResourceComparison
{
    private const string Bindings = "compiled_pattern_bindings";

    internal static bool ControlsRejectAlterations(TupleTerm expected, TupleTerm actual)
    {
        if (actual.Items.Count != 5)
        {
            return true;
        }
        var proof = (TupleTerm)actual.Items[4];
        var binding = (TupleTerm)((Cons)proof.Items[1]).Head;

        Term Outcome(Term stack, Term witness) => Term.Tuple(
            actual.Items[0],
            actual.Items[1],
            actual.Items[2],
            stack,
            witness
        );
        var removedStack = (TupleTerm)Outcome(Nil.Value, proof);
        var removedBindings = (TupleTerm)Outcome(actual.Items[3], Term.Tuple(proof.Items[0], Nil.Value));
        var changedQueries = (TupleTerm)Outcome(actual.Items[3], Term.Tuple(proof.Items[0], Term.List(Term.Tuple(binding.Items[0], Nil.Value))));
        var invalidResource = (TupleTerm)Outcome(actual.Items[3], Term.Tuple(proof.Items[0], Term.List(Term.Tuple(Term.Tuple(Term.A("bm"), Term.I(0)), binding.Items[1]))));

        return !StacksEqual(expected, removedStack) && !StacksEqual(expected, removedBindings)
            && !StacksEqual(expected, changedQueries) && !StacksEqual(expected, invalidResource);
    }

    internal static bool StacksEqual(TupleTerm expected, TupleTerm actual)
    {
        if (expected.Items.Count != actual.Items.Count)
        {
            return false;
        }
        if (expected.Items.Count == 4)
        {
            return expected.Items[3].Equals(actual.Items[3]);
        }
        if (expected.Items.Count != 5 || expected.Items[4] is not TupleTerm left || actual.Items[4] is not TupleTerm right
            || left.Items.Count != 2 || right.Items.Count != 2 || !left.Items[0].Equals(Term.A(Bindings)) || !left.Items[0].Equals(right.Items[0]))
        {
            return false;
        }
        Term[] leftBindings = Cons.Items(left.Items[1]).ToArray();
        Term[] rightBindings = Cons.Items(right.Items[1]).ToArray();
        if (leftBindings.Length == 0 || leftBindings.Length != rightBindings.Length)
        {
            return false;
        }
        var mapping = new Dictionary<Term, Term>();
        var used = new HashSet<Term>();
        for (int index = 0; index < leftBindings.Length; index++)
        {
            if (leftBindings[index] is not TupleTerm a || rightBindings[index] is not TupleTerm b || a.Items.Count != 2 || b.Items.Count != 2
                || a.Items[0] is not TupleTerm pa || b.Items[0] is not TupleTerm pb || pa.Items.Count != 2 || pb.Items.Count != 2
                || pa.Items[0] is not Atom tag || tag.Name is not ("bm" or "ac") || !pa.Items[0].Equals(pb.Items[0])
                || pa.Items[1] is not ReferenceTerm || pb.Items[1] is not ReferenceTerm || !a.Items[1].Equals(b.Items[1])
                || !mapping.TryAdd(pa, pb) || !used.Add(pb))
            {
                return false;
            }
        }

        return Equal(expected.Items[3], actual.Items[3], mapping);
    }

    private static bool Equal(Term left, Term right, Dictionary<Term, Term> mapping)
    {
        if (mapping.TryGetValue(left, out Term? bound))
        {
            return bound.Equals(right);
        }
        if (left is Cons a && right is Cons b)
        {
            return Equal(a.Head, b.Head, mapping) && Equal(a.Tail, b.Tail, mapping);
        }
        if (left is TupleTerm ta && right is TupleTerm tb)
        {
            return ta.Items.Count == tb.Items.Count && ta.Items.Zip(tb.Items).All(pair => Equal(pair.First, pair.Second, mapping));
        }

        return left.Equals(right);
    }
}
