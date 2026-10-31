namespace Erlang.NifNativeBackend;

public static class OrdinaryBinaryWritableCaseNames
{
    public const string Preservation = "runtime/statistics-ordinary-refc-writable-origin-preserves-append-token-capacity-aliases";
    internal const string Initialize = "InitializeWritable";
    internal const string Append = "FromValidatedAppend";
    internal const string PinSharing = "PinForSharing";
    internal const string Backing = "backing";
    internal const string Writer = "writer";
    internal const string Referenced = "ReferencedByteSize";
    internal const string CaptureImmutable = "CanCaptureImmutableOrdinary";
    internal const string NativeStorage = "NativeStorage524";
    internal const int InitialCapacity = 256;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
