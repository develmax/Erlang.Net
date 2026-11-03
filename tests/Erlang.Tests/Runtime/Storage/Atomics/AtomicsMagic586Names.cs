namespace Erlang.Tests;

internal static class AtomicsMagic586Names
{
    internal const string PhysicalTest = "runtime/atomics586/real-process-headers-share-live-prefix";
    internal const string GenerationsTest = "runtime/atomics586/old-magic-root-survives-minor-not-major";
    internal const string ForeignTest = "runtime/atomics586/foreign-before-stale-header-read";
    internal const string Heap = "TermHeap506";
    internal const string Gate = "Gate";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Collect = "Collect";
    internal const string CollectGeneration = "CollectGeneration508";
    internal const string OwnedStorage = "OwnedStorage562";
    internal const string CaptureAddress = "CaptureRawAddress571";
    internal const string Address = "Address571";
    internal const string RootsType = "Erlang.AtomicsRoots586";
    internal const string DirectRoot = "Direct586";
    internal const int Words = 3;
    internal const int BinaryWord = 1;
    internal const int NextWord = 2;
    internal const int Reference0Offset = 40;
    internal const long Header = 0x90;
    internal static readonly TimeSpan Deadline = AtomicsHost413Names.Deadline;
}
