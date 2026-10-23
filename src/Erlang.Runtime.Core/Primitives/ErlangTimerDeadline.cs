using System.Numerics;

namespace Erlang;

internal static class ErlangTimerDeadline
{
    public static Term Delay(Term duration, bool absolute)
    {
        if (duration is not Integer time || time.Value < long.MinValue || time.Value > long.MaxValue || !absolute && time.Value < 0)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger now = ErlangClockState.Floor(ErlangClockState.MonotonicNanoseconds, ErlangClockParameters.NanosecondsPerMillisecond);
        BigInteger internalNow = -ErlangClockState.Floor(-ErlangClockState.MonotonicNanoseconds, ErlangClockParameters.NanosecondsPerMillisecond) - ErlangClockParameters.MonotonicMillisecondsOffset;
        BigInteger internalDeadline = absolute ? time.Value - ErlangClockParameters.MonotonicMillisecondsOffset : internalNow + time.Value;
        if (internalDeadline < ErlangClockParameters.MinimumInternalTimerMilliseconds || internalDeadline > ErlangClockParameters.MaximumInternalTimerMilliseconds)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return new Integer(absolute ? BigInteger.Max(BigInteger.Zero, time.Value - now) : time.Value);
    }
}
