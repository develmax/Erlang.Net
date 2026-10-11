using System.Numerics;
using System.Text;
using System.Globalization;

namespace Erlang;

internal static class ErlangIntegerText
{
    public static int Base(IReadOnlyList<Term> arguments)
    {
        if (arguments.Count == 1)
            return ErlangDataLimits.DecimalIntegerBase;
        if (arguments[1] is not Integer radix || radix.Value < ErlangDataLimits.MinimumIntegerBase || radix.Value > ErlangDataLimits.MaximumIntegerBase)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return (int)radix.Value;
    }

    public static string Format(Term term, int radix)
    {
        if (term is not Integer number)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (radix == ErlangDataLimits.DecimalIntegerBase)
            return number.Value.ToString(CultureInfo.InvariantCulture);
        BigInteger value = BigInteger.Abs(number.Value);
        var text = new StringBuilder();
        do
        {
            value = BigInteger.DivRem(value, radix, out BigInteger digit);
            text.Append(ErlangDataLimits.IntegerDigits[(int)digit]);
        } while (value != 0);
        if (number.Value < 0)
            text.Append('-');
        char[] result = text.ToString().ToCharArray();
        Array.Reverse(result);

        return new string(result);
    }

    public static Integer Parse(IEnumerable<int> characters, int radix)
    {
        using var iterator = characters.GetEnumerator();
        if (!iterator.MoveNext())
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        bool negative = iterator.Current == '-';
        if (iterator.Current is '+' or '-')
        {
            if (!iterator.MoveNext())
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        var groups = new List<(BigInteger Value, BigInteger Multiplier)>();
        ulong chunk = 0;
        ulong multiplier = 1;
        int chunkDigits = 0;
        do
        {
            int character = iterator.Current;
            int digit = character switch
            {
                >= '0' and <= '9' => character - '0',
                >= 'A' and <= 'Z' => character - 'A' + ErlangDataLimits.DecimalIntegerBase,
                >= 'a' and <= 'z' => character - 'a' + ErlangDataLimits.DecimalIntegerBase,
                _ => -1
            };
            if (digit < 0 || digit >= radix)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            chunk = chunk * (uint)radix + (uint)digit;
            multiplier *= (uint)radix;
            chunkDigits++;
            if (chunkDigits == ErlangDataLimits.IntegerParsingChunkDigits)
            {
                groups.Add((chunk, multiplier));
                chunk = 0;
                multiplier = 1;
                chunkDigits = 0;
            }
        } while (iterator.MoveNext());
        if (chunkDigits != 0)
            groups.Add((chunk, multiplier));
        while (groups.Count > 1)
        {
            int next = 0;
            for (int index = 0; index < groups.Count; index += 2)
            {
                var left = groups[index];
                if (index + 1 == groups.Count)
                    groups[next++] = left;
                else
                {
                    var right = groups[index + 1];
                    groups[next++] = (left.Value * right.Multiplier + right.Value, left.Multiplier * right.Multiplier);
                }
            }
            groups.RemoveRange(next, groups.Count - next);
        }
        BigInteger value = groups[0].Value;

        return new Integer(negative ? -value : value);
    }

    public static IEnumerable<int> ListCharacters(Term list)
    {
        foreach (Term character in Cons.Items(list))
        {
            if (character is not Integer number || number.Value < 0 || number.Value > ErlangDataLimits.MaximumByte)
                throw new ErlangException(ErlangErrorReasons.BadArgument);

            yield return (int)number.Value;
        }
    }
}
