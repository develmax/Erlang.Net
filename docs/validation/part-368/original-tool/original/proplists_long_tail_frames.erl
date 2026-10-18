-module(proplists_long_tail_frames).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [unfold_three,unfold_long,compact_three,compact_long,delete_three,delete_long,foldr_three,foldr_long].
run() -> [{N,value(N)} || N <- cases()].
value(N) -> try probe(N) of V -> {ok,V} catch C:R -> {error,C,R} end.
run_stacks() ->
    Names=cases(), Default={default,[{N,capture(N)} || N <- Names]},
    Old=erlang:system_flag(backtrace_depth,8),
    try [Default|[at_depth(D,Names) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D,Names) -> erlang:system_flag(backtrace_depth,D),{D,[{N,capture(N)} || N <- Names]}.
capture(N) -> try probe(N) of V -> {ok,V} catch C:R:S -> {error,C,R,S} end.
probe(unfold_three) -> proplists:unfold([a,b,c|bad]);
probe(unfold_long) -> proplists:unfold(lists:duplicate(4096,a)++bad);
probe(compact_three) -> proplists:compact([a,b,c|bad]);
probe(compact_long) -> proplists:compact(lists:duplicate(4096,a)++bad);
probe(delete_three) -> proplists:delete(k,[a,b,c|bad]);
probe(delete_long) -> proplists:delete(k,lists:duplicate(4096,a)++bad);
probe(foldr_three) -> proplists:to_map([{k,a},{k,b},{k,c}|bad]);
probe(foldr_long) -> proplists:to_map(lists:duplicate(4096,{k,a})++bad).
