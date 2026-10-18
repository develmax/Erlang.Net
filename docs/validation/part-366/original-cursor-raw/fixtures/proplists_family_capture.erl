-module(proplists_family_capture).
-export([run_stacks/0]).
run_stacks() ->
    Names=proplists_family_probe:cases(),
    Default={default,[{N,capture(N)} || N <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try
        [Default | [at_depth(D,Names) || D <- [0,1,2,8]]]
    after
        erlang:system_flag(backtrace_depth,Old)
    end.
at_depth(D,Names) ->
    erlang:system_flag(backtrace_depth,D),
    {D,[{N,capture(N)} || N <- Names]}.
capture(N) ->
    try proplists_family_probe:run_case(N) of
        V -> {ok,V}
    catch C:R:S -> {error,C,R,S}
    end.
