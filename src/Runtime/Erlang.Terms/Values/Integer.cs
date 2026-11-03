using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class Integer : Term
{
    private readonly BigInteger portableValue;
    internal IOwnedIntegerStorage559? OwnedStorage559 { get; }
    public BigInteger Value => OwnedStorage559?.ReadInteger559() ?? portableValue;

    public Integer(BigInteger value) => portableValue = value;

    internal Integer(IOwnedIntegerStorage559 storage) => OwnedStorage559 = storage;

    public bool TryToDouble(out double result)
    {
        BigInteger value = Value;
        BigInteger magnitude = BigInteger.Abs(value);
        long width = magnitude.GetBitLength();

        if (width <= 53)
        {
            result = (double)value;

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

        result = value.Sign < 0 ? -number : number;

        return true;
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
