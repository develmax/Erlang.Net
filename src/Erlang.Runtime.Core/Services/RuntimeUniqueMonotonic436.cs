using System.Numerics;

namespace Erlang;

internal sealed class RuntimeUniqueMonotonic436
{
    private readonly object gate = new();
    private ulong raw = ulong.MaxValue;

    internal Term State
    {
        get
        {
            lock (gate)
                return new Integer(raw);
        }
    }

    internal bool Set(Term value)
    {
        if (value is not Integer integer || integer.Value < long.MinValue || integer.Value > ulong.MaxValue)
            return false;

        lock (gate)
            raw = (ulong)(integer.Value & ulong.MaxValue);

        return true;
    }

    internal Term Next(bool positive)
    {
        lock (gate)
        {
            raw = unchecked(raw + 1);
            if (positive)
                return new Integer(unchecked(raw + 1));

            long value = unchecked((long)raw + ErlangClockParameters.SmallIntegerMinimum);
            bool small = value >= ErlangClockParameters.SmallIntegerMinimum
                && value < -ErlangClockParameters.SmallIntegerMinimum;

            return small ? new Integer(value) : new Integer(unchecked((ulong)value));
        }
    }
}
