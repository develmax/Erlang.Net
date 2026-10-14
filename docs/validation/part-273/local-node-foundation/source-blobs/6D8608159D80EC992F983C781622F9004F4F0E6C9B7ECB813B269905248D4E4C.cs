using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class MapTerm : Term
{
    private readonly Dictionary<Term, Term> lookup;

    private readonly LocalNodeIdentity[] keyNodes;
    private readonly object? orderGate;
    private IReadOnlyList<KeyValuePair<Term, Term>> sortedEntries;
    private LocalNodeSnapshot[] sortedSnapshots = [];
    internal IReadOnlyList<KeyValuePair<Term, Term>> UnorderedEntries { get; }

    public IReadOnlyList<KeyValuePair<Term, Term>> Entries
    {
        get
        {
            NodeComparisonContext context = default;

            return OrderedEntries(ref context);
        }
    }

    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries)
    {
        var d = new Dictionary<Term, Term>();
        foreach (var e in entries)
            d[e.Key] = e.Value;
        lookup = d;
        keyNodes = NodeIdentityTraversal.KeyNodes(d);
        if (keyNodes.Length == 0)
        {
            sortedEntries = Array.AsReadOnly(d.OrderBy(entry => entry.Key, Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, true))).ToArray());
            UnorderedEntries = sortedEntries;
        }
        else
        {
            orderGate = new();
            UnorderedEntries = Array.AsReadOnly(d.ToArray());
            sortedEntries = UnorderedEntries;
            NodeComparisonContext context = default;
            sortedEntries = SortEntries(ref context);
            sortedSnapshots = keyNodes.Select(node => context.Read(node, node.Snapshot)).ToArray();
        }
    }

    internal IReadOnlyList<KeyValuePair<Term, Term>> OrderedEntries(ref NodeComparisonContext context)
    {
        if (keyNodes.Length == 0)
            return sortedEntries;
        lock (orderGate!)
        {
            bool changed = false;
            for (int index = 0; index < keyNodes.Length; index++)
                changed |= context.Read(keyNodes[index], keyNodes[index].Snapshot) != sortedSnapshots[index];
            if (changed)
            {
                var snapshots = new LocalNodeSnapshot[keyNodes.Length];
                for (int index = 0; index < snapshots.Length; index++)
                    snapshots[index] = context.Read(keyNodes[index], keyNodes[index].Snapshot);
                sortedEntries = SortEntries(ref context);
                sortedSnapshots = snapshots;
            }

            return sortedEntries;
        }
    }

    private IReadOnlyList<KeyValuePair<Term, Term>> SortEntries(ref NodeComparisonContext context)
    {
        foreach (var node in keyNodes)
            context.Read(node, node.Snapshot);
        var captured = context;

        return Array.AsReadOnly(UnorderedEntries.OrderBy(
            entry => entry.Key,
            Comparer<Term>.Create((left, right) => TermOrder.CompareSnapshot(
                left,
                right,
                true,
                ref captured
            ))
        ).ToArray());
    }

    public Term Get(Term key) => lookup.TryGetValue(key, out var value) ? value : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

    public bool TryGet(Term key, out Term? value)
    {
        return lookup.TryGetValue(key, out value);
    }

    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
