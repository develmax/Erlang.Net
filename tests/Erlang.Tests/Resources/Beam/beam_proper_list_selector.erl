-module(beam_proper_list_selector).
-export([flat/1, filtered/2, difference/2, unknown_tail/2]).

flat(Items) ->
    case lists:flatten(Items) of
        [] -> empty;
        _ -> present
    end.

filtered(Items, Keep) ->
    case lists:filter(Keep, Items) of
        [] -> empty;
        _ -> present
    end.

difference(Left, Right) ->
    case Left -- Right of
        [] -> empty;
        _ -> present
    end.

unknown_tail(Items, Tail) ->
    case lists:reverse(Items, Tail) of
        [] -> empty;
        _ -> present
    end.
