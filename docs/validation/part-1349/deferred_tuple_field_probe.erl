-module(deferred_tuple_field_probe).
-export([normalize/1, normalize_value/1]).

normalize([{node, Key, Value} | Tail]) when is_atom(Key) ->
    [{Key, expand(Value)} | normalize(Tail)];
normalize([Pair = {Key, _} | Tail]) when is_atom(Key) ->
    [Pair | normalize(Tail)];
normalize([]) ->
    [].

normalize_value([{node, Key, Value} | Tail]) when is_atom(Value) ->
    [{Key, expand(Value)} | normalize_value(Tail)];
normalize_value([Pair = {Key, _} | Tail]) when is_atom(Key) ->
    [Pair | normalize_value(Tail)];
normalize_value([]) ->
    [].

expand(Value) ->
    {expanded, Value}.
