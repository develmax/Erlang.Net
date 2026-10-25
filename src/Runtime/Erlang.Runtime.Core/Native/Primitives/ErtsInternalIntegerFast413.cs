using System.Numerics;

namespace Erlang;

internal static class ErtsInternalIntegerFast413
{
    private static bool Base(Term term, out int radix)
    {
        if (term is Integer value && value.Value >= ErlangDataLimits.MinimumIntegerBase && value.Value <= ErlangDataLimits.MaximumIntegerBase)
        {
            radix = (int)value.Value;

            return true;
        }
        radix = default;

        return false;
    }

    private static bool SmallByte(Term term, out byte character)
    {
        if (term is Integer value && value.Value >= ErlangIntegerParseParameters.MinimumNativeSmall
            && value.Value <= ErlangIntegerParseParameters.MaximumNativeSmall)
        {
            character = (byte)(value.Value & byte.MaxValue);

            return true;
        }
        character = default;

        return false;
    }

    private static int Digit(byte value) => value switch
    {
        >= (byte)'0' and <= (byte)'9' => value - '0',
        >= (byte)'A' and <= (byte)'Z' => value - 'A' + ErlangDataLimits.DecimalIntegerBase,
        >= (byte)'a' and <= (byte)'z' => value - 'a' + ErlangDataLimits.DecimalIntegerBase,
        _ => -1
    };

    internal static Term Binary(ProcessContext context, Term term, Term baseTerm)
    {
        if (!Base(baseTerm, out int radix) || term is not BitString { IsBinary: true } binary)
            return ErtsInternalScalarNativeAtoms413.BadArgument;
        ReadOnlySpan<byte> bytes = binary.ByteSpan;
        if (bytes.IsEmpty)
            return ErtsInternalScalarNativeAtoms413.BadArgument;
        bool negative = bytes[0] == '-';
        if (bytes[0] is (byte)'+' or (byte)'-')
            bytes = bytes[1..];
        if (bytes.IsEmpty)
            return ErtsInternalScalarNativeAtoms413.BadArgument;
        int zeros = 0;
        while (zeros < bytes.Length && bytes[zeros] == '0')
        {
            context.Cancellation.ThrowIfCancellationRequested();
            zeros++;
        }
        bytes = bytes[zeros..];
        if (bytes.IsEmpty)
            return Term.I(0);
        if (bytes.Length > ErlangIntegerParseParameters.DigitsPerSmall[radix - ErlangDataLimits.MinimumIntegerBase])
            return ErtsInternalScalarNativeAtoms413.Big;
        ulong value = 0;
        foreach (byte character in bytes)
        {
            int digit = Digit(character);
            if (digit < 0 || digit >= radix)
                return ErtsInternalScalarNativeAtoms413.BadArgument;
            value = value * (uint)radix + (uint)digit;
        }

        return new Integer(negative ? -(BigInteger)value : value);
    }

    internal static Term List(ProcessContext context, Term original, Term baseTerm)
    {
        if (original is Nil)
            return ErtsInternalScalarNativeAtoms413.NoInteger;
        if (original is not Cons first)
            return ErtsInternalScalarNativeAtoms413.NotList;
        if (!Base(baseTerm, out int radix))
            return ErtsInternalScalarNativeAtoms413.BadArgument;
        bool negative = first.Head is Integer sign && sign.Value == '-';
        Term rest = first.Head is Integer prefix && (prefix.Value == '+' || prefix.Value == '-') ? first.Tail : original;
        bool zeroSeen = false;
        while (rest is Cons zero && SmallByte(zero.Head, out byte character))
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (character != '0')
            {
                int digit = Digit(character);
                if (digit < 0 || digit >= radix)
                    return zeroSeen ? Term.Tuple(Term.I(0), rest) : ErtsInternalScalarNativeAtoms413.NoInteger;
                break;
            }
            zeroSeen = true;
            rest = zero.Tail;
        }
        if (rest is not Cons)
            return zeroSeen ? Term.Tuple(Term.I(0), rest) : ErtsInternalScalarNativeAtoms413.NoInteger;
        int limit = ErlangIntegerParseParameters.DigitsPerSmall[radix - ErlangDataLimits.MinimumIntegerBase];
        int digits = 0;
        ulong value = 0;
        while (digits <= limit && rest is Cons cell && SmallByte(cell.Head, out byte digitCharacter))
        {
            int digit = Digit(digitCharacter);
            if (digit < 0 || digit >= radix)
                break;
            value = value * (uint)radix + (uint)digit;
            digits++;
            rest = cell.Tail;
        }
        if (digits == 0)
            return ErtsInternalScalarNativeAtoms413.NoInteger;
        if (digits > limit)
            return ErtsInternalScalarNativeAtoms413.Big;

        return Term.Tuple(new Integer(negative ? -(BigInteger)value : value), rest);
    }
}
