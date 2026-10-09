using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public static class TermOrder
{
    private static int Rank(Term t) => t switch
    {
        Integer or FloatTerm => 0,
        Atom => 1,
        ReferenceTerm => 2,
        FunctionTerm => 3,
        PortTerm => 4,
        Pid => 5,
        TupleTerm => 6,
        MapTerm => 7,
        Nil => 8,
        Cons => 9,
        BitString => 10,
        _ => throw new NotSupportedException()
    };

    public static int Compare(Term a, Term b, bool exact = false)
    {
        int rank = Rank(a).CompareTo(Rank(b));
        if (rank != 0)
            return rank;
        switch (a, b)
        {
            case (Integer x, Integer y):
                return x.Value.CompareTo(y.Value);
            case (FloatTerm x, FloatTerm y):
                {
                    int c = x.Value.CompareTo(y.Value);

                    return c != 0 || !exact ? c : BitConverter.DoubleToInt64Bits(x.Value).CompareTo(BitConverter.DoubleToInt64Bits(y.Value));
                }
            case (Integer x, FloatTerm y):
                return exact ? -1 : IntegerFloat(x.Value, y.Value);
            case (FloatTerm x, Integer y):
                return exact ? 1 : -IntegerFloat(y.Value, x.Value);
            case (Atom x, Atom y):
                return Codepoints(x.Name, y.Name);
            case (Nil, Nil):
                return 0;
            case (Pid x, Pid y):
                return Identity(
                    x.Node,
                    x.Creation,
                    x.Id,
                    y.Node,
                    y.Creation,
                    y.Id
                );
            case (ReferenceTerm x, ReferenceTerm y):
                return Identity(
                    x.Node,
                    x.Creation,
                    x.Id,
                    y.Node,
                    y.Creation,
                    y.Id
                );
            case (PortTerm x, PortTerm y):
                return Identity(
                    x.Node,
                    x.Creation,
                    x.Id,
                    y.Node,
                    y.Creation,
                    y.Id
                );
            case (FunctionTerm x, FunctionTerm y):
                return x.Identity.CompareTo(y.Identity);
            case (TupleTerm x, TupleTerm y):
                {
                    int c = x.Items.Count.CompareTo(y.Items.Count);

                    return c != 0 ? c : Sequence(x.Items, y.Items, exact);
                }
            case (MapTerm x, MapTerm y):
                {
                    int c = x.Entries.Count.CompareTo(y.Entries.Count);
                    if (c != 0)
                        return c;
                    c = Sequence(x.Entries.Select(e => e.Key).ToArray(), y.Entries.Select(e => e.Key).ToArray(), true);

                    return c != 0 ? c : Sequence(x.Entries.Select(e => e.Value).ToArray(), y.Entries.Select(e => e.Value).ToArray(), exact);
                }
            case (Cons, Cons):
                while (a is Cons x && b is Cons y)
                {
                    int c = Compare(x.Head, y.Head, exact);
                    if (c != 0)
                        return c;
                    a = x.Tail;
                    b = y.Tail;
                }

                return Compare(a, b, exact);
            case (BitString x, BitString y):
                for (int i = 0; i < Math.Min(x.BitLength, y.BitLength); i++)
                {
                    int c = x.Bit(i).CompareTo(y.Bit(i));
                    if (c != 0)
                        return c;
                }

                return x.BitLength.CompareTo(y.BitLength);
            default:
                throw new NotSupportedException();
        }
    }

    private static int Identity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = string.CompareOrdinal(an, bn);
        if (c != 0)
            return c;
        c = ac.CompareTo(bc);

        return c != 0 ? c : ai.CompareTo(bi);
    }

    private static int Codepoints(string a, string b)
    {
        var x = a.EnumerateRunes().GetEnumerator();
        var y = b.EnumerateRunes().GetEnumerator();
        while (true)
        {
            bool ax = x.MoveNext(), by = y.MoveNext();
            if (!ax || !by)
                return ax.CompareTo(by);
            int c = x.Current.Value.CompareTo(y.Current.Value);
            if (c != 0)
                return c;
        }
    }

    private static int Sequence(IReadOnlyList<Term> a, IReadOnlyList<Term> b, bool exact)
    {
        for (int i = 0; i < a.Count; i++)
        {
            int c = Compare(a[i], b[i], exact);
            if (c != 0)
                return c;
        }

        return 0;
    }

    // Compare the exact binary rational represented by a double, without rounding a large integer.
    private static int IntegerFloat(BigInteger i, double f)
    {
        long bits = BitConverter.DoubleToInt64Bits(f);
        int exponent = (int)((bits >> 52) & 2047);
        long fraction = bits & 0xfffffffffffff;
        BigInteger numerator = exponent == 0 ? fraction : fraction | (1L << 52);
        int power = (exponent == 0 ? -1022 : exponent - 1023) - 52;
        if (bits < 0)
            numerator = -numerator;

        return power >= 0 ? i.CompareTo(numerator << power) : (i << -power).CompareTo(numerator);
    }

    public static int Hash(Term t)
    {
        var h = new HashCode();
        h.Add(Rank(t));
        switch (t)
        {
            case Integer x:
                h.Add(0);
                h.Add(x.Value);
                break;
            case FloatTerm x:
                h.Add(1);
                h.Add(BitConverter.DoubleToInt64Bits(x.Value));
                break;
            case Atom x:
                h.Add(x.Name, StringComparer.Ordinal);
                break;
            case Pid x:
                h.Add(x.Node);
                h.Add(x.Id);
                h.Add(x.Creation);
                break;
            case ReferenceTerm x:
                h.Add(x.Node);
                h.Add(x.Id);
                h.Add(x.Creation);
                break;
            case PortTerm x:
                h.Add(x.Node);
                h.Add(x.Id);
                h.Add(x.Creation);
                break;
            case FunctionTerm x:
                h.Add(x.Identity);
                break;
            case TupleTerm x:
                foreach (var item in x.Items)
                    h.Add(item);
                break;
            case MapTerm x:
                foreach (var item in x.Entries)
                {
                    h.Add(item.Key);
                    h.Add(item.Value);
                }
                break;
            case Cons:
                while (t is Cons c)
                {
                    h.Add(c.Head);
                    t = c.Tail;
                }
                h.Add(t);
                break;
            case BitString x:
                h.Add(x.BitLength);
                foreach (byte item in x.ToArray())
                    h.Add(item);
                break;
        }

        return h.ToHashCode();
    }
}
