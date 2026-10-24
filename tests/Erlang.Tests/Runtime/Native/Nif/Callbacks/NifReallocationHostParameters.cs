namespace Erlang.Tests;

internal static class NifReallocationHostParameters
{
    internal const int Initial = 13;
    internal const int Grown = 37;
    internal const int Shrunk = 7;
    internal const int AfterZero = 5;
    internal const int Other = 17;
    internal const int OtherGrown = 41;
    internal const int GlobalPeak = 54;
    internal const int Iterations = 128;
    internal const int SnapshotFields = 11;
    internal const byte Marker = 0x65;
}
