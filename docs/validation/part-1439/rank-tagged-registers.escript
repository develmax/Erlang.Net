#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Pairs}
         || {Key, Body} <- maps:to_list(Actual),
            Expected <- [x_tests(maps:get(Key, Ref, []))],
            Pairs <- [[{Kind, Arity, Tag} || {Kind, Arity, Tag} <- spilled_tests(Body),
                         lists:member({Arity, Tag}, Expected)]],
            Pairs =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images spill a tuple immediately before testing its tag, where OTP has an X-register test of the same arity and tag.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Kind) ->
        Matches = [Path || {Path, _, Pairs} <- Rows,
                           lists:any(fun({K, _, _}) -> Kind =:= K end, Pairs)],
        io:format("~p: ~p functions in ~p images.~n", [Kind, length(Matches), length(lists:usort(Matches))])
    end, [selector_move, nested_field]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

x_tests(Body) ->
    [{Arity, Tag} || {test, is_tagged_tuple, _, [{x, _}, Arity, {atom, Tag}]} <- Body].

spilled_tests([Previous,
               {test, is_tagged_tuple, _, [{y, _} = Register, Arity, {atom, Tag}]} = Test | Rest]) ->
    case Previous of
        {move, {x, _}, Register} ->
            [{selector_move, Arity, Tag} | spilled_tests(Rest)];
        {get_tuple_element, _, _, Register} ->
            [{nested_field, Arity, Tag} | spilled_tests(Rest)];
        _ ->
            spilled_tests([Test | Rest])
    end;
spilled_tests([_|Rest]) ->
    spilled_tests(Rest);
spilled_tests([]) ->
    [].
