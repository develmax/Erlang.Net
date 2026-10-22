-module(lc_source_frames_b).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [first,later,long_later,skip_first,skip_later,bad_filter,filter_raise,bad_generator_first,bad_generator_tail,nested_body,multiple,two_generators,source_nested,api_first,api_later,api_long_later].
run() -> [{N,value(N)} || N <- cases()].
value(N) -> try renamed_probe(N) of V -> {ok,V} catch C:R -> {error,C,R} end.
run_stacks() ->
    Names=cases(),
    Default={default,[{N,catch_case(N)} || N <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(D,Names) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D,Names) -> erlang:system_flag(backtrace_depth,D),{D,[{N,catch_case(N)} || N <- Names]}.
catch_case(N) -> try renamed_probe(N) of V -> {ok,V} catch C:R:S -> {error,C,R,S} end.
renamed_probe(first) -> [fail(X) || X <- [bad,ok]];
renamed_probe(later) -> [fail(X) || X <- [ok,bad]];
renamed_probe(long_later) -> [fail(X) || X <- lists:duplicate(4096,ok) ++ [bad]];
renamed_probe(skip_first) -> [fail(X) || X <- [skip,bad], X =/= skip];
renamed_probe(skip_later) -> [fail(X) || X <- [ok,skip,bad], X =/= skip];
renamed_probe(bad_filter) -> [X || X <- [ok,bad], filter(X)];
renamed_probe(filter_raise) -> [X || X <- [ok,bad], filter_raise(X)];
renamed_probe(bad_generator_first) -> [X || X <- bad];
renamed_probe(bad_generator_tail) -> [X || X <- [ok,ok,ok|bad]];
renamed_probe(nested_body) -> [[fail(Y) || Y <- [ok,bad]] || X <- [left], X =/= unused];
renamed_probe(multiple) -> A=[X || X <- [a]],{A,[fail(Y) || Y <- [ok,bad]]};
renamed_probe(two_generators) -> [fail(Y) || X <- [a,b], Y <- [ok,bad], X =/= unused];
renamed_probe(source_nested) -> [fail(X) || X <- [Y || Y <- [ok,bad]]];
renamed_probe(api_first) -> [api(X) || X <- [bad,ok]];
renamed_probe(api_later) -> [api(X) || X <- [ok,ok,ok,bad]];
renamed_probe(api_long_later) -> [api(X) || X <- lists:duplicate(4096,ok) ++ [bad]].
fail(bad) -> erlang:error(lc_marker);
fail(X) -> X.
filter(ok) -> true;
filter(bad) -> neither.
filter_raise(ok) -> true;
filter_raise(bad) -> erlang:error(filter_marker).
api(ok) -> proplists:get_value(k,[k]);
api(bad) -> proplists:get_value(k,bad).
