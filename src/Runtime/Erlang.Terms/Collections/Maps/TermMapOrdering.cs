namespace Erlang;

internal static class TermMapOrdering
{
    internal static IEnumerable<TermComparisonStep> FlatSteps(MapTerm left, MapTerm right, bool exact)
    {
        IReadOnlyList<KeyValuePair<Term, Term>> first = left.FlatPairs();
        IReadOnlyList<KeyValuePair<Term, Term>> second = right.FlatPairs();
        for (int index = 0; index < first.Count; index++)
            yield return TermComparisonStep.Pair(first[index].Key, second[index].Key, true);
        for (int index = 0; index < first.Count; index++)
            yield return TermComparisonStep.Pair(first[index].Value, second[index].Value, exact);
    }

    // utils.c HASHMAP_PHASE1_* / HASHMAP_PHASE2_*: traverse actual leaves.
    // Sorting a logical dictionary cannot preserve stale paths or equal-order
    // collision keys. Observe nested comparisons through the iterative engine.
    internal static IEnumerable<TermComparisonStep> HashSteps(
        MapTerm left,
        MapTerm right,
        bool exact,
        ITermMapOrderingLayout layout
    )
    {
        var first = layout.ReadHashEntries(left);
        var second = layout.ReadHashEntries(right);
        if (first.Count != left.Entries.Count || second.Count != right.Entries.Count)
            throw new InvalidOperationException(TermMapComparisonNames.InvalidLayout);
        int one = 0;
        int two = 0;
        int result = 0;
        Term? minimum = null;
        var observed = new TermComparisonResult();
        while (one < first.Count)
        {
            yield return TermComparisonStep.Observe(
                first[one].Key,
                second[two].Key,
                true,
                TermComparisonPurpose.Ordering,
                observed
            );
            if (observed.Value != 0)
            {
                int hashOrder = layout.CompareHashKeys(first[one].Key, second[two].Key);
                if (hashOrder == 0)
                    hashOrder = observed.Value;
                if (hashOrder < 0)
                {
                    minimum = first[one++].Key;
                    result = -1;
                }
                else
                {
                    minimum = second[two++].Key;
                    result = 1;
                }
                break;
            }
            bool compareValue = result == 0;
            if (!compareValue)
            {
                yield return TermComparisonStep.Observe(
                    first[one].Key,
                    minimum!,
                    true,
                    TermComparisonPurpose.Ordering,
                    observed
                );
                compareValue = observed.Value < 0;
            }
            if (compareValue)
            {
                yield return TermComparisonStep.Observe(
                    first[one].Value,
                    second[two].Value,
                    exact,
                    TermComparisonPurpose.Ordering,
                    observed
                );
                if (observed.Value != 0)
                {
                    result = observed.Value;
                    minimum = first[one].Key;
                }
            }
            one++;
            two++;
        }
        while (one < first.Count || two < second.Count)
        {
            int keyOrder = 0;
            if (one < first.Count && two < second.Count)
            {
                yield return TermComparisonStep.Observe(
                    first[one].Key,
                    second[two].Key,
                    true,
                    TermComparisonPurpose.Ordering,
                    observed
                );
                keyOrder = observed.Value;
                if (keyOrder == 0)
                {
                    one++;
                    two++;
                    continue;
                }
            }
            int hashOrder = one == first.Count ? 1 : two == second.Count ? -1
                : layout.CompareHashKeys(first[one].Key, second[two].Key);
            if (hashOrder == 0)
                hashOrder = keyOrder;
            Term key = hashOrder < 0 ? first[one++].Key : second[two++].Key;
            yield return TermComparisonStep.Observe(
                key,
                minimum!,
                true,
                TermComparisonPurpose.Ordering,
                observed
            );
            if (observed.Value < 0)
            {
                minimum = key;
                result = hashOrder < 0 ? -1 : 1;
            }
        }
        yield return TermComparisonStep.Scalar(result);
    }

    internal static IEnumerable<TermComparisonStep> Steps(MapTerm left, MapTerm right, bool exact = false)
    {
        IReadOnlyList<KeyValuePair<Term, Term>> first = left.ArithmeticKeyEntries;
        IReadOnlyList<KeyValuePair<Term, Term>> second = right.ArithmeticKeyEntries;
        for (int index = 0; index < first.Count; index++)
            yield return TermComparisonStep.Pair(first[index].Key, second[index].Key, true);
        for (int index = 0; index < first.Count; index++)
            yield return TermComparisonStep.Pair(first[index].Value, second[index].Value, exact);
    }
}
