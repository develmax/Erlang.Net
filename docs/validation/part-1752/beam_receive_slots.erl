-module(beam_receive_slots).
-export([notify/2, pick/2]).

notify(Tag, Timeout) ->
    Ref = make_ref(),
    put(ref, Ref),
    receive
        {Tag, Result} ->
            put(result, Result),
            {ok, Result}
    after Timeout ->
        {timeout, Ref}
    end.

pick(Tag, Timeout) ->
    receive
        {Tag, X} ->
            Old = put(x, X),
            {chosen, Old};
        {Tag, X, Y} ->
            Old = put(y, Y),
            {X, Old}
    after Timeout ->
        none
    end.

