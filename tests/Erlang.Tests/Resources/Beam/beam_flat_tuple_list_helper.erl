-module(beam_flat_tuple_list_helper).
-export([transform/1, boxed/1, captured/2, bad_input/1, observed/1]).

transform(Values) ->
    [{tag, X, 7, X} || X <- Values].

boxed(Values) ->
    [{X} || X <- Values].

captured(Values, Other) ->
    [{Other, X} || X <- Values].

bad_input(Values) ->
    try transform(Values)
    catch error:{bad_generator, Tail} -> Tail
    end.

observed(Values) ->
    put(events, []),
    try [{seen, observe(X)} || X <- Values]
    catch error:Reason -> {Reason, get(events)}
    end.

observe(X) ->
    put(events, [X | get(events)]),
    case X of
        stop -> erlang:error(selected);
        _ -> X
    end.
