using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class MapTerm : Term
{
    private readonly Dictionary<Term, Term> lookup;

    public IReadOnlyList<KeyValuePair<Term, Term>> Entries { get; }

    internal IReadOnlyList<KeyValuePair<Term, Term>> ArithmeticKeyEntries { get; }

    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries)
    {
        var d = new Dictionary<Term, Term>();
        foreach (var entry in entries)
            d[entry.Key] = entry.Value;
        lookup = d;
        NodeComparisonContext context = default;
        foreach (var node in NodeIdentityTraversal.KeyNodes(d))
            context.Read(node, node.Snapshot);
        Entries = Array.AsReadOnly(d.OrderBy(
            entry => entry.Key,
            Comparer<Term>.Create((left, right) => TermOrder.CompareCanonicalSnapshot(
                left,
                right,
                ref context
            ))
        ).ToArray());
        // Native flat-map key arrays retain their construction-time order even
        // when an internal node identity later publishes a different epoch.
        // Keep exact code identity for dictionary storage and arithmetic term
        // key order for comparison as separate views of the same bindings.
        ArithmeticKeyEntries = d.Count < 2 || d.Count > TermMapOrderParameters.FlatMapMaximumEntries
            ? Entries
            : Array.AsReadOnly(Entries.OrderBy(
                entry => entry.Key,
                Comparer<Term>.Create((left, right) => TermOrder.CompareSnapshot(
                    left,
                    right,
                    true,
                    ref context
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
