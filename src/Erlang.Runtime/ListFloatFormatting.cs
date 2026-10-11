using System.Globalization;
using System.Numerics;

namespace Erlang;

internal static class ListFloatFormatting
{
    public static string Format(double value)
    {
        bool negative = double.IsNegative(value);
        double magnitude = Math.Abs(value);
        int exponent = magnitude == 0 ? 0 : (int)Math.Floor(Math.Log10(magnitude));
        BigInteger numerator = BigInteger.Zero;
        BigInteger denominator = BigInteger.One;
        if (magnitude != 0)
        {
            int binaryExponent = Math.ILogB(magnitude) - ListFloatFormat.BinaryFractionBits;
            numerator = new BigInteger(Math.ScaleB(magnitude, -binaryExponent));
            if (binaryExponent >= 0)
                numerator <<= binaryExponent;
            else
                denominator <<= -binaryExponent;
            BigInteger scaledNumerator = exponent < 0 ? numerator * BigInteger.Pow(10, -exponent) : numerator;
            BigInteger scaledDenominator = exponent >= 0 ? denominator * BigInteger.Pow(10, exponent) : denominator;
            if (scaledNumerator < scaledDenominator)
                exponent--;
            else if (scaledNumerator >= scaledDenominator * 10)
                exponent++;
        }
        int scale = ListFloatFormat.FractionDigits - exponent;
        if (scale >= 0)
            numerator *= BigInteger.Pow(10, scale);
        else
            denominator *= BigInteger.Pow(10, -scale);
        BigInteger digits = BigInteger.DivRem(numerator, denominator, out var remainder);
        int midpoint = (remainder * 2).CompareTo(denominator);
        if (midpoint > 0 || midpoint == 0 && !digits.IsEven)
            digits++;
        if (digits >= BigInteger.Pow(10, ListFloatFormat.FractionDigits + 1))
        {
            digits /= 10;
            exponent++;
        }
        string mantissa = digits.ToString(CultureInfo.InvariantCulture).PadLeft(ListFloatFormat.FractionDigits + 1, '0');
        string exponentDigits = Math.Abs(exponent).ToString(CultureInfo.InvariantCulture).PadLeft(ListFloatFormat.MinimumExponentDigits, '0');

        return (negative ? ListFloatFormat.NegativeSign : string.Empty) + mantissa[..1] + ListFloatFormat.DecimalPoint + mantissa[1..] + ListFloatFormat.ExponentMarker + (exponent < 0 ? ListFloatFormat.NegativeSign : ListFloatFormat.PositiveSign) + exponentDigits;
    }
}
