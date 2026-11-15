-module(beam_entry_lifetimes).
-export([pack/2, branch/2]).

pack(First, Second) ->
    Pair = {First, Second},
    Old = put(capture, Pair),
    {Old, put(next, Old)}.

branch(Flag, Value) ->
    case Flag of
        yes ->
            Old = put(Flag, Value),
            {Old, put(next, Old)};
        no -> Value
    end.
