#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, lists:usort(Arity)}
         || {Key, Body} <- maps:to_list(Actual),
            Expected <- [x_arities(maps:get(Key, Ref, []))],
            Arity <- [[A || A <- spilled_arities(Body), lists:member(A, Expected)]],
            Arity =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images move a tuple to Y before a shape and arity test for which OTP has an X-register form.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

x_arities([{test, is_tuple, Fail, [{x, _} = Value]},
           {test, test_arity, Fail, [Value, Arity]} | Rest]) ->
    [Arity | x_arities(Rest)];
x_arities([_|Rest]) ->
    x_arities(Rest);
x_arities([]) ->
    [].

spilled_arities([{move, {x, _}, {y, _} = Value},
                 {test, is_tuple, Fail, [Value]},
                 {test, test_arity, Fail, [Value, Arity]} | Rest]) ->
    [Arity | spilled_arities(Rest)];
spilled_arities([_|Rest]) ->
    spilled_arities(Rest);
spilled_arities([]) ->
    [].
