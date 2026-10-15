namespace Erlang;

internal static class NifNativeNames
{
    internal const string HostAssembly = "Erlang.NifNativeBackend";
    internal const string Module = "memory_allocator_nif_fixture";
    internal const string SourceSha256 = "7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074";
    internal const string Initializer = "nif_init";
    internal const string Variant = "beam.vanilla";
    internal const string Increment = "increment";
    internal const string Greeting = "greeting";
    internal const string WindowsExtension = ".dll";
    internal const string LinuxExtension = ".so";
    internal const string LinuxDescriptorPrefix = "/proc/self/fd/";
    internal const string LoaderLibrary = "libdl.so.2";
    internal const string BridgeRegister = "nif_bridge_register";
    internal const string BridgeReadTerm = "nif_bridge_read_term";
    internal const int FunctionCount = 2;
    internal const int IncrementArity = 1;
    internal const int GreetingArity = 0;
    internal const uint BridgeVersion = 1;
    internal const int LinuxImmediate = 2;
    internal const int LinuxGlobal = 256;
    internal const int MaximumVariadicItems = 4096;
    internal static readonly Atom BadArgument = Term.A(ErlangErrorReasons.BadArgument);
    internal static readonly string[] BridgeSymbols =
    [
        "enif_priv_data", "enif_alloc", "enif_free", "enif_get_int",
        "enif_make_badarg", "enif_make_int", "enif_make_string",
        "enif_is_exception", "enif_has_pending_exception", "enif_raise_exception",
        "enif_make_tuple", "enif_make_list"
    ];
}
