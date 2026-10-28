using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class TupleTerm : Term
{
    private static readonly IReadOnlyList<Term> GlobalEmptyStorage428 = Array.AsReadOnly(Array.Empty<Term>());

    internal bool HasGlobalEmptyStorage428 => ReferenceEquals(Items, GlobalEmptyStorage428);

    public IReadOnlyList<Term> Items
    {
        get;
    }

    internal IOwnedTermSequence506? OwnedStorage506 => Items as IOwnedTermSequence506;

    internal TupleTerm(IOwnedTermSequence506 owned)
    {
        Items = owned;
    }

    public TupleTerm(IEnumerable<Term> items)
    {
        Term[] owned = items.ToArray();
        Items = owned.Length == 0 ? GlobalEmptyStorage428 : Array.AsReadOnly(owned);
    }

    public override string ToString() => "{" + string.Join(',', Items) + "}";
}
