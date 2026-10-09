using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class TupleTerm : Term
{
    public IReadOnlyList<Term> Items
    {
        get;
    }
    public TupleTerm(IEnumerable<Term> items) => Items = Array.AsReadOnly(items.ToArray());
    public override string ToString() => "{" + string.Join(',', Items) + "}";
}
