-module(beam_tuple_heap).
-export([category/1]).
category(Ref) ->
    case erlang:get(Ref) of
        {definition, Pos, Mod, Name} ->
            {class, {reference, Pos, Mod, Name}};
        _ ->
            type
    end.
