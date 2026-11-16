#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Ref = instructions(filename:join(Reference, Relative)),
        Actual = instructions(Path),
        [ {Relative, Key, Expected, Emitted, Tagged}
          || {Key, {Expected, _}} <- maps:to_list(Ref),
             {ok, {Emitted, Tagged}} <- [maps:find(Key, Actual)],
             Expected > Emitted ]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _, _, _} <- Rows]),
    io:format("~p functions in ~p release images emit fewer select_tuple_arity instructions than OTP.~n",
              [length(Rows), length(Modules)]),
    TaggedRows = [Path || {Path, _, _, _, Tagged} <- Rows, Tagged >= 2],
    io:format("~p of those functions in ~p images emit at least two is_tagged_tuple tests.~n",
              [length(TaggedRows), length(lists:usort(TaggedRows))]),
    lists:foreach(fun(Row) -> io:format("~0p~n",[Row]) end, lists:sort(Rows)).

instructions(Path) ->
    case beam_disasm:file(Path) of
        {beam_file, _, _, _, _, Functions} ->
            maps:from_list([{{Name, Arity}, {length([ok || {select_tuple_arity, _, _, _} <- Body]),
                                                length([ok || {test, is_tagged_tuple, _, _} <- Body])}}
                            || {function, Name, Arity, _, Body} <- Functions]);
        _ ->
            #{}
    end.
