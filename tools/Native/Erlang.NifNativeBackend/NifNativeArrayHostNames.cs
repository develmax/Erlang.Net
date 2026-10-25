namespace Erlang.NifNativeBackend;

internal static class NifNativeArrayHostNames
{
    internal const string Module = "native_array_callback_contract";
    internal const string Failure = "Native array callback contract failed";
    internal const string Pass = "PASS managed NIF array callbacks; no native library image loaded";
    // Independent pinned erl_nif_api_funcs.h ABI positions.
    internal const int TupleSlot = 72;
    internal const int ListSlot = 73;
    internal const int ManyItems = 8193;
    internal const int PayloadInteger = 37;
    internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
}
