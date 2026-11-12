-module(beam_checked_tuple_chain_probe).
-export([chain/1, tuple/1, decide/1]).

chain(Left) ->
    {_, First} = proplists:get_value(key, Left),
    {Second, _} = proplists:get_value(key, First),
    lists:reverse(Second).

tuple(Left) ->
    {_, First} = proplists:get_value(key, Left),
    {Second, _} = proplists:get_value(key, First),
    {ok, Second}.

decide(Left) ->
    {_, First} = proplists:get_value(key, Left),
    {Second, _} = proplists:get_value(key, First),
    case lists:member(a, Second) of
        true -> yes;
        false -> no
    end.
