namespace Erlang;

internal static class NativeMapFlatEntries505
{
    internal static IReadOnlyList<KeyValuePair<Term, Term>> Read(MapTerm map, ProcessRuntime? runtime = null)
    {
        if (map.FlatKeys426 is not { } keys)
            throw new ArgumentException(nameof(map));
        if (NativeMapMaterialization505.TryRead(map, runtime, out var saved))
            return saved.PhysicalEntries;
        // Canonical Entries is a stable sort: occurrences of the exact same key
        // object retain their original value order. Consume each occurrence once.
        var values = new Dictionary<Term, Queue<Term>>(ReferenceEqualityComparer.Instance);
        foreach (var entry in map.Entries)
        {
            if (!values.TryGetValue(entry.Key, out var queue))
                values.Add(entry.Key, queue = new());
            queue.Enqueue(entry.Value);
        }
        var physical = new KeyValuePair<Term, Term>[keys.Items.Count];
        for (int index = 0; index < physical.Length; index++)
        {
            Term key = keys.Items[index];
            physical[index] = new(key, values[key].Dequeue());
        }

        return Array.AsReadOnly(physical);
    }
}
