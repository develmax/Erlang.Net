-module(beam_tuple_case_continuation).
-export([take/3]).

take(Key, Rows, Default) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Payload}} ->
            Rest = lists:keydelete(Key, 1, Rows),
            {Payload, Rest};
        false -> {Default, Rows}
    end.
