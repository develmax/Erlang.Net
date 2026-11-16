#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Images = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = lists:nthtail(length(Root), filename:split(filename:absname(Path))),
        [{{filename:join(Relative), MFA}, Shapes}
            || {MFA, Shapes} <- read_module(filename:join([Reference|Relative]), Path)]
    end, Images),
    Groups = lists:foldl(fun({Function, Shapes}, Acc) ->
        lists:foldl(fun(Shape, Inner) ->
            maps:update_with(Shape, fun(Entries) -> [Function|Entries] end, [Function], Inner)
        end, Acc, Shapes)
    end, #{}, Rows),
    Ranked = [{length(lists:usort([Path || {Path, _} <- Entries])), length(Entries), Shape,
        lists:sublist(lists:sort(Entries), 3)} || {Shape, Entries} <- maps:to_list(Groups)],
    io:format("~B functions in the direct-call subgroup with list test before allocation in OTP.~n", [length(Rows)]),
    io:format("Groups use the first clause and the last clause of each function.~n", []),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:reverse(lists:sort(Ranked))).

read_module(Original, Generated) ->
    case beam_lib:chunks(Original, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Expected = first_operations(Original),
            Actual = first_operations(Generated),
            [{Key, [function_shape(Clauses)]}
                || {function, _, Name, Arity, Clauses = [_, _|_]} <- Forms,
                Key <- [{Name, Arity}],
                maps:get(Key, Expected, missing) =:= {test, is_nonempty_list},
                maps:get(Key, Actual, missing) =:= allocate,
                lists:any(fun(C) -> clause_shape(C) =:= {true,{[variable_head],false,call}} end, Clauses)];
        _ ->
            []
    end.

first_operations(Path) ->
    case beam_disasm:file(Path) of
        {beam_file, _, _, _, _, Functions} ->
            maps:from_list([{{Name, Arity}, First} || {function, Name, Arity, _, Ops} <- Functions,
                First <- lists:sublist([opcode(I) || I <- Ops,
                    not lists:member(opcode(I), [label, line, func_info])], 1)]);
        _ ->
            #{}
    end.

opcode({test, Name, _, _}) -> {test, Name};
opcode(Instruction) when is_tuple(Instruction) -> element(1, Instruction);
opcode(Instruction) -> Instruction.

clause_shape({clause, _, Parameters, Guards, Body}) ->
    Cons = [cons_shape(Parameter) || Parameter = {cons, _, _, _} <- Parameters],
    case Cons of
        [] -> false;
        _ -> {true, {Cons, Guards =/= [], body_shape(Body)}}
    end.

cons_shape({cons, _, _, {cons, _, _, _}}) -> prefix;
cons_shape({cons, _, {tuple, _, _}, _}) -> tuple_head;
cons_shape({cons, _, {var, _, _}, {nil, _}}) -> singleton;
cons_shape({cons, _, {var, _, _}, _}) -> variable_head;
cons_shape({cons, _, _, _}) -> value_head.

body_shape([{call, _, _, Arguments}]) ->
    case lists:usort([element(1, Expr) || Expr <- Arguments, element(1, Expr) =/= var]) of
        [] -> call;
        Kinds -> {call, Kinds}
    end;
body_shape([Expr]) -> element(1, Expr);
body_shape(Body) -> {sequence, element(1, lists:last(Body))}.


function_shape(Clauses) ->
    {length(Clauses), edge_shape(hd(Clauses)), edge_shape(lists:last(Clauses))}.

edge_shape({clause, _, Parameters, Guards, Body}) ->
    {lists:map(fun pattern_kind/1, Parameters), Guards =/= [], body_shape(Body)}.

pattern_kind(Cons = {cons, _, _, _}) -> cons_shape(Cons);
pattern_kind(Pattern) -> element(1, Pattern).
