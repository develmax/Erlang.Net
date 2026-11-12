#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Ref = counts(filename:join(Reference, Relative)),
        Actual = counts(Path),
        [{Relative, Key, Expected, Emitted}
         || {Key, Expected} <- maps:to_list(Ref),
            {ok, Emitted} <- [maps:find(Key, Actual)],
            Expected > Emitted]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _, _} <- Rows]),
    io:format("~p functions in ~p images emit fewer tuple tag selections than OTP.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

counts(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, tag_selections(Body, 0)}
                    || {function, Name, Arity, _, Body} <- Functions]).

tag_selections([{get_tuple_element, _, 0, Register},
                {select_val, Register, _, {list, [{atom, _}, _|_] = Pairs}}|Rest], Count) ->
    case atom_cases(Pairs) of
        true -> tag_selections(Rest, Count + 1);
        false -> tag_selections(Rest, Count)
    end;
tag_selections([_|Rest], Count) ->
    tag_selections(Rest, Count);
tag_selections([], Count) ->
    Count.

atom_cases([{atom, _}, {f, _}|Rest]) ->
    atom_cases(Rest);
atom_cases([]) ->
    true;
atom_cases(_) ->
    false.
