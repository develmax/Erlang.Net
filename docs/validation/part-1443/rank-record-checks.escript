#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Excess}
         || {Key, Body} <- maps:to_list(Actual),
            Excess <- [checked_records(Body) -- tag_tests(maps:get(Key, Ref, []))],
            Excess =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images have more explicit badrecord checks for the same record shape than all OTP tag tests for that shape.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

tag_tests(Body) ->
    [{Arity, Tag} || {test, is_tagged_tuple, _, [_, Arity, {atom, Tag}]} <- Body].

checked_records([{test, is_tagged_tuple, {f, Failure}, [Register, Arity, {atom, Tag}]},
                 {jump, {f, Valid}}, {label, Failure}, {badrecord, Register}, {label, Valid}|Rest]) ->
    [{Arity, Tag}|checked_records(Rest)];
checked_records([_|Rest]) ->
    checked_records(Rest);
checked_records([]) ->
    [].
