using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Otp;

internal static class ProcLibReportCharacters
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static BigInteger Measure(Term characters, Term encoding)
    {
        bool latin1 = encoding is Atom { Name: ProcLibReportAtomNames.Latin1 };
        if (latin1)
        {
            BigInteger size = BigInteger.Zero;
            foreach (Term value in Elements(characters))
            {
                if (value is BitString { IsBinary: true } binary)
                    size += binary.ToArray().Length;
                else if (value is Integer { Value.Sign: >= 0 } integer && integer.Value <= ProcLibReportCharacterParameters.Latin1Maximum)
                    size++;
                else
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }

            return size;
        }

        return StringInfo.ParseCombiningCharacters(FlattenUnicodeCore(characters, true)).Length;
    }

    public static string FlattenUnicode(Term characters)
        => FlattenUnicodeCore(characters, false);

    private static string FlattenUnicodeCore(Term characters, bool protectSurrogates)
    {
        var text = new StringBuilder();
        try
        {
            foreach (Term value in Elements(characters))
            {
                if (value is BitString { IsBinary: true } binary)
                    text.Append(StrictUtf8.GetString(binary.ToArray()));
                else if (value is Integer integer && integer.Value >= 0 && integer.Value <= ProcLibReportCharacterParameters.UnicodeMaximum)
                {
                    int character = (int)integer.Value;
                    // Both ranges have default Other grapheme properties in the pinned OTP data.
                    // Measurement preserves two raw surrogate integers instead of joining UTF-16 pairs.
                    if (protectSurrogates && character is >= ProcLibReportCharacterParameters.SurrogateFirst and <= ProcLibReportCharacterParameters.SurrogateLast)
                        character = ProcLibReportCharacterParameters.SurrogateGraphemeProxyFirst + character - ProcLibReportCharacterParameters.SurrogateFirst;
                    text.Append(character <= char.MaxValue ? ((char)character).ToString() : char.ConvertFromUtf32(character));
                }
                else
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }

            return text.ToString();
        }
        catch (DecoderFallbackException)
        {
            if (protectSurrogates)
            {
                Term remaining = characters;
                while (ProcLibReportUnicodeCharacters.TryRead(remaining, out _, out Term rest))
                    remaining = rest;
            }

            throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
    }

    private static IEnumerable<Term> Elements(Term characters)
    {
        var pending = new Stack<(Term Value, bool Scalar)>();
        pending.Push((characters, false));
        while (pending.TryPop(out var item))
        {
            switch (item.Value)
            {
                case Nil:
                    break;
                case Cons list:
                    pending.Push((list.Tail, false));
                    pending.Push((list.Head, true));
                    break;
                case BitString { IsBinary: true }:
                    yield return item.Value;
                    break;
                case Integer when item.Scalar:
                    yield return item.Value;
                    break;
                default:
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
    }
}
