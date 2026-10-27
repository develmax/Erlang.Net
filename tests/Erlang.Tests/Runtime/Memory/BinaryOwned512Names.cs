namespace Erlang.Tests;

internal static class BinaryOwned512Names
{
    internal const string SharingTest = "runtime/binary-owned512/real-shared-backing-roots-and-host-message-survival";
    internal const string SourceTest = "runtime/binary-owned512/source-publication-and-rollback-root-provider";
    internal const string NativeTest = "runtime/binary-owned512/native-reference-retains-storage-through-owner-release";
    internal const string Heap = "TermHeap506";
    internal const string Allocate = "AllocateBinaryInto512";
    internal const string Enter = "Enter";
    internal const string Reserve = "Reserve";
    internal const string Set = "Set";
    internal const string Read = "Read";
    internal const string Clear = "Clear";
    internal const string Collect = "Collect";
    internal const string BinarySnapshot = "BinarySnapshot512";
    internal const string Registry = "BinaryRegistry512";
    internal const string RegistrySnapshot = "Snapshot512";
    internal const string ReferenceBytes = "ReferenceBackingBytes";
    internal const string HeapBytes = "HeapBackingBytes";
    internal const string Distinct = "DistinctBackings";
    internal const string References = "RootBackingReferences";
    internal const string BinaryRootsField = "binaryRoots512";
    internal const string Prepare = "Prepare512";
    internal const string Capture = "CaptureBinaryPayload512";
    internal const string Identity = "Identity";
    internal const string NativeStorageType = "Erlang.NifNativeBinaryStorage";
    internal const string AllocateNative = "TryAllocate";
    internal const string NativeCount = "LiveBufferCount";
    internal const string NativeRetain = "RetainPayload512";
    internal const string NativeRelease = "ReleaseNativeOwnership";
    internal const string NativeSize = "Size512";
    internal const string NativeCopy = "Copy512";
    internal const string SourceModule = "binary_root_guest512";
    internal const string ProbeModule = "binary_root_probe512";
    internal const string Probe = "sample";
    internal const string Run = "run";
    internal const string Failure = "Owned binary512 private root-provider contract failed.";
    internal const int PayloadBytes = 4096;
    internal const string Source = """
        -module(binary_root_guest512).
        -export([run/1]).
        run(B) -> V={B,B}, put(binary_root512,V), binary_root_probe512:sample(B), erase(binary_root512).
        """;
}
