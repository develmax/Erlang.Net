namespace Erlang;

public sealed partial class MapTerm
{
    // Native writable keys may become equal after insertion. Preserve physical
    // cardinality and key order instead of rebuilding a deduplicated dictionary.
    private readonly Func<Term, (bool Found, Term? Value)>? nativeLookup505;
    internal Func<MapTerm, NativeMapEqualityPlan505?>? NativeEquality505 { get; }

    internal static MapTerm FromNativeLayout504(
        IReadOnlyList<KeyValuePair<Term, Term>> physicalEntries,
        Func<Term, (bool Found, Term? Value)>? nativeLookup = null,
        Func<MapTerm, NativeMapEqualityPlan505?>? nativeEquality = null
    ) => new(
        physicalEntries,
        NativeMapLayout504.Preserved,
        nativeLookup,
        nativeEquality
    );

    private MapTerm(
        IReadOnlyList<KeyValuePair<Term, Term>> physicalEntries,
        NativeMapLayout504 layout,
        Func<Term, (bool Found, Term? Value)>? nativeLookup,
        Func<MapTerm, NativeMapEqualityPlan505?>? nativeEquality
    )
    {
        _ = layout;
        nativeLookup505 = nativeLookup;
        NativeEquality505 = nativeEquality;
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
