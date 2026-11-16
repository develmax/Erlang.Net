-module(case_call_sequence_probe).
-export([walk/2, bound/2]).

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

bound(Key, Rows) ->
    case lists:keyfind(Key, 1, Rows) of
        false ->
            missing;
        Pair ->
            Values = erlang:tuple_to_list(Pair),
            lists:reverse(Values)
    end.
