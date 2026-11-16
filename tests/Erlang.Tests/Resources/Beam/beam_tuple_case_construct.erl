-module(beam_tuple_case_construct).
-export([find/2, with_default/3]).

find(Key, Rows) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {value, Value};
        false -> not_found
    end.

with_default(Key, Rows, Default) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Value}} -> {Default, Value};
        false -> missing
    end.
