using System.Reflection;

namespace Erlang.Tests;

internal static class BinaryArena598Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const BindingFlags StaticMembers = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Failure = "Binary598 actual arena/root invariant failed.";
    internal const string Compiler = "Erlang.BinarySearchCompiler";
    internal const string Compile = "Compile";
    internal const string Backing = "Backing598";
    internal const string Heap = "TermHeap506";
    internal const string Storage = "OwnedStorage562";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Collect = "Collect";
    internal const string CollectGeneration = "CollectGeneration508";
    internal const string Gate = "Gate";
    internal const string CaptureAddress = "CaptureRawAddress571";
    internal const string Address = "Address571";
    internal const string ReleaseProducer = "ReleaseProducer598";
    internal const string Create = "CreateBinarySearchPattern";
    internal const string Retired = "Retired598";
    internal const string Freed = "PhysicalFreed598";
    internal const string Arena = "Arena598";
    internal const string Data = "Data598";
    internal const string TotalBytes = "AllocationBytes598";
    internal const string Reader = "Read598";
    internal const string OwnReference = "Reference";
    internal const string Bm = "bm";
    internal const string Ac = "ac";
    internal const int Alphabet = 256;
    internal const int HeaderWords = 3;
    internal const int Header = 0x90;
    internal const int PrefixBytes = 56;
    internal const int OriginalSizeOffset = 24;
    internal const int RefCountOffset = 16;
    internal const int DestructorOffset = 32;
    internal const int Ref0Offset = 40;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
    internal const string AcRootCase = "runtime/binary598/pinned-x64-simd-trie-root-bytes-and-cap";
    // Independent literal x64 source-profile witnesses, not reflected arena constants.
    internal const int SourceAcTrieBytes = 40;
    internal const int SourceAcNodeBytes = 2064;
    internal const int SourceAcRootOffset = 8;
    internal const int SourceAcRootCountOffset = 16;
    internal const int SourceAcRootBytesOffset = 24;
    internal const int SourceAcRootSimdMax = 16;
    internal const int PatternByteCount = 2;
    internal const int AddedRootDuplicates = 2;
    internal const int RootByteStride = 7;
    internal const byte CommonSuffix = 255;
    internal const byte AlternateSuffix = 254;
    internal const string PhysicalCase = "runtime/binary598/physical-bm-ac-headers-and-gc";
    internal const string ActiveCase = "runtime/binary598/active-reader-and-host-store-retirement";
    internal const string HostCase = "runtime/binary598/host-reservation-shared-alias-transfer-retirement";
}
