using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class MapTerm : Term
{
    public IReadOnlyList<KeyValuePair<Term, Term>> Entries
    {
        get;
    }

    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries)
    {
        var d = new Dictionary<Term, Term>();
        foreach (var e in entries)
            d[e.Key] = e.Value;
        Entries = Array.AsReadOnly(d.OrderBy(e => e.Key, Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b, true))).ToArray());
    }

    public Term Get(Term key) => Entries.FirstOrDefault(e => e.Key.Equals(key)).Value ?? throw new ErlangException(Term.Tuple(Term.A(ErlangErrorReasons.BadKey), key));

    public bool TryGet(Term key, out Term? value)
    {
        value = Entries.FirstOrDefault(e => e.Key.Equals(key)).Value;

        return value is not null;
    }

    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
