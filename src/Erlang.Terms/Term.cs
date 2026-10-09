using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public abstract class Term : IEquatable<Term>, IComparable<Term>
{
    public bool Equals(Term? other) => other is not null && TermOrder.Compare(this, other, true) == 0;
    public override bool Equals(object? other) => other is Term term && Equals(term);
    public int CompareTo(Term? other) => other is null ? 1 : TermOrder.Compare(this, other);
    public bool NumericEquals(Term other) => TermOrder.Compare(this, other) == 0;
    public override int GetHashCode() => TermOrder.Hash(this);
    public static Atom A(string value) => new(value);
    public static Integer I(long value) => new(value);
    public static TupleTerm Tuple(params Term[] items) => new(items);
    public static Term List(params Term[] items) => Cons.From(items);
    public static Term String(string value) => Cons.From(value.EnumerateRunes().Select(r => (Term)new Integer(r.Value)));
    public static string Text(Term value)
    {
        var text = new StringBuilder();
        foreach (var item in Cons.Items(value))
        {
            if (item is not Integer i || i.Value < 0 || i.Value > 0x10ffff || !Rune.IsValid((int)i.Value)) throw new ErlangException("badarg");
            text.Append(new Rune((int)i.Value));
        }
        return text.ToString();
    }
}
public sealed class Atom(string name) : Term
{
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    public override string ToString() => Name.Length > 0 && char.IsLower(Name[0]) && Name.All(c => char.IsLetterOrDigit(c) || c is '_' or '@') ? Name : "'" + Name.Replace("\\", "\\\\").Replace("'", "\\'") + "'";
}
public sealed class Integer(BigInteger value) : Term
{
    public BigInteger Value { get; } = value;
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
public sealed class FloatTerm : Term
{
    public double Value { get; }
    public FloatTerm(double value) { if (!double.IsFinite(value)) throw new ErlangException("badarith"); Value = value; }
    public override string ToString() { var s = Value.ToString("R", CultureInfo.InvariantCulture); return s.Contains('.') || s.Contains('E') ? s : s + ".0"; }
}
public sealed class Nil : Term
{
    private Nil() { }
    public static Nil Value { get; } = new();
    public override string ToString() => "[]";
}
public sealed class Cons(Term head, Term tail) : Term
{
    public Term Head { get; } = head;
    public Term Tail { get; } = tail;
    public static Term From(IEnumerable<Term> items, Term? tail = null)
    {
        var a = items.ToArray(); Term result = tail ?? Nil.Value;
        for (int i = a.Length - 1; i >= 0; i--) result = new Cons(a[i], result);
        return result;
    }
    public static IEnumerable<Term> Items(Term list)
    {
        while (list is Cons cell) { yield return cell.Head; list = cell.Tail; }
        if (list is not Nil) throw new ErlangException("badarg");
    }
    public override string ToString()
    {
        var items = new List<string>(); Term tail = this;
        while (tail is Cons c) { items.Add(c.Head.ToString()!); tail = c.Tail; }
        return "[" + string.Join(',', items) + (tail is Nil ? "" : "|" + tail) + "]";
    }
}
public sealed class TupleTerm : Term
{
    public IReadOnlyList<Term> Items { get; }
    public TupleTerm(IEnumerable<Term> items) => Items = Array.AsReadOnly(items.ToArray());
    public override string ToString() => "{" + string.Join(',', Items) + "}";
}
public sealed class MapTerm : Term
{
    public IReadOnlyList<KeyValuePair<Term, Term>> Entries { get; }
    public MapTerm(IEnumerable<KeyValuePair<Term, Term>> entries)
    {
        var d = new Dictionary<Term, Term>(); foreach (var e in entries) d[e.Key] = e.Value;
        Entries = Array.AsReadOnly(d.OrderBy(e => e.Key, Comparer<Term>.Create((a, b) => TermOrder.Compare(a, b, true))).ToArray());
    }
    public Term Get(Term key) => Entries.FirstOrDefault(e => e.Key.Equals(key)).Value ?? throw new ErlangException(Term.Tuple(Term.A("badkey"), key));
    public override string ToString() => "#{" + string.Join(',', Entries.Select(e => e.Key + "=>" + e.Value)) + "}";
}
public sealed class BitString : Term
{
    private readonly byte[] bytes;
    public int BitLength { get; }
    public bool IsBinary => BitLength % 8 == 0;
    public BitString(ReadOnlySpan<byte> data, int? bitLength = null)
    {
        BitLength = bitLength ?? checked(data.Length * 8);
        if (BitLength < 0 || BitLength > data.Length * 8 || (BitLength + 7) / 8 != data.Length) throw new ErlangException("badarg");
        bytes = data.ToArray(); if (bytes.Length > 0 && BitLength % 8 != 0) bytes[^1] &= (byte)(0xff << (8 - BitLength % 8));
    }
    public byte[] ToArray() => (byte[])bytes.Clone();
    internal int Bit(int i) => (bytes[i / 8] >> (7 - i % 8)) & 1;
    public override string ToString() => IsBinary ? "<<" + string.Join(',', bytes) + ">>" : "<<" + string.Join(',', bytes[..^1].Select(x => x.ToString(CultureInfo.InvariantCulture)).Append($"{bytes[^1] >> (8 - BitLength % 8)}:{BitLength % 8}")) + ">>";
}
public sealed class Pid(string node, ulong id, uint creation = 0) : Term
{
    public string Node { get; } = node;
    public ulong Id { get; } = id;
    public uint Creation { get; } = creation;
    public override string ToString() => $"<{Node}.{Id}.{Creation}>";
}
public sealed class ReferenceTerm(string node, ulong id, uint creation = 0) : Term
{
    public string Node { get; } = node;
    public ulong Id { get; } = id;
    public uint Creation { get; } = creation;
    public override string ToString() => $"#Ref<{Node}.{Id}.{Creation}>";
}
public sealed class PortTerm(string node, ulong id, uint creation = 0) : Term
{
    public string Node { get; } = node;
    public ulong Id { get; } = id;
    public uint Creation { get; } = creation;
    public override string ToString() => $"#Port<{Node}.{Id}.{Creation}>";
}
public interface ITermExecutionContext;
public sealed class FunctionTerm(int arity, Func<ITermExecutionContext, IReadOnlyList<Term>, ValueTask<Term>> invoke) : Term
{
    private static long next;
    internal long Identity { get; } = Interlocked.Increment(ref next);
    public int Arity { get; } = arity;
    public ValueTask<Term> Invoke(ITermExecutionContext context, IReadOnlyList<Term> arguments) => arguments.Count == Arity ? invoke(context, arguments) : throw new ErlangException(Term.Tuple(Term.A("badarity"), Term.Tuple(this, Cons.From(arguments))));
    public override string ToString() => $"#Fun<{Identity}/{Arity}>";
}
public sealed class ErlangException(Term reason, string exceptionClass = "error") : Exception(reason.ToString())
{
    public Term Reason { get; } = reason;
    public string ExceptionClass { get; } = exceptionClass;
    public ErlangException(string reason) : this(Term.A(reason)) { }
}

public static class TermOrder
{
    private static int Rank(Term t) => t switch { Integer or FloatTerm => 0, Atom => 1, ReferenceTerm => 2, FunctionTerm => 3, PortTerm => 4, Pid => 5, TupleTerm => 6, MapTerm => 7, Nil => 8, Cons => 9, BitString => 10, _ => throw new NotSupportedException() };
    public static int Compare(Term a, Term b, bool exact = false)
    {
        int rank = Rank(a).CompareTo(Rank(b)); if (rank != 0) return rank;
        switch (a, b)
        {
            case (Integer x, Integer y): return x.Value.CompareTo(y.Value);
            case (FloatTerm x, FloatTerm y): { int c = x.Value.CompareTo(y.Value); return c != 0 || !exact ? c : BitConverter.DoubleToInt64Bits(x.Value).CompareTo(BitConverter.DoubleToInt64Bits(y.Value)); }
            case (Integer x, FloatTerm y): return exact ? -1 : IntegerFloat(x.Value, y.Value);
            case (FloatTerm x, Integer y): return exact ? 1 : -IntegerFloat(y.Value, x.Value);
            case (Atom x, Atom y): return Codepoints(x.Name, y.Name);
            case (Nil, Nil): return 0;
            case (Pid x, Pid y): return Identity(x.Node, x.Creation, x.Id, y.Node, y.Creation, y.Id);
            case (ReferenceTerm x, ReferenceTerm y): return Identity(x.Node, x.Creation, x.Id, y.Node, y.Creation, y.Id);
            case (PortTerm x, PortTerm y): return Identity(x.Node, x.Creation, x.Id, y.Node, y.Creation, y.Id);
            case (FunctionTerm x, FunctionTerm y): return x.Identity.CompareTo(y.Identity);
            case (TupleTerm x, TupleTerm y): { int c = x.Items.Count.CompareTo(y.Items.Count); return c != 0 ? c : Sequence(x.Items, y.Items, exact); }
            case (MapTerm x, MapTerm y):
                {
                    int c = x.Entries.Count.CompareTo(y.Entries.Count); if (c != 0) return c;
                    c = Sequence(x.Entries.Select(e => e.Key).ToArray(), y.Entries.Select(e => e.Key).ToArray(), true);
                    return c != 0 ? c : Sequence(x.Entries.Select(e => e.Value).ToArray(), y.Entries.Select(e => e.Value).ToArray(), exact);
                }
            case (Cons, Cons):
                while (a is Cons x && b is Cons y) { int c = Compare(x.Head, y.Head, exact); if (c != 0) return c; a = x.Tail; b = y.Tail; }
                return Compare(a, b, exact);
            case (BitString x, BitString y):
                for (int i = 0; i < Math.Min(x.BitLength, y.BitLength); i++) { int c = x.Bit(i).CompareTo(y.Bit(i)); if (c != 0) return c; }
                return x.BitLength.CompareTo(y.BitLength);
            default: throw new NotSupportedException();
        }
    }
    private static int Identity(string an, uint ac, ulong ai, string bn, uint bc, ulong bi) { int c = string.CompareOrdinal(an, bn); if (c != 0) return c; c = ac.CompareTo(bc); return c != 0 ? c : ai.CompareTo(bi); }
    private static int Codepoints(string a, string b)
    { var x = a.EnumerateRunes().GetEnumerator(); var y = b.EnumerateRunes().GetEnumerator(); while (true) { bool ax = x.MoveNext(), by = y.MoveNext(); if (!ax || !by) return ax.CompareTo(by); int c = x.Current.Value.CompareTo(y.Current.Value); if (c != 0) return c; } }
    private static int Sequence(IReadOnlyList<Term> a, IReadOnlyList<Term> b, bool exact) { for (int i = 0; i < a.Count; i++) { int c = Compare(a[i], b[i], exact); if (c != 0) return c; } return 0; }
    // Compare the exact binary rational represented by a double, without rounding a large integer.
    private static int IntegerFloat(BigInteger i, double f)
    {
        long bits = BitConverter.DoubleToInt64Bits(f); int exponent = (int)((bits >> 52) & 2047); long fraction = bits & 0xfffffffffffff;
        BigInteger numerator = exponent == 0 ? fraction : fraction | (1L << 52); int power = (exponent == 0 ? -1022 : exponent - 1023) - 52;
        if (bits < 0) numerator = -numerator;
        return power >= 0 ? i.CompareTo(numerator << power) : (i << -power).CompareTo(numerator);
    }
    public static int Hash(Term t)
    {
        var h = new HashCode(); h.Add(Rank(t));
        switch (t)
        {
            case Integer x: h.Add(0); h.Add(x.Value); break;
            case FloatTerm x: h.Add(1); h.Add(BitConverter.DoubleToInt64Bits(x.Value)); break;
            case Atom x: h.Add(x.Name, StringComparer.Ordinal); break;
            case Pid x: h.Add(x.Node); h.Add(x.Id); h.Add(x.Creation); break;
            case ReferenceTerm x: h.Add(x.Node); h.Add(x.Id); h.Add(x.Creation); break;
            case PortTerm x: h.Add(x.Node); h.Add(x.Id); h.Add(x.Creation); break;
            case FunctionTerm x: h.Add(x.Identity); break;
            case TupleTerm x: foreach (var item in x.Items) h.Add(item); break;
            case MapTerm x: foreach (var item in x.Entries) { h.Add(item.Key); h.Add(item.Value); } break;
            case Cons: while (t is Cons c) { h.Add(c.Head); t = c.Tail; } h.Add(t); break;
            case BitString x: h.Add(x.BitLength); foreach (byte item in x.ToArray()) h.Add(item); break;
        }
        return h.ToHashCode();
    }
}
