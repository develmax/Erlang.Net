using System.Numerics;

namespace Erlang;

internal static class ErlangNumericBifs
{
    public static Term Run(string operation, Term value)
    {
        if (operation == ErlangBasicBifNames.Float)
        {
            if (value is FloatTerm)
                return value;
            if (value is Integer integer && integer.TryToDouble(out double converted))
                return new FloatTerm(converted);

            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (value is Integer number)
            return operation == ErlangBasicBifNames.Absolute && number.Value.Sign < 0 ? new Integer(BigInteger.Abs(number.Value)) : value;
        if (value is not FloatTerm floating)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (operation == ErlangBasicBifNames.Absolute)
            return floating.Value > 0 ? value : new FloatTerm(Math.Abs(floating.Value));
        double result = operation switch
        {
            ErlangBasicBifNames.Round => Math.Round(floating.Value, MidpointRounding.AwayFromZero),
            ErlangBasicBifNames.Truncate => Math.Truncate(floating.Value),
            ErlangBasicBifNames.Floor => Math.Floor(floating.Value),
            ErlangBasicBifNames.Ceiling => Math.Ceiling(floating.Value),
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return new Integer(new BigInteger(result));
    }
}
