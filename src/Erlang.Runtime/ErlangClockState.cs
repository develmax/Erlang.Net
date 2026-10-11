using System.Diagnostics;
using System.Numerics;

namespace Erlang;

internal static class ErlangClockState
{
    private static readonly long started = Stopwatch.GetTimestamp();
    private static readonly BigInteger initialSystemNanoseconds = (new BigInteger(DateTimeOffset.UtcNow.Ticks) - DateTimeOffset.UnixEpoch.Ticks) * ErlangClockParameters.NanosecondsPerTick;
    private static readonly object counterGate = new();
    private static BigInteger unique = -1;
    private static BigInteger uniqueMonotonic = -1;
    private static BigInteger previousNow;

    public static BigInteger MonotonicNanoseconds => ErlangClockParameters.InitialMonotonicNanoseconds + new BigInteger(Stopwatch.GetTimestamp() - started) * ErlangClockParameters.NanosecondsPerSecond / Stopwatch.Frequency;

    public static BigInteger OffsetNanoseconds => initialSystemNanoseconds - ErlangClockParameters.InitialMonotonicNanoseconds;

    public static BigInteger SystemNanoseconds => MonotonicNanoseconds + OffsetNanoseconds;

    public static BigInteger Unit(Term value) => value switch
    {
        Integer integer when integer.Value > 0 => integer.Value,
        Atom { Name: ErlangClockNames.Native or ErlangClockNames.Nanosecond or ErlangClockNames.NanoSeconds } => ErlangClockParameters.NanosecondsPerSecond,
        Atom { Name: ErlangClockNames.Microsecond or ErlangClockNames.MicroSeconds } => ErlangClockParameters.MicrosecondsPerSecond,
        Atom { Name: ErlangClockNames.Millisecond or ErlangClockNames.MilliSeconds } => ErlangClockParameters.MillisecondsPerSecond,
        Atom { Name: ErlangClockNames.Second or ErlangClockNames.Seconds } => BigInteger.One,
        Atom { Name: ErlangClockNames.PerfCounter } => Stopwatch.Frequency,
        _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
    };

    public static BigInteger Floor(BigInteger numerator, BigInteger denominator)
    {
        BigInteger quotient = BigInteger.DivRem(numerator, denominator, out var remainder);

        return remainder.Sign < 0 ? quotient - 1 : quotient;
    }

    public static Term Convert(IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Integer time)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        BigInteger from = Unit(arguments[1]);
        BigInteger to = Unit(arguments[2]);

        return new Integer(Floor(time.Value * to, from));
    }

    public static Term Clock(string name, IReadOnlyList<Term> arguments)
    {
        BigInteger unit = arguments.Count == 0 ? ErlangClockParameters.NanosecondsPerSecond : Unit(arguments[0]);
        BigInteger time = name switch
        {
            ErlangClockNames.MonotonicTime => MonotonicNanoseconds,
            ErlangClockNames.SystemTime => SystemNanoseconds,
            ErlangClockNames.TimeOffset => OffsetNanoseconds,
            _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
        };

        return new Integer(Floor(time * unit, ErlangClockParameters.NanosecondsPerSecond));
    }

    public static Term Timestamp(bool strictlyIncreasing)
    {
        BigInteger time = Floor(SystemNanoseconds, ErlangClockParameters.NanosecondsPerSecond / ErlangClockParameters.MicrosecondsPerSecond);
        if (strictlyIncreasing)
        {
            lock (counterGate)
            {
                if (time <= previousNow)
                    time = previousNow + 1;
                previousNow = time;
            }
        }
        BigInteger seconds = BigInteger.DivRem(time, ErlangClockParameters.MicrosecondsPerSecond, out var microseconds);
        BigInteger megaSeconds = BigInteger.DivRem(seconds, ErlangClockParameters.SecondsPerMegaSecond, out var remainder);

        return Term.Tuple(new Integer(megaSeconds), new Integer(remainder), new Integer(microseconds));
    }

    public static Term Unique(IReadOnlyList<Term> arguments)
    {
        bool monotonic = false;
        bool positive = false;
        if (arguments.Count != 0)
        {
            foreach (Term modifier in Cons.Items(arguments[0]))
            {
                if (modifier is Atom { Name: ErlangClockNames.Positive })
                    positive = true;
                else if (modifier is Atom { Name: ErlangClockNames.Monotonic })
                    monotonic = true;
                else
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
        }
        lock (counterGate)
        {
            BigInteger raw = monotonic ? ++uniqueMonotonic : ++unique;

            return new Integer(raw + (positive ? 1 : ErlangClockParameters.SmallIntegerMinimum));
        }
    }
}
