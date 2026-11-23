-module(beam_conditional_list_helper).
-export([transform/1, bad_input/1, effects/0, selected_failure/0]).

-define(MAP(X), if X >= 10, X =< 20 -> X - (1 + 2); true -> X end).

transform(Values) ->
    [?MAP(X) || X <- Values].

bad_input(Values) ->
    try transform(Values)
    catch error:{bad_generator, Tail} -> Tail
    end.

effects() ->
    put(events, []),
    Result = [if X >= 10, X =< 20 -> effect(X); true -> X end
              || X <- [9, 20, 10, atom, 21]],
    {Result, get(events)}.

selected_failure() ->
    put(events, []),
    try [if X >= 10, X =< 20 -> effect(X); true -> X end || X <- [11, 15, 17]]
    catch error:selected -> get(events)
    end.

effect(X) ->
    put(events, [X | get(events)]),
    case X of
        15 -> erlang:error(selected);
        _ -> X - 3
    end.
