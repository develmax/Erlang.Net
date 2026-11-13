-module(beam_split_singleton).
-export([run/2]).
run(Bin, Size) ->
    if
        Size >= 0 ->
            note(Size),
            {First, _Rest} = split_binary(Bin, Size),
            [First];
        true -> []
    end.
note(Size) -> put(last_size, Size).
