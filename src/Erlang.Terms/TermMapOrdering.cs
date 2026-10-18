namespace Erlang;

internal static class TermMapOrdering
{
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
