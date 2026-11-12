#!/usr/bin/env escript
-mode(compile).

main([Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
        [{Relative, {Name, Arity}, length(Dead)}
         || {function, Name, Arity, _, Body} <- Functions,
            Dead <- [dead_projections(Body)],
            Dead =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images project a tuple field immediately before a heap check that drops its X register and a tuple construction.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

dead_projections([{get_tuple_element, {x, 0}, _, {x, Register}},
                  {test_heap, _, Live},
                  {put_tuple2, {x, 0}, {list, Operands}}|Rest]) when Register >= Live ->
    case lists:member({x, Register}, Operands) of
        true -> dead_projections(Rest);
        false -> [Register|dead_projections(Rest)]
    end;
dead_projections([_|Rest]) ->
    dead_projections(Rest);
dead_projections([]) ->
    [].
