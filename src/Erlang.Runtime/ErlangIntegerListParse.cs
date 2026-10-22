namespace Erlang;

internal static class ErlangIntegerListParse
{
    internal static Integer Parse(Term original, int radix)
    {
        if (original is not Cons first)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool negative = first.Head is Integer sign && sign.Value == '-';
        Term rest = first.Head is Integer prefix && (prefix.Value == '+' || prefix.Value == '-')
            ? first.Tail
            : original;
        bool zeroSeen = false;
        while (rest is Cons zero && TrySmallByte(zero.Head, out byte zeroCharacter) && zeroCharacter == '0')
        {
            zeroSeen = true;
            rest = zero.Tail;
        }
        if (rest is Nil && zeroSeen)
            return Term.I(0);
        int limit = ErlangIntegerParseParameters.DigitsPerSmall[radix - ErlangDataLimits.MinimumIntegerBase];
        int digits = 0;
        ulong value = 0;
        while (rest is Cons cell && TrySmallByte(cell.Head, out byte character))
        {
            int digit = Digit(character);
            if (digit < 0 || digit >= radix)
                break;
            value = value * (uint)radix + (uint)digit;
            digits++;
            rest = cell.Tail;
            if (digits > limit)
            {
                return ErlangIntegerText.Parse(
                    ErlangIntegerText.ListCharacters(original),
                    radix
                );
            }
        }
        if (digits == 0 || rest is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new Integer(negative ? -(System.Numerics.BigInteger)value : value);
    }

    private static bool TrySmallByte(Term term, out byte character)
    {
        if (term is Integer number &&
            number.Value >= ErlangIntegerParseParameters.MinimumNativeSmall &&
            number.Value <= ErlangIntegerParseParameters.MaximumNativeSmall)
        {
            character = (byte)(number.Value & byte.MaxValue);

            return true;
        }
        character = default;

        return false;
    }

    private static int Digit(byte character) => character switch
    {
        >= (byte)'0' and <= (byte)'9' => character - '0',
        >= (byte)'A' and <= (byte)'Z' => character - 'A' + ErlangDataLimits.DecimalIntegerBase,
        >= (byte)'a' and <= (byte)'z' => character - 'a' + ErlangDataLimits.DecimalIntegerBase,
        _ => -1
    };
}
