namespace Erlang;

internal static class NifNativeLinuxNamespaceNames
{
    internal const string DefaultBridge = "Erlang.NifBridge.so";
    internal const string CapsuleMemoryFile = "erlang-nif-capsule";
    internal const string Libc = "libc.so.6";
    internal const string Unavailable = "glibc native namespace creation or loading failed";
    internal const string BridgeMismatch = "Native namespace bridge identity differs from the runtime domain";
    internal const string ActiveImages = "Native namespace still has active capsule images";
    internal const string Disposed = "Native namespace is closed";
    internal const int Immediate = 2;
    internal const int NamespaceRequest = 1;
    internal const int CloseOnExec = 1;
    internal const nint NewNamespace = -1;
}
