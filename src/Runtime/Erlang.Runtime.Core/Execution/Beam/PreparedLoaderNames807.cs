namespace Erlang;

internal static class PreparedLoaderNames807
{
    internal const string Invalid = "An admitted native prepared-loader owner is required.";
    internal const string Closed = "The native prepared-loader owner is closed.";
    internal const string PlanUsed = "The prepared finish plan was already committed.";
    internal const string MetadataQueueFailure = "The prepared-loader metadata notification could not be queued.";
    internal const string QueueFailure = "The prepared-loader cleanup item could not be queued.";
    internal const string DestructorFailure = "The actual prepared-loader native-state destructor did not complete.";
    internal static int PointerBytes => NativeWordLayout.Bytes;
    internal static int MagicPrefixBytes => NativeMagicLayout.AlignedDataOffset;
    internal const int IdentityWords = 3;
}
