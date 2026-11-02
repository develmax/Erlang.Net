namespace Erlang.NifNativeBackend;

public static class ReadonlyTrace817CaseNames
{
    public const string Physical = "native/trace817/real-private-fragment-late-share-discard-and-gc-import";
    internal const string Failure = "The actual readonly trace fragment changed source ownership early, lost native cells or leaked cleanup.";
    internal const string Payload = "readonly_trace817";
    internal const string RuntimeGateField = "gate";
    internal const int BinaryBytes = 512;
    internal const int ReferencesByteOffset = 16;
    internal const int OutputPhysicalWords = 16;
    internal const int PreservedTokenLabel = 17;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
