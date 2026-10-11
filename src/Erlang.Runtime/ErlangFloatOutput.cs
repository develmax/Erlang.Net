using System.Globalization;
using System.Text;

namespace Erlang;

internal static class ErlangFloatOutput
{
    public static string Format(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not FloatTerm number || !double.IsFinite(number.Value))
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        string format = ErlangFloatBifNames.Scientific;
        long precision = ListFloatFormat.FractionDigits;
        bool compact = false;
        if (arguments.Count == 2)
        {
            foreach (Term option in Cons.Items(arguments[1]))
            {
                switch (option)
                {
                    case Atom { Name: ErlangFloatBifNames.Compact }:
                        compact = true;
                        break;
                    case Atom { Name: ErlangFloatBifNames.Short }:
                        format = ErlangFloatBifNames.Short;
                        break;
                    case TupleTerm { Items.Count: 2 } tuple when tuple.Items[0] is Atom { Name: ErlangFloatBifNames.Decimals or ErlangFloatBifNames.Scientific } kind && tuple.Items[1] is Integer digits && digits.Value >= ErlangFloatParameters.MinimumSmallInteger && digits.Value <= ErlangFloatParameters.MaximumSmallInteger:
                        format = kind.Name;
                        precision = (long)digits.Value;
                        break;
                    default:
                        throw new ErlangException(ErlangErrorReasons.BadArgument);
                }
            }
        }
        string result;
        if (format == ErlangFloatBifNames.Short)
            result = Short(number.Value);
        else if (format == ErlangFloatBifNames.Decimals && precision >= 0 && precision <= ErlangFloatParameters.MaximumFixedPrecision)
            result = Fixed(number.Value, (int)precision, compact);
        else if (format == ErlangFloatBifNames.Scientific && precision >= 0 && precision <= ErlangFloatParameters.MaximumScientificPrecision)
            result = ErlangFloatScientific.Format(number.Value, (int)precision);
        else
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        if (result.Length >= ErlangFloatParameters.OutputBufferBytes)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return result;
    }

    private static string Fixed(double value, int precision, bool compact)
    {
        bool negative = double.IsNegative(value);
        double magnitude = Math.Abs(value);
        string text;
        bool fallback = magnitude > ErlangFloatParameters.ExactIntegerBoundary || precision > ErlangFloatParameters.MaximumFastPrecision;
        if (fallback)
        {
            text = magnitude.ToString(ErlangFloatParameters.FixedFormat + precision, CultureInfo.InvariantCulture);
            if (text.Length + (negative ? 1 : 0) >= ErlangFloatParameters.OutputBufferBytes)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        else if (precision == 0)
            text = Math.Round(magnitude, MidpointRounding.AwayFromZero).ToString(ErlangFloatParameters.IntegerFormat, CultureInfo.InvariantCulture);
        else
        {
            double integral = Math.Floor(magnitude);
            double multiplier = Math.Pow(10, precision);
            double fraction = Math.Round((magnitude - integral) * multiplier, MidpointRounding.AwayFromZero);
            if (fraction >= multiplier)
            {
                integral++;
                fraction = 0;
            }
            text = integral.ToString(ErlangFloatParameters.IntegerFormat, CultureInfo.InvariantCulture) + ErlangFloatParameters.DecimalPoint + fraction.ToString(ErlangFloatParameters.IntegerFormat, CultureInfo.InvariantCulture).PadLeft(precision, '0');
        }
        if (compact && (precision > 0 || fallback))
        {
            text = text.TrimEnd('0');
            if (text.EndsWith(ErlangFloatParameters.DecimalPoint, StringComparison.Ordinal))
                text += ErlangFloatParameters.Zero;
        }

        return (negative ? ErlangFloatParameters.NegativeSign : string.Empty) + text;
    }

    private static string Short(double value)
    {
        string sign = double.IsNegative(value) ? ErlangFloatParameters.NegativeSign : string.Empty;
        double magnitude = Math.Abs(value);
        if (magnitude == 0)
            return sign + ErlangFloatParameters.FloatZero;
        string source = magnitude.ToString(ErlangFloatParameters.RoundTripFormat, CultureInfo.InvariantCulture);
        int marker = source.IndexOfAny(['E', 'e']);
        int power = marker < 0 ? 0 : int.Parse(source[(marker + 1)..], CultureInfo.InvariantCulture);
        string mantissa = marker < 0 ? source : source[..marker];
        int point = mantissa.IndexOf('.');
        if (point >= 0)
        {
            power -= mantissa.Length - point - 1;
            mantissa = mantissa.Remove(point, 1);
        }
        mantissa = mantissa.TrimStart('0');
        while (mantissa.EndsWith('0'))
        {
            mantissa = mantissa[..^1];
            power++;
        }
        string scientific = mantissa[..1] + ErlangFloatParameters.DecimalPoint + (mantissa.Length == 1 ? ErlangFloatParameters.Zero : mantissa[1..]) + ErlangFloatParameters.ExponentMarker + (power + mantissa.Length - 1).ToString(CultureInfo.InvariantCulture);
        int position = power + mantissa.Length;
        string fixedText = position <= 0
            ? ErlangFloatParameters.FloatZero[..2] + new string('0', -position) + mantissa
            : position >= mantissa.Length
                ? mantissa + new string('0', position - mantissa.Length) + ErlangFloatParameters.DecimalPoint + ErlangFloatParameters.Zero
                : mantissa[..position] + ErlangFloatParameters.DecimalPoint + mantissa[position..];

        return sign + (magnitude < ErlangFloatParameters.ExactIntegerBoundary && fixedText.Length <= scientific.Length ? fixedText : scientific);
    }
}
