namespace Erlang.Differential;

internal static class NifDeclarationCaseNames
{
    internal const string Prefix = "erlang-nif-declaration-";
    internal const string ModulePrefix = "-module(oracle_nifs). -export([run/0]). ";
    internal const string Body = "run()->{fallback(),module_info(nifs),lists:keymember(nifs,1,module_info(attributes))}. fallback()->kept.";
    internal const string Kept = "kept";
}
