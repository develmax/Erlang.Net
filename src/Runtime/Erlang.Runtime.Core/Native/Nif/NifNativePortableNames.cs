namespace Erlang;

internal static class NifNativePortableNames
{
    internal const string ProcessHost = "nif_bridge_process_host";
    internal const uint ProcessHostProtocol = 1;
    internal const string DefaultBridge = "Erlang.NifBridge.so";
    internal const string Open = "nif_bridge_platform_open";
    internal const string Symbol = "nif_bridge_platform_symbol";
    internal const string Close = "nif_bridge_platform_close";
    internal const string Error = "nif_bridge_platform_error";
    internal const string OpenFile = "nif_bridge_platform_open_file";
    internal const string WorkingDirectory = "nif_bridge_platform_getcwd";
    internal const string InitializerAlias = "_nif_init";
    internal const string SingleRuntime = "The portable Unix NIF bridge is process-owned by one runtime; a second runtime cannot borrow its callbacks.";
    internal const string ReviewedLoading = "Verified descriptor-based NIF loading is not implemented by the portable Unix loader; use ordinary host loading or the Linux glibc profile.";
    internal const string BridgeMismatch = "The process-owned portable NIF bridge cannot be replaced by another deployment.";
    internal const string UnsupportedReallocation = "The reviewed reallocation extension requires the Linux glibc namespace profile.";
}
