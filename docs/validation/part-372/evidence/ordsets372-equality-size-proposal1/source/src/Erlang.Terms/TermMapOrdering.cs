namespace Erlang;

internal static class TermMapOrdering
{
    internal static IEnumerable<TermComparisonStep> Steps(MapTerm left, MapTerm right, bool exact = false)
    {
        var first = new TermMapOrderingResult();
        var second = new TermMapOrderingResult();
        foreach (var step in Sort(left.Entries, first))
            yield return step;
        foreach (var step in Sort(right.Entries, second))
            yield return step;
        for (int index = 0; index < first.Entries.Length; index++)
            yield return TermComparisonStep.Pair(first.Entries[index].Key, second.Entries[index].Key, true);
        for (int index = 0; index < first.Entries.Length; index++)
            yield return TermComparisonStep.Pair(first.Entries[index].Value, second.Entries[index].Value, exact);
    }

    private static IEnumerable<TermComparisonStep> Sort(
        IReadOnlyList<KeyValuePair<Term, Term>> entries,
        TermMapOrderingResult result
    )
    {
        var source = entries.ToArray();
        var target = new KeyValuePair<Term, Term>[source.Length];
        var capture = new TermComparisonResult();
        for (int width = 1; width < source.Length; width *= 2)
        {
            for (int start = 0; start < source.Length; start += width * 2)
            {
                int left = start;
                int middle = Math.Min(start + width, source.Length);
                int right = middle;
                int end = Math.Min(start + width * 2, source.Length);
                for (int index = left; index < end; index++)
                {
                    if (left == middle)
                        target[index] = source[right++];
                    else if (right == end)
                        target[index] = source[left++];
                    else
                    {
                        yield return TermComparisonStep.Observe(
                            source[left].Key,
                            source[right].Key,
                            true,
                            TermComparisonPurpose.Ordering,
                            capture
                        );
                        target[index] = capture.Value <= 0 ? source[left++] : source[right++];
                    }
                }
            }
            (source, target) = (target, source);
        }
        result.Entries = source;
    }
}
