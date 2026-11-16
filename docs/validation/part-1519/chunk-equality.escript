#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    {ok, _, Expected} = beam_lib:all_chunks(Reference),
    {ok, _, Actual} = beam_lib:all_chunks(Candidate),
    Keys = lists:usort([Id || {Id, _} <- Expected ++ Actual]),
    [io:format("~s ~s ~p ~p~n", [Key, compare(E, A), size_of(E), size_of(A)]) ||
        Key <- Keys, E <- [proplists:get_value(Key, Expected)], A <- [proplists:get_value(Key, Actual)]].

compare(A, A) -> "Equal";
compare(_, _) -> "Different".

size_of(undefined) -> absent;
size_of(Chunk) -> byte_size(Chunk).
