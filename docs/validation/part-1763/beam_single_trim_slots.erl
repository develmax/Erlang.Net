-module(beam_single_trim_slots).
-export([consume/2, pair/2, lookup/2, unpack/2]).
-record(state, {connection, tag, mode}).
-record(box, {value}).

consume(_, #state{connection = Connection}) ->
    close(Connection),
    ok.

pair(#box{value = Left}, #box{value = Right}) ->
    combine(Left, Right).

lookup(Key, #box{value = Map}) ->
    maps:get(Key, Map, #{}).

unpack(_, {Format, Args}) ->
    io_lib:format(Format, Args).

close(Connection) -> put(closed, Connection).
combine(Left, Right) -> {Left, Right}.