using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Integer(BigInteger value) : Term
{
    public BigInteger Value { get; } = value;

    public bool TryToDouble(out double result)
    {
        BigInteger magnitude = BigInteger.Abs(Value);
        long width = magnitude.GetBitLength();

        if (width <= 53)
        {
            result = (double)Value;
            return true;
        }

        if (width > 1024)
        {
            result = 0;
            return false;
        }

        int shift = (int)width - 53;
        BigInteger significant = magnitude >> shift;
        BigInteger remainder = magnitude - (significant << shift), halfway = BigInteger.One << (shift - 1);
        if (remainder > halfway || remainder == halfway && !significant.IsEven)
            significant++;

        double number = Math.ScaleB((double)significant, shift);
        if (!double.IsFinite(number))
        {
            result = 0;
            return false;
        }

        result = Value.Sign < 0 ? -number : number;
        return true;
    }
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
