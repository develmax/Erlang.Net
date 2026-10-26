namespace Erlang.Differential;

internal static class NativeHostDefaultCaseSources
{
    internal const string Case0 = "-module(native_host_default_contract). -export([run/0]). run()->{error,{load_failed,Reason}}=erlang:load_nif(\"__erlang_net_default_missing_nif_ba1ad0c1__\",[]),true=is_list(Reason),done.\n";
    internal const string Case1 = "-module(native_host_default_contract). -export([run/0]). run()->{error,{load_failed,Reason}}=erlang:load_nif(<<\"__erlang_net_default_missing_nif_ba1ad0c1__\",0,\"ignored\">>,[]),true=is_list(Reason),done.\n";
    internal const string Case2 = "-module(native_host_default_contract). -export([run/0]). run()->{'EXIT',{badarg,_}}=catch erlang:load_nif(42,[]),done.\n";
}
