namespace Erlang;

internal static class CodePurgeNames
{
    public const string Function = ModuleCodeLifecycleNames.PurgeModule;
    public const string ReplyTag = "$code_purge_reply";
    public const string ErrorModuleKey = "module";
    public const string ErrorFormatter = "erl_erts_errors";
}
