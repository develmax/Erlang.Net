-module(beam_literal_call_probe).
-export([invoke/0, continuation/1, const/0, wrap/1, wrap_arg/2]).
invoke() -> take(#{b => 2, a => 1}, <<>>).
continuation(X) ->
    take(X, []),
    {ok, <<"x">>}.
const() -> {ok, <<"x">>}.
take(A, _) -> A.

wrap([]) ->
    Flags = #{strategy => one_for_one},
    Static = #{id => watcher},
    Value = logger_proxy:child_spec(),
    {ok, {Flags, [Static, Value]}}.

wrap_arg(Sup, Arg) ->
    supervisor:start_child(Sup, [Arg]).
