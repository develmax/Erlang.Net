-module(beam_lookup_result_type).
-export([read/2, classify/2]).
-import(lists, [keyfind/3]).

read(Key, Rows) ->
    {_, Value} = lookup(Key, Rows),
    Value.

lookup(Key, Rows) ->
    keyfind(Key, 1, Rows).

classify(Key, Map) ->
    case find(Key, Map) of
        {ok, Value} -> Value;
        _ -> absent
    end.

find(Key, Map) ->
    maps:find(Key, Map).
