-module(beam_case_constructor_spill_probe).
-export([preserve/2]).

preserve([Head|Tail], Flags) ->
    case lists:member(Head, Flags) of
        true ->
            Left = lists:reverse(Tail),
            Right = lists:append(Left,[Head]),
            {Left,Right};
        false ->
            Tail
    end;
preserve([], _) ->
    [].
