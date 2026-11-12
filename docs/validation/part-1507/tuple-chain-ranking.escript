#!/usr/bin/env escript
-mode(compile).

main([Root, Generated, PathFile]) ->
    {ok, Data} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Path) || Path <- binary:split(Data, <<10>>, [global]),
                                                 Path =/= <<>>],
    Rows = lists:flatmap(fun(Path) -> candidates(Root, Generated, Path) end, Paths),
    Groups = lists:foldl(fun({Shape, Path, Function, Expected, Current}, Acc) ->
        Example = {Path, Function, Expected, Current},
        maps:update_with(Shape, fun(Group) -> [Example | Group] end, [Example], Acc)
    end, #{}, Rows),
    Ranked = lists:reverse(lists:sort(
        [{length(lists:usort([Path || {Path, _, _, _} <- Group])), length(Group), Shape, Group}
         || {Shape, Group} <- maps:to_list(Groups)])),
    io:format("~B modules, ~B functions with a tuple field consumed by the next matched call~n",
              [length(lists:usort([Path || {_, Path, _, _, _} <- Rows])), length(Rows)]),
    lists:foreach(fun({Modules, Functions, Shape, Group}) ->
        io:format("~B modules, ~B functions: ~p ~0p~n", [Modules, Functions, Shape, lists:sublist(Group, 4)])
    end, Ranked).

candidates(Root, Generated, Path) ->
    Original = filename:join(Root, Path),
    case beam_lib:chunks(Original, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Shapes = [{Key, Shape} || {function, _, Name, Arity, Clauses} <- Forms,
                       Key <- [{Name, Arity}], Shape <- chain_shape(Clauses)],
            compare_frames(Shapes, Original, filename:join(Generated, Path), Path);
        _ -> []
    end.

chain_shape([{clause, _, Args, [], [{match, _, {tuple, _, Fields}, First},
                                    {match, _, {tuple, _, _} = SecondPattern, Next} | Rest]}]) ->
    FieldsUsedLater = variables([Args, SecondPattern | Rest]),
    DirectFields = [Field || {var, _, Field} <- Fields, Field =/= '_',
                             not lists:member(Field, FieldsUsedLater)],
    [ {FirstKind, NextKind} || {FirstKind, _} <- ordinary_calls(First),
       {NextKind, NextArguments} <- ordinary_calls(Next),
       lists:any(fun({var, _, Argument}) -> lists:member(Argument, DirectFields);
                    (_) -> false end, NextArguments),
       lists:all(fun({var, _, _}) -> true; (_) -> false end, Args)];
chain_shape(_) -> [].

ordinary_calls({call, _, {atom, _, Name}, Arguments}) ->
    case erl_internal:bif(Name, length(Arguments)) of
        true -> [];
        false -> [{local, Arguments}]
    end;
ordinary_calls({call, _, {remote, _, {atom, _, _}, {atom, _, _}}, Arguments}) ->
    [{remote, Arguments}];
ordinary_calls(_) -> [].

variables({var, _, Name}) -> [Name];
variables(Term) when is_tuple(Term) -> variables(tuple_to_list(Term));
variables([Head | Tail]) -> variables(Head) ++ variables(Tail);
variables(_) -> [].

compare_frames([], _, _, _) -> [];
compare_frames(Shapes, Original, Generated, Path) ->
    Expected = frames(Original),
    Current = frames(Generated),
    [{Shape, Path, Key, Size, Actual} || {Key, Shape} <- Shapes, Size <- [maps:get(Key, Expected, 0)],
        Actual <- [maps:get(Key, Current, 0)], Actual > Size].

frames(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, lists:max([0 | [Size || {allocate, Size, _} <- Code] ++
        [Size || {allocate_heap, Size, _, _} <- Code]])} || {function, Name, Arity, _, Code} <- Functions]).
