namespace Erlang;

public sealed partial class MapTerm
{
    // Native writable keys may become equal after insertion. Preserve physical
    // cardinality and key order instead of rebuilding a deduplicated dictionary.
    internal static MapTerm FromNativeLayout504(IReadOnlyList<KeyValuePair<Term, Term>> physicalEntries)
        => new(physicalEntries, NativeMapLayout504.Preserved);

    private MapTerm(IReadOnlyList<KeyValuePair<Term, Term>> physicalEntries, NativeMapLayout504 layout)
    {
        _ = layout;
        var captured = physicalEntries.ToArray();
        lookup = new();
        foreach (var entry in captured)
            lookup.TryAdd(entry.Key, entry.Value);
        NodeComparisonContext context = default;
        foreach (var node in NodeIdentityTraversal.KeyNodes(captured))
            context.Read(node, node.Snapshot);
        Entries = Array.AsReadOnly(captured.OrderBy(
            entry => entry.Key,
            Comparer<Term>.Create((left, right) => TermOrder.CompareCanonicalSnapshot(left, right, ref context))
        ).ToArray());
        ArithmeticKeyEntries = Array.AsReadOnly(captured.OrderBy(
            entry => entry.Key,
            Comparer<Term>.Create((left, right) => TermOrder.CompareSnapshot(
                left,
                right,
                true,
                ref context
            ))
        ).ToArray());
        if (captured.Length <= TermMapOrderParameters.FlatMapMaximumEntries)
            FlatKeys426 = new TupleTerm(captured.Select(entry => entry.Key));
    }
}
