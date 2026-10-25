using System.Diagnostics;
using System.Numerics;

namespace Erlang;

internal static class ErlangClockUnits413
{
    internal static long NativeFrequency => OperatingSystem.IsWindows()
        ? Stopwatch.Frequency >= ErlangClockUnitParameters413.MinimumWindowsQpcFrequency
            && Stopwatch.Frequency < ErlangClockUnitParameters413.MaximumWindowsQpcFrequencyExclusive
            ? Stopwatch.Frequency
            : ErlangClockUnitParameters413.WindowsTickCountFrequency
        : ErlangClockParameters.NanosecondsPerSecond;

    internal static long PerformanceFrequency => OperatingSystem.IsWindows()
        ? ErlangClockParameters.NanosecondsPerSecond
        : Stopwatch.Frequency;

    internal static BigInteger PerformanceCounter => ErlangClockState.Floor(
        new BigInteger(Stopwatch.GetTimestamp()) * PerformanceFrequency, Stopwatch.Frequency
    );
}
