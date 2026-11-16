#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Expected = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Overlap}
         || {Key, Ops} <- maps:to_list(Expected),
            {ok, GeneratedOps} <- [maps:find(Key, Actual)],
            Overlap <- [overlap(Ops, GeneratedOps)],
            Overlap >= 2]
    end, Files),
    io:format("~p functions in ~p images contain OTP atom jump-table keys tested individually in C#.~n",
        [length(Rows), length(lists:usort([Path || {Path, _, _} <- Rows]))]),
    Sorted = lists:sort(fun({PathA, KeyA, A}, {PathB, KeyB, B}) ->
        {-A, PathA, KeyA} < {-B, PathB, KeyB}
    end, Rows),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sublist(Sorted, 50)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Ops} || {function, Name, Arity, _, Ops} <- Functions]).

overlap(Expected, Actual) ->
    Existing = sets:from_list(lists:append(jump_tables(Actual))),
    Tests = sets:from_list([Atom || {test, is_eq_exact, _, [_, {atom, Atom}]} <- Actual]),
    LinearOnly = sets:subtract(Tests, Existing),
    lists:max([0|[sets:size(sets:intersection(sets:from_list(Keys), LinearOnly))
        || Keys <- jump_tables(Expected)]]).

jump_tables(Operations) ->
    [[Atom || {atom, Atom} <- Entries]
     || {select_val, _, _, {list, Entries}} <- Operations].
