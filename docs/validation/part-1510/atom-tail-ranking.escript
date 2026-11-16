#!/usr/bin/env escript
-mode(compile).

main([Root, Generated, PathFile]) ->
    {ok, Data} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Path) || Path <- binary:split(Data, <<10>>, [global]),
                                                 Path =/= <<>>],
    Rows = lists:flatmap(fun(Path) -> candidates(Root, Generated, Path) end, Paths),
    io:format("~B modules, ~B functions with atom arms, local tails, and an exit fallback~n",
              [length(lists:usort([Path || {Path, _, _, _} <- Rows])), length(Rows)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, Rows).

candidates(Root, Generated, Path) ->
    Release = filename:join(Root, Path),
    case beam_lib:chunks(Release, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Shapes = [{{Name, Arity}, Count} || {function, _, Name, Arity, Clauses} <- Forms,
                       Count <- terminal_case(Clauses)],
            compare_frames(Shapes, Release, filename:join(Generated, Path), Path);
        _ -> []
    end.

terminal_case([{clause, _, Arguments, [], [{'case', _, {var, _, Selector}, Arms}]}]) ->
    {Literals, Fallback} = lists:split(length(Arms) - 1, Arms),
    case length(Literals) >= 2 andalso lists:all(fun atom_tail/1, Literals)
         andalso lists:member(Selector, [Name || {var, _, Name} <- Arguments])
         andalso lists:all(fun({var, _, _}) -> true; (_) -> false end, Arguments)
         andalso exit_fallback(Fallback) of
        true -> [length(Literals)];
        false -> []
    end;
terminal_case(_) -> [].

atom_tail({clause, _, [{atom, _, _}], [], [{call, _, {atom, _, _}, Arguments}]}) ->
    lists:all(fun(Argument) -> element(1, Argument) =:= var orelse erl_syntax:is_literal(Argument) end, Arguments);
atom_tail(_) -> false.

exit_fallback([{clause, _, [{var, _, _}], [], [{call, _, {atom, _, exit}, [{tuple, _, _}]}]}]) -> true;
exit_fallback([{clause, _, [{var, _, _}], [],
                [{call, _, {remote, _, {atom, _, erlang}, {atom, _, exit}}, [{tuple, _, _}]}]}]) -> true;
exit_fallback(_) -> false.

compare_frames([], _, _, _) -> [];
compare_frames(Shapes, Release, Generated, Path) ->
    Expected = allocations(Release),
    Current = allocations(Generated),
    [{Path, Key, Count, Frame} || {Key, Count} <- Shapes, maps:get(Key, Expected, 0) =:= 0,
                                   Frame <- [maps:get(Key, Current, 0)], Frame > 0].

allocations(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, lists:max([0 | [Size || {allocate, Size, _} <- Code] ++
        [Size || {allocate_heap, Size, _, _} <- Code]])} || {function, Name, Arity, _, Code} <- Functions]).
