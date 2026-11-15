-module(beam_map_binary_slots).
-export([associate/4, exact/4, lookup/4, binary/3]).

associate(Map, Key, Value, Keep) ->
    Updated = Map#{Key => Value},
    Old = put(first, Updated),
    {Old, Keep, put(next, Old)}.

exact(Map, Key, Value, Keep) ->
    Old = put(first, Value),
    Updated = Map#{Key := Old},
    {Updated, Keep, put(next, Updated)}.

lookup(Map, Key, Fallback, Keep) ->
    Snapshot = put(first, Map),
    case Snapshot of
        #{Key := A, fixed := B} -> C = put(second, A), {A, B, C, Keep};
        _ -> {Fallback, Keep}
    end.

binary(Bin, Number, Keep) ->
    Old = put(next, Bin),
    {<<Old/binary, Number:16>>, Keep, put(next, Old)}.
