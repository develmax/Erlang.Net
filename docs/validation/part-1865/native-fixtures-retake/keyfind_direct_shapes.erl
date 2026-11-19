-module(keyfind_direct_shapes).
-export([project/1, tail/1, wildcard/1, other_position/1]).

project(Rows) ->
    {entry, Value} = lists:keyfind(entry, 1, Rows),
    Value.

tail(Rows) ->
    {entry, Value} = lists:keyfind(entry, 1, Rows),
    forward(Value).

wildcard(Rows) ->
    {_, Value} = lists:keyfind(entry, 1, Rows),
    Value.

other_position(Rows) ->
    {entry, Value} = lists:keyfind(entry, 2, Rows),
    Value.

forward(Value) -> {accepted, Value}.
