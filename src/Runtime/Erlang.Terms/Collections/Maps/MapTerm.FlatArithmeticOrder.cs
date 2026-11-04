namespace Erlang;

public sealed partial class MapTerm
{
    private IReadOnlyList<KeyValuePair<Term, Term>>? flatArithmeticPairs;

    internal IReadOnlyList<KeyValuePair<Term, Term>> FlatArithmeticPairs()
    {
        bool cache = OwnedMapPayload517 is null;
        var known = cache ? Volatile.Read(ref flatArithmeticPairs) : null;
        if (known is not null)
            return known;

        if (CanReuseArithmeticFlatPairs())
            return !cache ? ArithmeticKeyEntries
                : Interlocked.CompareExchange(ref flatArithmeticPairs, ArithmeticKeyEntries, null) ?? ArithmeticKeyEntries;

        // utils.c retains the physical numeric/rest partitions. Only atom
        // keys are compared by name instead of their physical atom indexes.
        var pairs = FlatPairs().ToArray();
        int start = 0;
        while (start < pairs.Length)
        {
            if (pairs[start].Key is not Atom)
            {
                start++;
                continue;
            }
            int end = start + 1;
            while (end < pairs.Length && pairs[end].Key is Atom)
                end++;

            var atoms = pairs[start..end].OrderBy(
                pair => pair.Key,
                Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true))
            ).ToArray();
            atoms.CopyTo(pairs, start);
            start = end;
        }
        IReadOnlyList<KeyValuePair<Term, Term>> result = Array.AsReadOnly(pairs);

        return !cache ? result : Interlocked.CompareExchange(ref flatArithmeticPairs, result, null) ?? result;
    }

    private bool CanReuseArithmeticFlatPairs()
    {
        if (FlatKeys426 is not { } keys)
            return false;

        for (int index = 0; index < keys.Items.Count; index++)
        {
            Term physical = keys.Items[index];
            Term ordered = ArithmeticKeyEntries[index].Key;
            if (physical is Atom)
            {
                if (ordered is not Atom)
                    return false;
            }
            else if (!ReferenceEquals(physical, ordered)
                || PreservedFlatPairs426 is { } preserved
                    && !ReferenceEquals(preserved[index].Value, ArithmeticKeyEntries[index].Value))
                return false;
        }

        return true;
    }
}
