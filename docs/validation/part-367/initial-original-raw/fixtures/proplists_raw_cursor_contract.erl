-module(proplists_raw_cursor_contract).
-export([cases/0,run/0,run_stacks/0,native_hash_probes/0,hash_cases/0,hash_run/0,hash_run_stacks/0]).
cases() -> [flat32_one,flat32_fifteen,flat32_limit,flat32_over,flat32_negative,flat32_big,
            hamt33_zero,hamt33_one,hamt33_two,hamt33_fifteen,hamt33_sixteen,
            hamt33_negative_small,hamt33_small_min,hamt33_big,hamt33_negative_big,
            hamt33_tuple_keys,hamt33_atom_keys,hamt33_binary_keys].
run() -> [{Name,value(Name)} || Name <- cases()].
value(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason -> {error,Class,Reason} end.
run_stacks() ->
    Names = cases(),
    Default = {default,[{Name,capture(Name)} || Name <- Names]},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth,Names) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth,Names) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,[{Name,capture(Name)} || Name <- Names]}.
capture(Name) ->
    try probe(Name) of Value -> {ok,Value}
    catch Class:Reason:Stack -> {error,Class,Reason,Stack} end.
flat32() -> maps:from_list([{I,I} || I <- lists:seq(0,31)]).
hamt33() -> maps:from_list([{I,I} || I <- lists:seq(0,32)]).
probe(flat32_one) -> proplists:from_map([1|flat32()]);
probe(flat32_fifteen) -> proplists:from_map([15|flat32()]);
probe(flat32_limit) -> proplists:from_map([32|flat32()]);
probe(flat32_over) -> proplists:from_map([33|flat32()]);
probe(flat32_negative) -> proplists:from_map([-1|flat32()]);
probe(flat32_big) -> proplists:from_map([(1 bsl 80)|flat32()]);
probe(hamt33_zero) -> proplists:from_map([0|hamt33()]);
probe(hamt33_one) -> proplists:from_map([1|hamt33()]);
probe(hamt33_two) -> proplists:from_map([2|hamt33()]);
probe(hamt33_fifteen) -> proplists:from_map([15|hamt33()]);
probe(hamt33_sixteen) -> proplists:from_map([16|hamt33()]);
probe(hamt33_negative_small) -> proplists:from_map([-1|hamt33()]);
probe(hamt33_small_min) -> proplists:from_map([-(1 bsl 59)|hamt33()]);
probe(hamt33_big) -> proplists:from_map([(1 bsl 80)|hamt33()]);
probe(hamt33_negative_big) -> proplists:from_map([-(1 bsl 80)|hamt33()]);
probe(hamt33_tuple_keys) ->
    Map = maps:from_list([{{key,I},I} || I <- lists:seq(0,32)]),
    proplists:from_map([1|Map]);
probe(hamt33_atom_keys) ->
    Keys = [a00,a01,a02,a03,a04,a05,a06,a07,a08,a09,a10,
            a11,a12,a13,a14,a15,a16,a17,a18,a19,a20,a21,
            a22,a23,a24,a25,a26,a27,a28,a29,a30,a31,a32],
    Map = maps:from_list([{Key,Key} || Key <- Keys]),
    proplists:from_map([1|Map]);
probe(hamt33_binary_keys) ->
    Map = maps:from_list([{<<I:16>>,I} || I <- lists:seq(0,32)]),
    proplists:from_map([1|Map]).
native_hash_probes() ->
    Old = erts_debug:set_internal_state(available_internal_state,true),
    try [{Term,erts_debug:get_internal_state({internal_hash,Term})} || Term <-
        [0,1,-1,(1 bsl 59)-1,-(1 bsl 59),1 bsl 59,-(1 bsl 59)-1,
         1 bsl 64,1 bsl 128,0.0,1.5,<<>>,<<1,2,3>>,<<5:3>>,
         {},{1,2},[],[1,2,3],[1|2],key]]
    after erts_debug:set_internal_state(available_internal_state,Old) end.

hash_cases() ->
    [integer_zero,integer_one,integer_minus_one,small_max,small_min,
     positive_big_boundary,negative_big_boundary,big_word,big_two_words,
     float_zero,float_fraction,binary_empty,binary_three,bitstring_partial,
     tuple_empty,tuple_integers,list_empty,list_bytes,list_improper,atom_key].
hash_run() ->
    Pairs = native_hash_probes(),
    lists:zipwith(
        fun(Name,{Term,Hash}) -> {Name,{ok,{Term,Hash}}} end,
        hash_cases(),
        Pairs).
hash_run_stacks() ->
    Default = {default,hash_run()},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [hash_at_depth(Depth) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
hash_at_depth(Depth) ->
    erlang:system_flag(backtrace_depth,Depth),
    {Depth,hash_run()}.
