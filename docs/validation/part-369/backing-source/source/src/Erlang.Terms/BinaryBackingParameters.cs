namespace Erlang;

internal static class BinaryBackingParameters
{
    internal const int MinimumAppendBytes = 256;
    internal const int LargeGrowthThreshold = 16 * 1024 * 1024;
    internal const int PinMinimumSpareBytes = 8;
    internal const double LargeGrowthFactor = 1.2;
}
