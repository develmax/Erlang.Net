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
            if (item is not Integer i || i.Value < 0 || i.Value > 0x10ffff || !Rune.IsValid((int)i.Value))
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            text.Append(new Rune((int)i.Value));
        }
        return text.ToString();
    }
}
