-module(beam_flat_tuple_list_helper).
-export([transform/1, boxed/1, captured/2, bad_input/1, observed/1,
         prefixed/2, paired/2, branch_element/2, flat_failure/2]).

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

prefixed(Values, Prefix) ->
    [Prefix ++ word(X) || X <- Values].

paired(Values, Marker) ->
    [{word(X), Marker} || X <- Values].

branch_element(Values, Marker) ->
    [{Marker, case X of {value, Bound} -> Bound; _ -> X end} || X <- Values].

flat_failure(Values, Prefix) ->
    put(events, []),
    try prefixed(Values, Prefix)
    catch error:Reason -> {Reason, get(events)}
    end.

word(X) ->
    [observe(X)].

observe(X) ->
    put(events, [X | get(events)]),
    case X of
        stop -> erlang:error(selected);
        _ -> X
    end.
