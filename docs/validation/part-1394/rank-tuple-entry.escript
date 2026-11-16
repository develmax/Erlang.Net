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
    io:format("~B functions start with a tuple test in OTP and allocation in generated code.~n", [length(Rows)]),
    io:format("Groups overlap when a function has clauses of different shapes.~n", []),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:reverse(lists:sort(Ranked))).

read_module(Original, Generated) ->
    case beam_lib:chunks(Original, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            Expected = first_operations(Original),
            Actual = first_operations(Generated),
            [{Key, lists:usort(lists:filtermap(fun(C) -> clause_shape(C, Clauses) end, Clauses))}
                || {function, _, Name, Arity, Clauses = [_, _|_]} <- Forms,
                Key <- [{Name, Arity}],
                maps:get(Key, Expected, missing) =:= {test, is_tuple},
                maps:get(Key, Actual, missing) =:= allocate];
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

clause_shape({clause, _, Parameters, Guards, Body}, Clauses) ->
    Tuple = [{Index, tuple_shape(Pattern), other_shapes(Clauses, Index)}
        || {Pattern, Index} <- lists:zip(Parameters, lists:seq(1,length(Parameters))),
        tuple_shape(Pattern) =/= none],
    case Tuple of
        [] -> false;
        _ -> {true, {[{Shape, Others} || {_, Shape, Others} <- Tuple], Guards =/= [], body_shape(Body)}}
    end.

other_shapes(Clauses, Index) ->
    lists:usort([category(lists:nth(Index, Parameters)) || {clause, _, Parameters, _, _} <- Clauses]).

category(Pattern) ->
    case tuple_shape(Pattern) of none -> element(1,Pattern); Shape -> Shape end.

tuple_shape({match,_,L,R}) ->
    case tuple_shape(L) of none -> tuple_shape(R); Shape -> Shape end;
tuple_shape({tuple,_,[{atom,_,_}|_]}) -> tagged_tuple;
tuple_shape({tuple,_,Fields}) ->
    case lists:all(fun(E) -> element(1,E) =:= var end, Fields) of true -> variable_tuple; false -> patterned_tuple end;
tuple_shape({record,_,_,_}) -> tagged_tuple;
tuple_shape(_) -> none.

body_shape([{call, _, _, Arguments}]) ->
    case lists:usort([element(1, Expr) || Expr <- Arguments, element(1, Expr) =/= var]) of
        [] -> call;
        Kinds -> {call, Kinds}
    end;
body_shape([{'case', _, Selector, Clauses}]) -> {'case', selector_kind(Selector), lists:usort([branch_kind(Body) || {clause, _, _, _, Body} <- Clauses])};
body_shape([Expr]) -> element(1, Expr);
body_shape(_) -> sequence.
selector_kind({call, _, {atom, _, Name}, Arguments}) -> {call, Name, length(Arguments)};
selector_kind({call, _, {remote, _, {atom, _, Module}, {atom, _, Name}}, Arguments}) ->
    {call, Module, Name, length(Arguments)};
selector_kind({op, _, Operator, _, _}) -> {operator, Operator};
selector_kind(Value) -> element(1, Value).
branch_kind([{call, _, _, _}]) -> call;
branch_kind([{tuple, _, _}]) -> tuple;
branch_kind([Expr]) -> element(1, Expr);
branch_kind(_) -> sequence.
