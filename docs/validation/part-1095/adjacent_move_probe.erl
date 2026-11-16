-module(adjacent_move_probe).
-export([adjacent/1, clobbered/1, branch/2]).
adjacent(Value) ->
    Old = erlang:put(key, Value),
    erlang:put(observed, stamp(Old)),
    {Old, Value}.
clobbered(Value) ->
    Old = erlang:put(key, Value),
    erlang:put(observed, other),
    {stamp(Old), Value}.
branch(Value, Mode) ->
    Old = erlang:put(key, Value),
    case Mode of
        yes -> stamp(Old);
        _ -> Old
    end.
stamp(Item) -> {stamped, Item}.
