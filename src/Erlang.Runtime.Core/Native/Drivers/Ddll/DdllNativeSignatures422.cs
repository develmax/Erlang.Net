namespace Erlang;

internal static class DdllNativeSignatures422
{
    internal const string Load = "try_load", Unload = "try_unload", Info = "info";
    internal const string Monitor = "monitor", Demonitor = "demonitor", Loaded = "loaded_drivers";
    internal static readonly (string Function, int Arity)[] Stateful =
    [
        (Load, 3), (Unload, 2), (Info, 2), (Monitor, 2), (Demonitor, 1), (Loaded, 0)
    ];
}
