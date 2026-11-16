#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        Ref = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, lists:usort(Fields)}
         || {Key, Body} <- maps:to_list(Actual),
            Expected <- [maps:get(Key, Ref, [])],
            Fields <- [[{Index, Destination}
                        || {Index, Destination} <- moved_fields(Body) -- moved_fields(Expected),
                           lists:any(fun({get_tuple_element, _, I, D}) -> I =:= Index andalso D =:= Destination;
                                        (_) -> false
                                     end, Expected)]],
            Fields =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images move a projected field to an X register where OTP projects that field index directly to the destination.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Body} || {function, Name, Arity, _, Body} <- Functions]).

moved_fields([{get_tuple_element, _, Index, Temporary},
              {move, Temporary, {x, _} = Destination}|Rest]) ->
    [{Index, Destination}|moved_fields(Rest)];
moved_fields([_|Rest]) ->
    moved_fields(Rest);
moved_fields([]) ->
    [].
