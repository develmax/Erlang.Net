#!/usr/bin/env escript
-mode(compile).

main([Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
        [{Relative, {Name, Arity}, Fields}
         || {function, Name, Arity, _, Body} <- Functions,
            %% A trim changes Y numbering, so slot identities cannot be compared across it.
            not lists:any(fun(I) -> element(1, I) =:= trim end, [I || I <- Body, is_tuple(I)]),
            Reads <- [sets:from_list(lists:flatmap(fun read_registers/1, Body))],
            Fields <- [[{Position, Slot} || {get_tuple_element, _, Position, {y, Slot}} <- Body,
                                           not sets:is_element(Slot, Reads)]],
            Fields =/= []]
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    io:format("~p functions in ~p images extract tuple fields to Y slots with no other use in the function.~n",
              [length(Rows), length(Modules)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

read_registers({get_tuple_element, Source, _, _}) ->
    registers(Source);
read_registers({init_yregs, _}) ->
    [];
read_registers({move, Source, _}) ->
    registers(Source);
read_registers(Instruction) ->
    registers(Instruction).

registers({y, Slot}) ->
    [Slot];
registers(Tuple) when is_tuple(Tuple) ->
    lists:flatmap(fun registers/1, tuple_to_list(Tuple));
registers([Head|Tail]) ->
    registers(Head) ++ registers(Tail);
registers(_) ->
    [].
