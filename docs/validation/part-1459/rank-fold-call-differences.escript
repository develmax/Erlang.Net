#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Expected = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Count, maps:get(Key, Actual)}
         || {Key, Count} <- maps:to_list(Expected),
            maps:is_key(Key, Actual), Count > maps:get(Key, Actual)]
    end, Files),
    Images = lists:usort([Path || {Path, _, _, _} <- Rows]),
    io:format("~B functions in ~B images have fewer direct lists:foldl/3 calls than OTP.~n",
        [length(Rows), length(Images)]),
    Sorted = lists:sort(fun({PathA, KeyA, ExpectedA, ActualA}, {PathB, KeyB, ExpectedB, ActualB}) ->
        {ActualA-ExpectedA, PathA, KeyA} < {ActualB-ExpectedB, PathB, KeyB}
    end, Rows),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sublist(Sorted, 30)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, length(lists:filter(fun is_fold_call/1, Ops))}
                    || {function, Name, Arity, _, Ops} <- Functions]).

is_fold_call({Opcode, 3, {extfunc, lists, foldl, 3}})
  when Opcode =:= call_ext; Opcode =:= call_ext_only -> true;
is_fold_call({call_ext_last, 3, {extfunc, lists, foldl, 3}, _}) -> true;
is_fold_call(_) -> false.
