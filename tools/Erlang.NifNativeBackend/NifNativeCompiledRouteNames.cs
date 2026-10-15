namespace Erlang.NifNativeBackend;

internal static class NifNativeCompiledRouteNames
{
    internal const string BridgeModule = "nif_native_compiled_bridge";
    internal const string Install = "install";
    internal const string Plain = "plain";
    internal const string Local = "local";
    internal const string NotLoaded = "not_loaded";
    internal const int PlainValue = 17;
    internal const int Input = 41;
    internal const int Expected = 42;
    internal const string InstallSuccess = "ok";
    internal static readonly Atom Installed = Term.A(InstallSuccess);
    internal const string Pass = "Trusted native generated-source entry routes passed; BEAM/old-image/general ABI NOT established.";
    internal const string SourceHashLabel = "Native generated source SHA256: ";
    internal static readonly Atom NotLoadedReason = Term.A(NotLoaded);
    internal static readonly string[] Routes = ["increment", "tail", "named", "external", "anonymous", "remote"];
    internal const string Source = "-module(memory_allocator_nif_fixture).\n-export([install/0,increment/1,greeting/0,plain/0,tail/1,local/1,named/1,external/1,anonymous/1,remote/1]).\n-nifs([increment/1,greeting/0]).\ninstall() -> ok = nif_native_compiled_bridge:install(), ok.\nincrement(_) -> erlang:nif_error(not_loaded).\ngreeting() -> erlang:nif_error(not_loaded).\nplain() -> 17.\ntail(X) -> increment(X).\nlocal(X) -> {increment(X),greeting()}.\nnamed(X) -> F=fun increment/1,F(X).\nexternal(X) -> F=fun memory_allocator_nif_fixture:increment/1,F(X).\nanonymous(X) -> F=fun(Y)->increment(Y) end,F(X).\nremote(X) -> memory_allocator_nif_fixture:increment(X).\n";
}
