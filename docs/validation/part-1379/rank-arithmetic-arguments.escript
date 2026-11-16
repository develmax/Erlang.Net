#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = lists:nthtail(length(Root), filename:split(filename:absname(Path))),
        Original = filename:join([Reference|Relative]),
        Expected = read_functions(Original),
        Actual = read_functions(Path),
        case beam_lib:chunks(Original, [abstract_code]) of
            {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
                [{Shape, filename:join(Relative), MFA}
                    || {function, _, Name, Arity, Clauses} <- Forms,
                       MFA <- [{Name, Arity}],
                       maps:get(MFA, Expected, missing) =/= maps:get(MFA, Actual, missing),
                       Shape <- lists:usort(clauses(Clauses, entry))];
            _ -> []
        end
    end, Files),
    Groups = lists:foldl(fun({Shape, Path, MFA}, Acc) ->
        maps:update_with(Shape, fun(Old) -> [{Path,MFA}|Old] end, [{Path,MFA}], Acc)
    end, #{}, Rows),
    Ranked = [{length(lists:usort([Path || {Path,_} <- Examples])),
        length(Examples), Shape, lists:sublist(lists:sort(Examples), 2)}
        || {Shape,Examples} <- maps:to_list(Groups)],
    io:format("Module and function counts by simple arithmetic argument in differing functions.~n", []),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:reverse(lists:sort(Ranked))).

clauses(Clauses, Context) ->
    lists:flatmap(fun({clause, _, _, _, Body}) -> expression(lists:last(Body), Context) end, Clauses).

expression({'case', _, _, Clauses}, _) -> clauses(Clauses, 'case');
expression({'if', _, Clauses}, _) -> clauses(Clauses, 'if');
expression(Expr, Context) ->
    case call_and_prefix(Expr) of
        {Call, Prefix} -> [{Context, Prefix} || simple_arithmetic_call(Call)];
        none -> []
    end.

call_and_prefix({call, _, _, _} = Call) -> {Call, tail};
call_and_prefix({cons, _, Head, Tail}) ->
    case {simple(Head), call_and_prefix(Tail)} of
        {true, {Call, _}} -> {Call, cons};
        _ -> none
    end;
call_and_prefix(_) -> none.

simple_arithmetic_call({call, _, Target, Arguments}) ->
    Direct = case Target of {atom, _, _} -> true;
        {remote, _, {atom, _, _}, {atom, _, _}} -> true; _ -> false end,
    {Other, Math} = lists:partition(fun simple/1, Arguments),
    Direct andalso length(Other) + 1 =:= length(Arguments) andalso
        case Math of
            [{op, _, Operator, Left, Right}] ->
                lists:member(Operator, ['+','-','*','/','div','rem']) andalso simple(Left) andalso simple(Right);
            _ -> false
        end.

simple(Expr) ->
    lists:member(element(1, Expr), [var, atom, integer, char, float, nil]).
read_functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, normalize(Instructions)}
        || {function, Name, Arity, _, Instructions} <- Functions]).

normalize(Instructions) ->
    Labels = [Label || {label, Label} <- Instructions],
    LabelMap = maps:from_list(lists:zip(Labels, lists:seq(1, length(Labels)))),
    Fix = fun Rewrite({label, Label}) -> {label, maps:get(Label, LabelMap, Label)};
        Rewrite({f, Label}) -> {f, maps:get(Label, LabelMap, Label)};
        Rewrite({literal, _} = Literal) -> Literal;
        Rewrite({make_fun3, Module, Index, _, Destination, Arguments}) ->
            {make_fun3, Module, Index, uniq, Rewrite(Destination), Rewrite(Arguments)};
        Rewrite(Tuple) when is_tuple(Tuple) -> list_to_tuple([Rewrite(Value) || Value <- tuple_to_list(Tuple)]);
        Rewrite([Head|Tail]) -> [Rewrite(Head)|Rewrite(Tail)];
        Rewrite(Value) -> Value
    end,
    [Fix(Instruction) || Instruction <- Instructions].

