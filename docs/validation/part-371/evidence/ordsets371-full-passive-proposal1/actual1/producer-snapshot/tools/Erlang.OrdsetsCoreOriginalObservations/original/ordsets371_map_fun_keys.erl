-module(ordsets371_map_fun_keys).
-export([cases/0,run/0,run_stacks/0,probe/1]).
cases() -> [fun_map_is_set,fun_map_is_element,fun_map_add_element,fun_map_del_element,fun_map_from_list,fun_map_union_two,fun_map_union_many,fun_map_intersection_two,fun_map_intersection_many,fun_map_is_disjoint,fun_map_subtract,fun_map_is_subset,fun_map_map,fun_map_filtermap].
run() -> ordsets_core_support:run(ordsets371_map_fun_keys,cases()).
run_stacks() -> ordsets_core_support:profiles(ordsets371_map_fun_keys,cases()).
probe(fun_map_is_set) -> {First,Second}=maps_pair(), ordsets:is_set([First,Second]);
probe(fun_map_is_element) -> {First,Second}=maps_pair(), ordsets:is_element(Second,[First,Second]);
probe(fun_map_add_element) -> {First,Second}=maps_pair(), ordsets:add_element(Second,[First]);
probe(fun_map_del_element) -> {First,Second}=maps_pair(), ordsets:del_element(Second,[First,Second]);
probe(fun_map_from_list) -> {First,Second}=maps_pair(), ordsets:from_list([Second,First]);
probe(fun_map_union_two) -> {First,Second}=maps_pair(), ordsets:union([First],[Second]);
probe(fun_map_union_many) -> {First,Second}=maps_pair(), ordsets:union([[First],[Second]]);
probe(fun_map_intersection_two) -> {First,Second}=maps_pair(), ordsets:intersection([First,Second],[Second]);
probe(fun_map_intersection_many) -> {First,Second}=maps_pair(), ordsets:intersection([[First,Second],[Second]]);
probe(fun_map_is_disjoint) -> {First,Second}=maps_pair(), ordsets:is_disjoint([First,Second],[Second]);
probe(fun_map_subtract) -> {First,Second}=maps_pair(), ordsets:subtract([First,Second],[Second]);
probe(fun_map_is_subset) -> {First,Second}=maps_pair(), ordsets:is_subset([Second],[First,Second]);
probe(fun_map_map) -> {First,Second}=maps_pair(), ordsets:map(fun(X)->X end,[First,Second]);
probe(fun_map_filtermap) -> {First,Second}=maps_pair(), ordsets:filtermap(fun(X)->{true,X} end,[First,Second]).
maps_pair() ->
    A=local_stub(255,1),
    B=local_stub(0,2),
    {uniq,1}=erlang:fun_info(A,uniq),
    {uniq,2}=erlang:fun_info(B,uniq),
    true=(A =/= B),
    {#{A=>0,B=>1},#{A=>1,B=>0}}.
local_stub(Byte,OldUnique) ->
    Unique=binary:copy(<<Byte>>,16),
    <<131,Module/binary>>=erlang:term_to_binary(ordsets371_map_key_stub),
    <<131,OldIndex/binary>>=erlang:term_to_binary(0),
    <<131,OldUniq/binary>>=erlang:term_to_binary(OldUnique),
    Owner = <<88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>,
    Body= <<0:8,Unique/binary,0:32,0:32,Module/binary,OldIndex/binary,OldUniq/binary,Owner/binary>>,
    Size=byte_size(Body)+4,
    erlang:binary_to_term(<<131,112,Size:32,Body/binary>>).
