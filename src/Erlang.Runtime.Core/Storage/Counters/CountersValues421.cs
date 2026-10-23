namespace Erlang;

internal static class CountersValues421
{
    internal static ulong Length(Term value)
    {
        if (value is not Integer integer || integer.Value <= 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (integer.Value > ulong.MaxValue) throw new ErlangException(ErlangErrorReasons.SystemLimit);

        return (ulong)integer.Value;
    }

    internal static ulong Index(Term value, ulong length)
    {
        if (value is not Integer integer || integer.Value <= 0 || integer.Value > length)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (ulong)integer.Value - 1;
    }

    internal static long Value(Term value, bool increment)
    {
        if (value is not Integer integer || integer.Value < long.MinValue
            || integer.Value > (increment ? new System.Numerics.BigInteger(ulong.MaxValue) : long.MaxValue))
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return integer.Value < 0 ? (long)integer.Value : unchecked((long)(ulong)integer.Value);
    }
}
