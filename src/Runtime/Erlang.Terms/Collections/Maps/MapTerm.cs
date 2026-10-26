using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed partial class MapTerm : Term
{
    private readonly Dictionary<Term, Term> lookup;

    public IReadOnlyList<KeyValuePair<Term, Term>> Entries { get; }

    internal IReadOnlyList<KeyValuePair<Term, Term>> ArithmeticKeyEntries { get; }

    internal TupleTerm? FlatKeys426 { get; }

    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries) : this(entries, null)
    {
    }

    internal MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries, TupleTerm? copiedFlatKeys)
        : this(entries, copiedFlatKeys, false)
    {
    }

    internal MapTerm(
        IEnumerable<KeyValuePair<Term, Term>> entries,
        TupleTerm? copiedFlatKeys,
        bool rebaseCopiedFlatKeys
    )
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
        if (d.Count <= TermMapOrderParameters.FlatMapMaximumEntries)
        {
            var keys = Entries.OrderBy(
                entry => entry.Key,
                Comparer<Term>.Create((left, right) => left is Atom first && right is Atom second
                    ? AtomNameRegistry.IndexOf(first.Name).CompareTo(AtomNameRegistry.IndexOf(second.Name))
                    : TermOrder.CompareSnapshot(
                        left,
                        right,
                        true,
                        ref context
                    ))
            ).Select(entry => entry.Key).ToArray();
            if (copiedFlatKeys is not null && (copiedFlatKeys.Items.Count != keys.Length
                || keys.Where((key, index) => !ReferenceEquals(key, copiedFlatKeys.Items[index])).Any()))
            {
                var supplied = new HashSet<Term>(copiedFlatKeys.Items, ReferenceEqualityComparer.Instance);
                if (!rebaseCopiedFlatKeys || copiedFlatKeys.Items.Count != keys.Length
                    || supplied.Count != keys.Length || keys.Any(key => !supplied.Contains(key)))
                    throw new ArgumentException(MapFlatKeyNames426.InvalidCopiedKeys, nameof(copiedFlatKeys));
                copiedFlatKeys = null;
            }
            FlatKeys426 = copiedFlatKeys ?? new TupleTerm(keys);
        }
        else if (copiedFlatKeys is not null)
            throw new ArgumentException(MapFlatKeyNames426.InvalidCopiedKeys, nameof(copiedFlatKeys));
    }

    public Term Get(Term key) => lookup.TryGetValue(key, out var value) ? value : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

    public bool TryGet(Term key, out Term? value)
    {
        return lookup.TryGetValue(key, out value);
    }

    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
