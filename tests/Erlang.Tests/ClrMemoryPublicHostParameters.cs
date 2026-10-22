namespace Erlang.Tests;

internal static class ClrMemoryPublicHostParameters
{
    internal const int PublicFunctions = 2;
    internal const int Payload = 17;
    internal const int RetainedPayload = 128;
    internal const int RetainedOffsetBits = 8;
    internal const int RetainedSliceBits = 520;
    internal const int PartialBits = 5;
    internal const int FirstNative = 7;
    internal const int SecondNative = 13;
    internal const int GrownNative = 37;
    internal const int ShrunkNative = 3;
    internal const int SmallProcessLimit = 3;
    internal const int SlotCycles = 9;
    internal const int ConcurrentCycles = 32;
    internal const int CollectionAttempts = 12;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
}
