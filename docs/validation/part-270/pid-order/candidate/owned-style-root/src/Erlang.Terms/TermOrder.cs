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
        NativeRecordTerm => 7,
        MapTerm => 8,
        Nil => 9,
        Cons => 10,
        BitString => 11,
        _ => throw new NotSupportedException()
    };

    public static int Compare(Term a, Term b, bool exact = false)
    {
        int rank = Rank(a).CompareTo(Rank(b));
        if (rank != 0)
            return rank;
        if (ReferenceEquals(a, b))
            return 0;
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
                return PidIdentity(
                    x.Node,
                    x.Creation,
                    x.Id,
                    y.Node,
                    y.Creation,
                    y.Id
                );
            case (ReferenceTerm x, ReferenceTerm y):
                {
                    int node = Codepoints(x.Node, y.Node);
                    if (node != 0)
                        return node;
                    int creation = x.Creation.CompareTo(y.Creation);

                    return creation != 0 ? creation : x.FullId.CompareTo(y.FullId);
                }
            case (PortTerm x, PortTerm y):
                return PortIdentity(
                    x.Node,
                    x.Creation,
                    x.Id,
                    y.Node,
                    y.Creation,
                    y.Id
                );
            case (FunctionTerm x, FunctionTerm y):
                if (x.ExternalIdentity is { } left && y.ExternalIdentity is { } right)
                {
                    int module = Compare(left.Module, right.Module, true);
                    if (module != 0)
                        return module;
                    int function = Compare(left.Function, right.Function, true);

                    return function != 0 ? function : left.Arity.CompareTo(right.Arity);
                }
                if (x.ExternalIdentity is not null || y.ExternalIdentity is not null)
                    return (x.ExternalIdentity is not null).CompareTo(y.ExternalIdentity is not null);
                if (x.LocalIdentity is { } localLeft && y.LocalIdentity is { } localRight)
                {
                    int module = Compare(localLeft.Module, localRight.Module, true);
                    if (module != 0)
                        return module;
                    int index = localLeft.Index.CompareTo(localRight.Index);
                    if (index != 0)
                        return index;
                    int unique = Compare(localLeft.Unique, localRight.Unique, true);
                    if (unique != 0)
                        return unique;
                    int arity = x.Arity.CompareTo(y.Arity);
                    if (arity != 0)
                        return arity;
                    int count = x.Environment.Count.CompareTo(y.Environment.Count);

                    return count != 0 ? count : Sequence(x.Environment, y.Environment, exact);
                }
                if (x.LocalIdentity is not null || y.LocalIdentity is not null)
                    return (x.LocalIdentity is not null).CompareTo(y.LocalIdentity is not null);

                return x.Identity.CompareTo(y.Identity);
            case (TupleTerm x, TupleTerm y):
                {
                    int c = x.Items.Count.CompareTo(y.Items.Count);

                    return c != 0 ? c : Sequence(x.Items, y.Items, exact);
                }
            case (NativeRecordTerm x, NativeRecordTerm y):
                {
                    int c = Compare(x.Module, y.Module, true);
                    if (c != 0)
                        return c;
                    c = Compare(x.Name, y.Name, true);
                    if (c != 0)
                        return c;
                    c = x.IsExported.CompareTo(y.IsExported);
                    if (c != 0)
                        return c;
                    c = x.FieldNames.Count.CompareTo(y.FieldNames.Count);
                    if (c != 0)
                        return c;
                    c = Sequence(x.FieldNames, y.FieldNames, true);

                    return c != 0 ? c : Sequence(x.Values, y.Values, exact);
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

    private static int PidIdentity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = ai.CompareTo(bi);
        if (c != 0)
            return c;
        c = Codepoints(an, bn);

        return c != 0 ? c : ac.CompareTo(bc);
    }

    private static int PortIdentity(
        string an,
        uint ac,
        ulong ai,
        string bn,
        uint bc,
        ulong bi
    )
    {
        int c = Codepoints(an, bn);
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
                h.Add(x.FullId);
                h.Add(x.Creation);
                break;
            case PortTerm x:
                h.Add(x.Node);
                h.Add(x.Id);
                h.Add(x.Creation);
                break;
            case FunctionTerm x:
                if (x.ExternalIdentity is { } identity)
                {
                    h.Add(identity.Module);
                    h.Add(identity.Function);
                    h.Add(identity.Arity);
                }
                else if (x.LocalIdentity is { } local)
                {
                    h.Add(local.Module);
                    h.Add(local.Index);
                    h.Add(local.Unique);
                    h.Add(x.Arity);
                    foreach (var value in x.Environment)
                        h.Add(value);
                }
                else
                {
                    h.Add(x.Identity);
                }
                break;
            case TupleTerm x:
                foreach (var item in x.Items)
                    h.Add(item);
                break;
            case NativeRecordTerm x:
                h.Add(x.Module);
                h.Add(x.Name);
                h.Add(x.IsExported);
                foreach (var field in x.FieldNames)
                    h.Add(field);
                foreach (var value in x.Values)
                    h.Add(value);
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
