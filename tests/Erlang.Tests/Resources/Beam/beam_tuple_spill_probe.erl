-module(beam_tuple_spill_probe).
-export([carried/1, tagged/1, local/2, swapped/1, used_twice/1, successive/1]).

carried(Props) ->
    {A, B} = proplists:get_value(key, Props),
    C = lists:reverse(B),
    lists:append(A, C).

tagged(Props) ->
    {ok, A, B} = proplists:get_value(key, Props),
    C = lists:reverse(B),
    lists:append(A, C).

local(A, B) ->
    {Left, Right} = make_pair(A, B),
    Tail = lists:reverse(Right),
    lists:append(Left, Tail).

make_pair(A, B) ->
    {lists:reverse(A), B}.

swapped(Props) ->
    {A, B} = proplists:get_value(key, Props),
    C = lists:reverse(A),
    lists:append(B, C).

used_twice(Props) ->
    {A, B} = proplists:get_value(key, Props),
    C = lists:append(A, B),
    lists:append(A, C).

successive(Props) ->
    {A, B} = proplists:get_value(key, Props),
    {C, D} = proplists:get_value(key, B),
    E = lists:append(A, C),
    lists:append(D, E).

