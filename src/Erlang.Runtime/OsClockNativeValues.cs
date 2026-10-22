using System.Globalization;
using System.Numerics;

namespace Erlang;

// OS wall-clock/PID/performance kernels for the complete Windows private native set.
internal static class OsClockNativeValues
{
    internal static BigInteger WallNanoseconds =>
        (new BigInteger(DateTimeOffset.UtcNow.Ticks) - DateTimeOffset.UnixEpoch.Ticks)
            * ErlangClockParameters.NanosecondsPerTick;

    internal static Term GetPid() => Cons.From(Environment.ProcessId.ToString(CultureInfo.InvariantCulture)
        .Select(character => (Term)Term.I(character)));

    internal static Term PerformanceCounter() => new Integer(ErlangClockUnits413.PerformanceCounter);

    internal static Term SystemTime() => new Integer(ErlangClockState.Floor(
        WallNanoseconds * ErlangClockUnits413.NativeFrequency, ErlangClockParameters.NanosecondsPerSecond
    ));

    internal static Term SystemTime(Term unit)
    {
        BigInteger now = WallNanoseconds;

        return new Integer(ErlangClockState.Floor(now * ErlangClockState.Unit(unit), ErlangClockParameters.NanosecondsPerSecond));
    }

    internal static Term Timestamp()
    {
        BigInteger microseconds = ErlangClockState.Floor(WallNanoseconds, ErlangClockParameters.NanosecondsPerSecond / ErlangClockParameters.MicrosecondsPerSecond);
        BigInteger seconds = BigInteger.DivRem(microseconds, ErlangClockParameters.MicrosecondsPerSecond, out var micro);
        BigInteger mega = BigInteger.DivRem(seconds, ErlangClockParameters.SecondsPerMegaSecond, out var remainder);

        return Term.Tuple(new Integer(mega), new Integer(remainder), new Integer(micro));
    }
}
