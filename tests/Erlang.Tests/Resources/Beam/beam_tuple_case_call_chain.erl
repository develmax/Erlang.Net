-module(beam_tuple_case_call_chain).
-export([take/3]).

take(Default, Rows, Key) ->
    case lists:keysearch(Key, 1, Rows) of
        {value, {Key, Payload}} ->
            Rest = lists:keydelete(Key, 1, Rows),
            Sorted = lists:reverse(Rest),
            {Payload, Sorted};
        false -> {Default, Rows}
    end.
