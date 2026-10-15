namespace Erlang.NifNativeBackend;

internal static class NifNativeReallocationParameters
{
    internal const int NewFixturePathIndex = 0;
    internal const int NewFixtureHashIndex = 1;
    internal const int OldFixturePathIndex = 2;
    internal const int OldFixtureHashIndex = 3;
    internal const int OldBridgePathIndex = 4;
    internal const int OldBridgeHashIndex = 5;
    internal const int ExtensionPathIndex = 6;
    internal const int ExtensionHashIndex = 7;
    internal const int WindowsLayoutPathIndex = 4;
    internal const int WindowsLayoutHashIndex = 5;
    internal const int LegacyProfileParameterCount = 5;
    internal const int WindowsArguments = 6;
    internal const int LinuxArguments = 8;
    internal const int NullPositiveSelector = 1;
    internal const int NullZeroSelector = 2;
    internal const int SequenceSelector = 3;
    internal const int NullPositiveMask = 1539;
    internal const int NullZeroMask = 1601;
    internal const int SequenceMask = 2047;
    internal const int InitialBytes = 13;
    internal const int GrownBytes = 37;
    internal const int ShrunkBytes = 7;
    internal const int AfterZeroBytes = 5;
    internal const int LoadMarker = 42;
    internal const int IncrementInput = 41;
    internal const int IncrementOutput = 42;
    internal const int WindowsFunctionCount = 180;
    internal const int WindowsTailPointers = 2;
    internal const int WindowsReallocationSlot = 58;
    internal const int DataSentinel = 83;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(20);
    internal static readonly NifNativeReallocationCase[] Cases =
    [
        new(
            NifNativeReallocationHarnessNames.NullPositive,
            NullPositiveSelector,
            NullPositiveMask,
            [InitialBytes]
        ),
        new(
            NifNativeReallocationHarnessNames.NullZero,
            NullZeroSelector,
            NullZeroMask,
            [0]
        ),
        new(
            NifNativeReallocationHarnessNames.Sequence,
            SequenceSelector,
            SequenceMask,
            [InitialBytes, GrownBytes, ShrunkBytes, 0, AfterZeroBytes]
        )
    ];
}
