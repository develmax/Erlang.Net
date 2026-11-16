#!/usr/bin/env escript
-mode(compile).

main([Root, Previous, Current, Changed]) ->
    {ok, Bin} = file:read_file(Changed),
    Paths = [unicode:characters_to_list(Line) ||
                Line <- binary:split(Bin, <<10>>, [global]), Line =/= <<>>],
    Rows = lists:append([compare(Path, Root, Previous, Current) || Path <- Paths]),
    Improved = [Row || Row={_,_,Reference,Old,New} <- Rows,
                      abs(New-Reference) < abs(Old-Reference)],
    Reached = [Row || Row={_,_,Reference,_,New} <- Improved, New =:= Reference],
    Regressed = [Row || Row={_,_,Reference,Old,New} <- Rows,
                       abs(New-Reference) > abs(Old-Reference)],
    io:format("Frame changes in ~B functions across ~B images.~n", [length(Rows), count_images(Rows)]),
    io:format("Closer ~B across ~B images, exact ~B, farther ~B across ~B images.~n",
              [length(Improved), count_images(Improved), length(Reached),
               length(Regressed), count_images(Regressed)]),
    lists:foreach(fun(Row) -> io:format("~p~n", [Row]) end, Rows).

compare(Path, Root, Previous, Current) ->
    Reference = frames(filename:join(Root, Path)),
    Old = generated(Previous, Path, Reference),
    New = generated(Current, Path, Reference),
    [{Path, Function, Size, Before, After} ||
        {Function, Size} <- maps:to_list(Reference),
        {ok, Before} <- [maps:find(Function, Old)],
        {ok, After} <- [maps:find(Function, New)],
        Before =/= After].

generated(Directory, Path, Reference) ->
    Full = filename:join(Directory, Path),
    case filelib:is_regular(Full) of
        true -> frames(Full);
        false -> Reference
    end.

frames(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, peak(Instructions)} ||
                       {function, Name, Arity, _, Instructions} <- Functions]).

peak(Instructions) ->
    lists:max([0 | [element(2, I) || I <- Instructions, is_tuple(I),
                    element(1, I) =:= allocate orelse element(1, I) =:= allocate_heap]]).

count_images(Rows) ->
    length(lists:usort([Path || {Path,_,_,_,_} <- Rows])).
