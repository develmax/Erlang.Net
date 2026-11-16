#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated, Hint]) ->
    Expected = list_to_existing_atom(Hint),
    Other = case Expected of copy -> reuse; reuse -> copy end,
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Cases}
         || {Key, Body} <- maps:to_list(Actual),
            RefHints <- [updates(Expected, maps:get(Key, Ref, []))],
            ActualHints <- [updates(Expected, Body)],
            Deficit <- [RefHints -- ActualHints],
            Cases <- [lists:usort([Shape || Shape <- updates(Other, Body), lists:member(Shape, Deficit)])], Cases =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images use update_record ~p and lack a matching OTP ~p form.~n",
              [length(Rows), length(Modules), Other, Expected]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

updates(Hint, Body) ->
    [{Arity, indices(Pairs)}
     || {update_record, {atom, Mode}, Arity, _, _, {list, Pairs}} <- Body, Mode =:= Hint].

indices([Index, _|Rest]) ->
    [Index|indices(Rest)];
indices([]) ->
    [].
