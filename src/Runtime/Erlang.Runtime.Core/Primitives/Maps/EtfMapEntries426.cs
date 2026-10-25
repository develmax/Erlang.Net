namespace Erlang;

internal static class EtfMapEntries426
{
    internal static IReadOnlyList<KeyValuePair<Term, Term>> Read(ProcessRuntime runtime, MapTerm map)
    {
        if (map.FlatKeys426 is { } keys)
            return keys.Items.Select(key => new KeyValuePair<Term, Term>(key, map.Get(key))).ToArray();

        // external.c pushes HAMT children in forward order onto its LIFO work
        // stack; wire order is the reverse of the iterator's forward traversal.
        return MapIteration.Entries(map, runtime).Reverse().ToArray();
    }
}
