using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Cons(Term head, Term tail) : Term
{
    public Term Head { get; } = head;
    public Term Tail { get; } = tail;
    public static Term From(IEnumerable<Term> items, Term? tail = null)
    {
        var a = items.ToArray();
        Term result = tail ?? Nil.Value;
        for (int i = a.Length - 1; i >= 0; i--)
            result = new Cons(a[i], result);
        return result;
    }
    public static IEnumerable<Term> Items(Term list)
    {
        while (list is Cons cell)
        {
            yield return cell.Head;
            list = cell.Tail;
        }
        if (list is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
    }
    public override string ToString()
    {
        var items = new List<string>();
        Term tail = this;
        while (tail is Cons c)
        {
            items.Add(c.Head.ToString()!);
            tail = c.Tail;
        }
        return "[" + string.Join(',', items) + (tail is Nil ? "" : "|" + tail) + "]";
    }
}
