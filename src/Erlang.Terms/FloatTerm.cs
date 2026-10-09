using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang;

public sealed class FloatTerm : Term
{
    public double Value
    {
        get;
    }
    public FloatTerm(double value)
    {
        if (!double.IsFinite(value))
            throw new ErlangException(ErlangErrorReasons.BadArithmetic);
        Value = value;
    }
    public override string ToString()
    {
        var s = Value.ToString("R", CultureInfo.InvariantCulture);
        return s.Contains('.') || s.Contains('E') ? s : s + ".0";
    }
}
