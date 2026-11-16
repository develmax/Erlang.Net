-module(beam_tagged_lookup_projection).
-export([list/2, map/2]).

list(Key, Rows) ->
    case lists:keyfind(Key, 1, Rows) of
        {hit, Value} -> Value;
        _ -> absent
    end.

map(Key, Map) ->
    case lookup(Key, Map) of
        {ok, Value} -> Value;
        _ -> absent
    end.

lookup(Key, Map) -> maps:find(Key, Map).
