-module(beam_trim_list_branch).
-export([run/2]).
run(Bin, Size) when Size =< 0 ->
    [identity(Bin)];
run(Bin, Size) when Size > 0 ->
    Head = identity(Bin),
    Count = byte_size(Head),
    if
        Count > Size ->
            note(Size),
            {Prefix, _Rest} = split_binary(Head, Size),
            [Prefix];
        Count =:= Size ->
            note(Size),
            [Head];
        true ->
            [identity(Bin)]
    end.
identity(Bin) -> Bin.
note(Size) -> put(last_size, Size).
