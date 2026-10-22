-module(proplists_reference_hash_contract).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [word_counts,map_aliases,roundtrip_aliases].
run() ->
    Old = erts_debug:set_internal_state(available_internal_state,true),
    try [{Name,{ok,value(Name)}} || Name <- cases()]
    after erts_debug:set_internal_state(available_internal_state,Old) end.
run_stacks() ->
    Default = {default,run()}, Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(D) || D <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(D) -> erlang:system_flag(backtrace_depth,D), {D,run()}.
reference(Count) ->
    <<131,Node/binary>> = term_to_binary('hash_reference@remote'),
    Tail = list_to_binary(lists:duplicate(4*(Count-1),0)),
    binary_to_term(<<131,90,Count:16,Node/binary,1:32,7:32,Tail/binary>>).
hash(R) -> erts_debug:get_internal_state({internal_hash,R}).
value(word_counts) ->
    One = reference(1),
    [{N,R =:= One,term_to_binary(R),hash(R)} || N <- lists:seq(1,5), R <- [reference(N)]];
value(map_aliases) ->
    One = reference(1), Base = maps:from_list([{I,I} || I <- lists:seq(1,33)]),
    Map = Base#{One=>present},
    [{N,maps:get(reference(N),Map,missing)} || N <- lists:seq(1,5)];
value(roundtrip_aliases) ->
    One = reference(1),
    [{N,R =:= One,hash(R) =:= hash(One)} || N <- lists:seq(1,5), R <- [binary_to_term(term_to_binary(reference(N)))]] .
