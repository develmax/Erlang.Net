namespace Erlang.NifNativeBackend;

public static class OrdinaryBinaryCaseNames
{
    public const string Physical = "runtime/statistics-process-refc-binary-actual-prefix-header-words-and-reclaim";
    public const string ShortReference = "runtime/statistics-process-refc-short-view-preserves-native-subbits";
    public const string Graph = "runtime/statistics-process-refc-binary-queue-graph-transfer-and-host-escape";
    public const string Shutdown = "runtime/statistics-process-refc-binary-vm-close-waits-real-interest";
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    internal const int Bytes = 256;
    internal const int HeaderWords = 8;
    internal const int SubBitsWords = 5;
    internal const string StorageProperty = "OwnedStorage577";
    internal const string BackingProperty = "ResourceBacking577";
    internal const string NativeStorageProperty = "NativeStorage524";
    internal const string ReferencedBytesProperty = "ReferencedByteSize";
}
