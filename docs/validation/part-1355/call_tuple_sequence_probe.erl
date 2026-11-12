-module(call_tuple_sequence_probe).
-export([run/1, tokens/1, typed/1, pair/2, once/1, checked/1, literal/2]).

run(Input) ->
    Values = lists:reverse(Input),
    lists:foreach(fun record_item/1, Values),
    {ok, Input}.

tokens(Value) ->
    Tokens = string:tokens(Value, " "),
    lists:foreach(fun record_item/1, Tokens),
    {ok, Value}.

typed(Input) ->
    Values = lists:reverse(Input),
    lists:foreach(fun typed_item/1, Values),
    {ok, Input}.

pair(Input, Key) ->
    record_item(Key),
    erlang:put(Key, Input),
    {ok, Input}.

once(Input) ->
    record_item(Input),
    {done, Input}.

checked(Input) ->
    Input = typed_item(Input),
    {done, Input}.

record_item(Item) ->
    erlang:put(last_item, Item),
    Item.

typed_item(Item) ->
    erlang:put(last_item, Item),
    marker.

literal(Input, _Ignored) ->
    record_item(Input),
    {done, constant}.

