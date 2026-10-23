using System.Numerics;
namespace Erlang;

internal static class AtomicsValues413
{
    internal static ulong Word(Term value)
    {
        if (value is not Integer integer || integer.Value < 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (integer.Value > ulong.MaxValue) throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return (ulong)integer.Value;
    }

    internal static ulong Index(Term value, ulong length)
    {
        if (value is not Integer integer || integer.Value <= 0 || integer.Value > length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (ulong)integer.Value - 1;
    }

    internal static long Value(Term value, bool signed, bool increment)
    {
        if (value is not Integer integer) throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger min = signed || increment ? long.MinValue : BigInteger.Zero;
        BigInteger max = signed && !increment ? long.MaxValue : ulong.MaxValue;
        if (integer.Value < min || integer.Value > max) throw new ErlangException(ErlangErrorReasons.BadArgument);

        return integer.Value < 0 ? (long)integer.Value : unchecked((long)(ulong)integer.Value);
    }

    internal static Term Term(long bits, bool signed)
        => signed ? Erlang.Term.I(bits) : new Integer(new BigInteger(unchecked((ulong)bits)));
}
