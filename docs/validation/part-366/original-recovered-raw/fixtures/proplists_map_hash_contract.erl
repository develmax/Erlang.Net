-module(proplists_map_hash_contract).
-export([cases/0,run/0,run_stacks/0]).
cases() -> [empty,one,two,flat32,hamt33,hamt64,hamt256,nested_flat,nested_hamt,map_keys,atom_flat,collision_first,collision_second,collision_map,collision_tuple].
run() ->
    Old = erts_debug:set_internal_state(available_internal_state,true),
    try [{Name,{ok,{value(Name),erts_debug:get_internal_state({internal_hash,value(Name)})}}} || Name <- cases()]
    after erts_debug:set_internal_state(available_internal_state,Old) end.
run_stacks() ->
    Default = {default,run()},
    Old = erlang:system_flag(backtrace_depth,8),
    try [Default | [at_depth(Depth) || Depth <- [0,1,2,8]]]
    after erlang:system_flag(backtrace_depth,Old) end.
at_depth(Depth) -> erlang:system_flag(backtrace_depth,Depth),{Depth,run()}.
integers(Count) -> maps:from_list([{I,I} || I <- lists:seq(0,Count-1)]).
value(empty) -> #{};
value(one) -> integers(1);
value(two) -> integers(2);
value(flat32) -> integers(32);
value(hamt33) -> integers(33);
value(hamt64) -> integers(64);
value(hamt256) -> integers(256);
value(nested_flat) -> #{1=>integers(2)};
value(nested_hamt) -> #{1=>integers(33)};
value(map_keys) -> maps:from_list([{#{I=>I},I} || I <- lists:seq(0,32)]);
value(atom_flat) -> #{map_layout_zeta=>1,map_layout_alpha=>2};
value(collision_first) -> collision_first();
value(collision_second) -> collision_second();
value(collision_map) -> collision_map();
value(collision_tuple) -> {[collision_first()|1001],[collision_second()|1002]}.
collision_first() -> <<0:256>>.
collision_second() ->
    <<1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
      191,126,234,150,110,105,130,163,61,74,218,232,169,133,62,97>>.
collision_map() ->
    Pairs = [{I,I} || I <- lists:seq(0,30)],
    maps:from_list([{collision_first(),1001},{collision_second(),1002}|Pairs]).
