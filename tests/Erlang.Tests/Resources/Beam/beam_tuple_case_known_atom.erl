-module(beam_tuple_case_known_atom).
-export([find/2, other/2]).

find(Key, Rows) ->
    case lists:keyfind(Key, 1, Rows) of
        {Key, Value} -> {value, Value};
        false -> missing
    end.

other(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {value, Value};
        missing -> absent
    end.
