using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class FloatTerm : Term
{
    private readonly double portableValue;
    internal IOwnedFloatStorage556? OwnedStorage556 { get; }
    public double Value => OwnedStorage556?.ReadFloat556() ?? portableValue;

    internal FloatTerm(IOwnedFloatStorage556 storage) => OwnedStorage556 = storage;

    public FloatTerm(double value)
    {
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);
        portableValue = value;
    }

    public override string ToString()
    {
        var s = Value.ToString("R", CultureInfo.InvariantCulture);

        return s.Contains('.') || s.Contains('E') ? s : s + ".0";
    }
}
