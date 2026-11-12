-module(case_tuple_sequence_probe).
-export([pair/2, preserve/2]).

pair([Head|Tail], Flags) ->
    case lists:member(Head, Flags) of
        true ->
            Left = lists:reverse(Tail),
            Right = erlang:atom_to_list(Head),
            {Left,Right};
        false ->
            Tail
    end;
pair([], _) ->
    [].

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
