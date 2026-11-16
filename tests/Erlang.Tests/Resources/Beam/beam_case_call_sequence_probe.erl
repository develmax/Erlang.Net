-module(beam_case_call_sequence_probe).
-export([walk/2]).

walk([Head|Tail], Flags) ->
    case lists:member(Head, Flags) of
        true ->
            Reversed = lists:reverse(Tail),
            lists:append(Reversed, [Head]);
        false ->
            Tail
    end;
walk([], _) ->
    [].
