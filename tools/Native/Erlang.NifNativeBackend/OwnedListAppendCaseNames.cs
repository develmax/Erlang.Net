namespace Erlang.NifNativeBackend;

public static class OwnedListAppendCaseNames
{
    public const string Physical = "native/owned-list-append776/physical-prefix-and-opaque-replay";
    internal const string Module = "owned_list_append776";
    internal const string ReplayModule = "owned_list_append776_replay";
    internal const string ReplayCaller = "owned_list_append776_replay_caller";
    internal const string ReplayFunction = "append";
    internal const string ModuleDeclarationFormat = "-module({0}).";
    internal const string CanonicalAppendCall = "erlang:'++'";
    internal const string QualifiedCallFormat = "{0}:{1}";
    internal const string Probe = "owned_list_append776_probe";
    internal const string Check = "check";
    internal const string Tail = "tail";
    internal const string Ok = "ok";
    internal const string Run = "run";
    internal const string File = "owned_list_append776.erl";
    internal const string Failure = "Canonical list append physical ownership changed";
    internal const string Functions = "functions";
    internal const string Source = """
        -module(owned_list_append776).
        -export([run/0]).
        run() ->
            Head = {make_ref(), <<1,2,3>>},
            Left = [Head, Head],
            Tail = [Head | tail],
            Result = erlang:'++'(Left, Tail),
            ok = owned_list_append776_probe:check(Left, Tail, Result, erlang:'++'(Left, [])),
            {Result, [] ++ tail, [1] ++ tail,
             try erlang:'++'(tail, []) catch error:badarg -> badarg end,
             try erlang:'++'([1 | tail], []) catch error:badarg -> badarg end}.
        """;
}
