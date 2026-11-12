#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        {beam_file, _, _, _, _, Ref} = beam_disasm:file(filename:join(Reference, Relative)),
        {beam_file, _, _, _, _, Actual} = beam_disasm:file(Path),
        Shapes = maps:from_list([{{Name, Arity}, or_tests(Body)}
                                || {function, Name, Arity, _, Body} <- Ref]),
        [{Relative, {Name, Arity}, Pairs}
         || {function, Name, Arity, _, Body} <- Actual,
            Bifs <- [[Bif || {bif, Bif, _, _, _} <- Body]],
            Pairs <- [[{First, Second}
                       || {First, Second} <- maps:get({Name, Arity}, Shapes, []),
                          lists:member(First, Bifs), lists:member(Second, Bifs)]],
            Pairs =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images have short-circuit guard type tests in OTP and the corresponding value-producing BIFs in C#.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

or_tests([{test, First, {f, Alternative}, [Value]},
          {jump, {f, Success}},
          {label, Alternative},
          {test, Second, {f, _}, [Value]},
          {label, Success} | Rest]) ->
    [{First, Second}|or_tests(Rest)];
or_tests([_|Rest]) ->
    or_tests(Rest);
or_tests([]) ->
    [].
