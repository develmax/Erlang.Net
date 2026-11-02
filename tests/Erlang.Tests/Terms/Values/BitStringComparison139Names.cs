namespace Erlang.Tests;

internal static class BitStringComparison139Names
{
    internal const string BoundaryCase = "terms/bitstring-comparison139/exhaustive-prefix-padding-offsets";
    internal const string OwnedCase = "runtime/bitstring-comparison139/actual-heap-module-stale-foreign";
    internal const string ResourceCase = "runtime/bitstring-comparison139/actual-resource-reader-lifetime";
    internal const string Failure = "BitString comparison139 contract failed";
    internal const string ForeignRoot = "An owned guest value crossed heaps without the explicit copy boundary.";
    internal const string ExpectedException = "BitString comparison139 expected ";
    internal const string ExpectedHeap = "Expected real owned heap-bit storage";
    internal const string ExpectedModule = "Expected actual module binary storage";
    internal const string Heap = "TermHeap506", Enter = "Enter", Reserve = "Reserve", Adopt = "AdoptGuestInto507";
    internal const string Clear = "Clear", Collect = "Collect", ReadHeapBits = "ReadHeapBitLength", SetLocal = "SetLocalGraph507";
    internal const string OwnedHeap = "OwnedHeapBits", OwnedResource = "OwnedStorage577", Owner = "Owner", Domain = "LiteralDomain800";
    internal const string Code = "Code", Structure = "Structure", Literals = "Literals", Execution = "ExecutionIdentity", Image = "Image";
    internal const string Purge = "PurgeModule", CollectGeneration = "CollectGeneration508";
    internal const int MajorReductionCredit = 0;
    internal const string MailboxRetirements = "PayloadRetirements593", MailboxRetirementGate = "gate";
    internal const string ActiveRetirements = "active", FailedRetirements = "failed", RetirementCompletion = "Completion", CompletionTask = "Task";
    internal const string Retirement = "Retirement800", BackingType = "Erlang.BinaryBacking";
    internal const int ShortMaximumBits = 17, PatternCount = 3, AlignmentCount = 8, RawBackingBytes = 8;
    internal const int BitMask = 1, UnreservedSlot = -1;
    internal const System.Reflection.BindingFlags Members = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
    internal const int BitsPerByte = 8, HighBit = 7, ZeroPattern = 0, OnesPattern = 1, MixedPatternSeed = 0x96, MixedPatternStep = 53;
    internal const int BoundaryPatternStep = 37, BoundaryPatternSeed = 9, BoundaryBackingBytes = 40;
    internal const int LiteralBytes = 131, LiteralPatternStep = 37, LiteralPatternSeed = 5;
    internal const int LargeBytes = 2 * 1024 * 1024, Less = -1, Equal = 0, Greater = 1;
    internal const int AlignedOffset = 0, OneBit = 1, TwoBits = 2, ThreeBits = 3;
    internal const int OwnedBits = 17, OwnedPrefixBits = 16, ResourceTailTrim = 7, ResourceOffsetBits = 3;
    internal static readonly int[] BoundaryLengths = [23, 24, 25, 31, 32, 33, 63, 64, 65, 127, 128, 129];
    internal static readonly byte[] HighOne = [0x80], AllHigh = [0xff], ThreeBitClean = [0xa0], ThreeBitDirty = [0xbf];
    internal static readonly byte[] OwnedInput = [0x96, 0xff, 0x80];
}
