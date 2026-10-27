using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Cons : Term
{
    // Two reference fields, matching the portable representation's field count.
    // Owned payloads use the first field as a typed sequence instead of keeping
    // duplicate head/tail references beside the actual slab cells.
    private readonly object headOrOwned;
    private readonly Term? tail;
    internal IOwnedTermSequence506? OwnedStorage506 => headOrOwned as IOwnedTermSequence506;
    public Term Head => headOrOwned is Term value ? value : ((IOwnedTermSequence506)headOrOwned)[0];
    public Term Tail => tail ?? ((IOwnedTermSequence506)headOrOwned)[1];

    public Cons(Term head, Term tail)
    {
        headOrOwned = head;
        this.tail = tail;
    }

    internal Cons(IOwnedTermSequence506 owned) => headOrOwned = owned;

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
