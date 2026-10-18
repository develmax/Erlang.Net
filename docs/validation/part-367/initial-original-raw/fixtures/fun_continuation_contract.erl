-module(fun_continuation_contract).
-export([cases/0,run/0,run_stacks/0,opaque_call/1]).
cases() -> [opaque_named,opaque_anonymous,static_named,static_alias,protected_catch,protected_after].
run() -> [{Name,value(Name)} || Name <- cases()].
value(Name) -> try probe(Name) of Value -> {ok,Value} catch Class:Reason -> {error,Class,Reason} end.
run_stacks() ->
    Names=cases(),
    Default={default,[{Name,capture(Name)} || Name <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names) -> erlang:system_flag(backtrace_depth,Depth),{Depth,[{Name,capture(Name)} || Name <- Names]}.
capture(Name) -> try probe(Name) of Value -> {ok,Value} catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
probe(opaque_named) -> opaque_call(fun terminal/0);
probe(opaque_anonymous) -> opaque_call(fun() -> erlang:error(anonymous_marker) end);
probe(static_named) -> F=fun terminal/0,V=F(),{after_call,V};
probe(static_alias) -> F=fun terminal/0,Alias=F,V=Alias(),{after_call,V};
probe(protected_catch) ->
    F=fun terminal/0,
    try V=F(),{after_call,V} catch error:terminal_marker -> caught end;
probe(protected_after) ->
    erlang:put(after_marker,missing),
    F=fun terminal/0,
    Result=try try V=F(),{after_call,V} after erlang:put(after_marker,executed) end
        catch error:terminal_marker -> caught end,
    {Result,erlang:erase(after_marker)}.
opaque_call(F) -> V=F(),{after_call,V}.
terminal() -> erlang:error(terminal_marker).
