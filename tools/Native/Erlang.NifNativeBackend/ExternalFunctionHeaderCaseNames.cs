namespace Erlang.NifNativeBackend;

public static class ExternalFunctionHeaderCaseNames
{
    public const string Physical = "runtime/statistics-opaque-sdk-external-function-real-kind-header-no-canonical-admission";
    internal const string Module = "external_header_metadata";
    internal const string Function = "opaque_external";
    internal const string Storage = "OwnedFunctionStorage";
    internal const nuint Header = 0x1000114;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
