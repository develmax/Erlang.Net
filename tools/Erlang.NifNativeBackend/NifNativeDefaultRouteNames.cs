namespace Erlang.NifNativeBackend;

internal static class NifNativeDefaultRouteNames
{
    internal const string Error = "error";
    internal const string Mode = "--managed-default-routes";
    internal const string Module = "native_default_route_control";
    internal const string Load = "load";
    internal const string Reserve = "reserve";
    internal const string Reserved = "reserved";
    internal const string ProbeModule = "nif_route_probe";
    internal const string RelativeStem = "route/../entrée";
    internal const string MissingPrefix = "managed-nif-missing-";
    internal const string HashLeaf = "-hash";
    internal const string HostResult = "host_override";
    internal const string Pass = "Managed default-source native route controls passed; NO native library or original OTP executed.";
    internal const string UnopenedHash = "0000000000000000000000000000000000000000000000000000000000000000";
    internal const string BridgeLeaf = "never-opened-bridge.so";
    internal const string Source = """
        -module(native_default_route_control).
        -export([load/1,reserve/0]).
        load(Name) -> erlang:load_nif(Name, []).
        reserve() -> Value = nif_route_probe:reserve(), {reserved, Value}.
        """;
}
