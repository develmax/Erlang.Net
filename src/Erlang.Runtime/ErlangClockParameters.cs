namespace Erlang;

internal static class ErlangClockParameters
{
    public const long NanosecondsPerSecond = 1000000000;
    public const long MicrosecondsPerSecond = 1000000;
    public const long MillisecondsPerSecond = 1000;
    public const long NanosecondsPerTick = 100;
    public const long NanosecondsPerMillisecond = 1000000;
    public const long SecondsPerMegaSecond = 1000000;
    public const long SmallIntegerMinimum = -(1L << 59);
    public const long InitialMonotonicNanoseconds = SmallIntegerMinimum / NanosecondsPerSecond * NanosecondsPerSecond;
    public const long MinimumInternalTimerMilliseconds = 1000;
    public const long MaximumInternalTimerMilliseconds = long.MaxValue / NanosecondsPerSecond * MillisecondsPerSecond;
    public const long MonotonicMillisecondsOffset = InitialMonotonicNanoseconds / NanosecondsPerMillisecond - MinimumInternalTimerMilliseconds;
}
