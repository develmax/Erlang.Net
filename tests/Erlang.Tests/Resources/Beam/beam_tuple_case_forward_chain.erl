-module(beam_tuple_case_forward_chain).
-export([take/2]).

take(Key, Tail) ->
    case init:get_argument(Key) of
        error -> {Key, []};
        {ok, Rows} ->
            Flat = lists:append(Rows),
            Result = lists:append(Flat, Tail),
            {Key, Result}
    end.
