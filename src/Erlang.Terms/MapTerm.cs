using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class MapTerm : Term
{
    private readonly Dictionary<Term, Term> lookup;

    public IReadOnlyList<KeyValuePair<Term, Term>> Entries
    {
        get;
    }

    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries)
    {
        var d = new Dictionary<Term, Term>();
        foreach (var e in entries)
            d[e.Key] = e.Value;
        lookup = d;
        Entries = Array.AsReadOnly(d.OrderBy(e => e.Key, Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b, true))).ToArray());
    }

    public Term Get(Term key) => lookup.TryGetValue(key, out var value) ? value : throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

    public bool TryGet(Term key, out Term? value)
    {
        return lookup.TryGetValue(key, out value);
    }

    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
