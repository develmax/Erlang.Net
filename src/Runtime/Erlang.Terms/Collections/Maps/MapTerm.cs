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
        IsOrdinaryLogicalMap517 = true;
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
        ArithmeticKeyEntries = HasStrictArithmeticOrder(Entries, ref context)
            ? Entries
            : Array.AsReadOnly(d.OrderBy(
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
            var keys = d.Keys.ToArray();
            // ERTS flatmap_from_validated_list inserts each incoming key by
            // comparing backwards. Non-total fun arithmetic makes the chosen
            // algorithm and operand direction observable.
            for (int index = 1; index < keys.Length; index++)
            {
                Term key = keys[index];
                int hole = index;
                while (hole > 0)
                {
                    Term previous = keys[hole - 1];
                    int comparison = key is Atom first && previous is Atom second
                        ? AtomNameRegistry.IndexOf(first.Name).CompareTo(AtomNameRegistry.IndexOf(second.Name))
                        : TermOrder.CompareSnapshot(
                            key,
                            previous,
                            true,
                            ref context
                        );
                    if (comparison >= 0)
                        break;
                    keys[hole] = previous;
                    hole--;
                }
                keys[hole] = key;
            }
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

    // This boundary already has the source builder/copy's physical order.
    // Re-sorting is not idempotent for admitted non-total local-fun keys.
    internal static MapTerm FromSourceFlatPairs(IReadOnlyList<KeyValuePair<Term, Term>> physical)
    {
        var logical = new MapTerm(physical);
        if (logical.Entries.Count != physical.Count)
            return FromNativeLayout504(physical);

        return new MapTerm(logical, physical);
    }

    private MapTerm(MapTerm logical, IReadOnlyList<KeyValuePair<Term, Term>> physical)
    {
        IsOrdinaryLogicalMap517 = true;
        lookup = logical.lookup;
        Entries = logical.Entries;
        ArithmeticKeyEntries = logical.ArithmeticKeyEntries;
        preservedFlatPairs426 = Array.AsReadOnly(physical.ToArray());
        FlatKeys426 = new TupleTerm(preservedFlatPairs426.Select(pair => pair.Key));
    }

    public Term Get(Term key) => TryGet(key, out var value) ? value! : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

    public bool TryGet(Term key, out Term? value)
    {
        if (nativeLookup505 is { } native)
        {
            var result = native(key);
            value = result.Value;

            return result.Found;
        }

        return lookup.TryGetValue(key, out value);
    }

    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
