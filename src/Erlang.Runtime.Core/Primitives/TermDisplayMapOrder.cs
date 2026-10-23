namespace Erlang;

internal static class TermDisplayMapOrder
{
    public static IReadOnlyList<KeyValuePair<Term, Term>> Entries(MapTerm map) => map.Entries.Count > TermDisplayParameters.NativeFlatMapMaximumEntries
        ? map.Entries
        : map.Entries.OrderBy(
        entry => entry.Key,
        Comparer<Term>.Create(Compare)
    ).ToArray();

    private static int Compare(Term left, Term right) => left is Atom a && right is Atom b
        ? AtomNameRegistry.IndexOf(a.Name).CompareTo(AtomNameRegistry.IndexOf(b.Name))
        : TermOrder.Compare(left, right, true);
}
