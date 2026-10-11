namespace Erlang;

internal static class ProcessSchedulingLimits
{
    public const int CooperativeSlice = 2000;
    public const int NativeMaximumBump = 4000;
    public const long MaximumSmallBump = (1L << 59) - 1;
}
