-module(beam_deferred_tuple_probe).
-export([both/1, tagged/1, mixed/1, known/2]).

both(Props) ->
    {A, B} = proplists:get_value(key, Props),
    C = lists:reverse(Props),
    lists:zip3(A, B, C).

tagged(Props) ->
    {ok, A, B} = proplists:get_value(key, Props),
    C = lists:seq(1, 1),
    lists:zip3(A, B, C).

mixed(Props) ->
    {A, B, C} = proplists:get_value(key, Props),
    D = lists:reverse(C),
    lists:zip3(A, B, D).

known(A, B) ->
    {Left, Right} = make_pair(A, B),
    Tail = lists:seq(1, 1),
    lists:zip3(Left, Right, Tail).

make_pair(A, B) ->
    {lists:reverse(A), B}.

