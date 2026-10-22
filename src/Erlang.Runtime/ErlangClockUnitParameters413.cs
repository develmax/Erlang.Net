namespace Erlang;

internal static class ErlangClockUnitParameters413
{
    internal const long MinimumWindowsQpcFrequency = 9_000_000;
    internal const long MaximumWindowsQpcFrequencyExclusive = 1L << 32;
    internal const int WindowsTickCountFractionBits = 10;
    internal const long WindowsTickCountFrequency = ErlangClockParameters.MillisecondsPerSecond << WindowsTickCountFractionBits;
}
