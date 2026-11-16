-module(beam_tuple_fields_chain_probe).
-export([chain/1, tagged/1, shuffled/1, known/2, chain_twice/1, known_chain/2]).

chain(Props) ->
    {A, B} = proplists:get_value(key, Props),
    lists:append(A, B).

tagged(Props) ->
    {ok, A, B} = proplists:get_value(key, Props),
    lists:append(A, B).

shuffled(Props) ->
    {A, B, C} = proplists:get_value(key, Props),
    lists:zip3(C, A, B).

known(A, B) ->
    {Left, Right} = make_pair(A, B),
    lists:append(Left, Right).

make_pair(A, B) ->
    {lists:reverse(A), B}.

chain_twice(Props) ->
    {A, B} = proplists:get_value(key, Props),
    {C, D} = proplists:get_value(key, lists:append(A, B)),
    lists:append(C, D).

known_chain(A, B) ->
    {Left, Right} = make_pair(A, B),
    {C, D} = proplists:get_value(key, lists:append(Left, Right)),
    lists:append(C, D).
