using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class MatchSpecFlatMapOrder
{
    private static readonly ConditionalWeakTable<MapTerm, ReadOnlyCollection<KeyValuePair<Term, Term>>> entries = new();

    internal static IReadOnlyList<KeyValuePair<Term, Term>> Entries(MapTerm map)
    {
        if (map.Entries.Count > MatchSpecFlatMapParameters.MaximumEntries)
            return map.Entries;

        return entries.GetValue(map, static value => Array.AsReadOnly(value.Entries
            .OrderBy(pair => pair.Key, Comparer<Term>.Create(CompareKeys)).ToArray()));
    }

    private static int CompareKeys(Term left, Term right) => left is Atom a && right is Atom b
        ? AtomNameRegistry.IndexOf(a.Name).CompareTo(AtomNameRegistry.IndexOf(b.Name))
        : TermOrder.Compare(left, right, true);
}
