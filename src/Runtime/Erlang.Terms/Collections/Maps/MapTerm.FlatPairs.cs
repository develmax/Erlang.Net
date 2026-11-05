namespace Erlang;

public sealed partial class MapTerm
{
    internal IReadOnlyList<KeyValuePair<Term, Term>> FlatPairs()
    {
        if (FlatKeys426 is null)
            throw new InvalidOperationException(MapFlatKeyNames426.InvalidPhysicalEntries);
        if (PreservedFlatPairs426 is { } preserved)
            return preserved;
        var occurrences = new Dictionary<Term, Queue<KeyValuePair<Term, Term>>>(ReferenceEqualityComparer.Instance);
        foreach (var pair in ArithmeticKeyEntries)
        {
            if (!occurrences.TryGetValue(pair.Key, out var values))
                occurrences.Add(pair.Key, values = new());
            values.Enqueue(pair);
        }
        var result = new KeyValuePair<Term, Term>[FlatKeys426.Items.Count];
        for (int index = 0; index < result.Length; index++)
        {
            if (!occurrences.TryGetValue(FlatKeys426.Items[index], out var values) || !values.TryDequeue(out result[index]))
                throw new InvalidOperationException(MapFlatKeyNames426.InvalidPhysicalEntries);
        }

        return result;
    }
}
