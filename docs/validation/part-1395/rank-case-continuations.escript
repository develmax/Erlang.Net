#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Images = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Groups = lists:foldl(fun(Path, Acc) ->
        Relative = lists:nthtail(length(Root), filename:split(filename:absname(Path))),
        Original = filename:join([Reference|Relative]),
        [{filename:join(Relative), MFA, Prefix, Sizes}
            || {MFA, Prefix, Sizes} <- read_module(Original, Path)] ++ Acc
    end, [], Images),
    io:format("~B modules and ~B variable-head sequences beginning with a bound local call and with allocation operand differences.~n",
        [length(lists:usort([Path || {Path, _, _, _} <- Groups])), length(Groups)]),
    io:format("Each row gives module count, function count, continuation shape, then two examples with OTP and generated allocations.~n", []),
    ByPrefix = lists:foldl(fun({Path, MFA, Prefix, Sizes}, Acc) ->
        maps:update_with(Prefix, fun(Rows) -> [{Path, MFA, Sizes}|Rows] end, [{Path, MFA, Sizes}], Acc)
    end, #{}, Groups),
    Rows = [{length(lists:usort([Path || {Path, _, _} <- Entries])), length(Entries), Prefix,
        lists:sublist(lists:sort(Entries), 2)} || {Prefix, Entries} <- maps:to_list(ByPrefix)],
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:reverse(lists:sort(Rows))).

read_module(Original, Generated) ->
    case {beam_lib:chunks(Original, [abstract_code]), instructions(Original), instructions(Generated)} of
        {{ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}}, Expected, Actual} ->
            [{{Name, Arity}, continuation_shape(Body), Sizes}
                || {function, _, Name, Arity, [{clause, _, Parameters, [], Body = [First, _|_]}]} <- Forms,
                expression(First) =:= {match,var,direct_call},
                lists:all(fun(Pattern) -> element(1, Pattern) =:= var end, Parameters),
                Key <- [{Name, Arity}],
                {ok, Sizes} <- [allocation_gap(maps:get(Key, Expected, []), maps:get(Key, Actual, []))]];
        _ ->
            []
    end.

instructions(Path) ->
    case beam_disasm:file(Path) of
        {beam_file, _, _, _, _, Functions} ->
            maps:from_list([{{Name, Arity}, [I || I <- Ops, not lists:member(opcode(I), [label, line, func_info])]}
                || {function, Name, Arity, _, Ops} <- Functions]);
        _ ->
            #{}
    end.

allocation_gap([Expected, Next|_], [Actual, Other|_])
        when element(1, Expected) =:= allocate, element(1, Actual) =:= allocate,
             element(1, Next) =:= move, element(1, Other) =:= init_yregs, Expected =/= Actual ->
    {ok, {Expected, Actual}};
allocation_gap([Expected|MoreExpected], [Actual|MoreActual]) ->
    case opcode(Expected) =:= opcode(Actual) of
        true -> allocation_gap(MoreExpected, MoreActual);
        false -> none
    end;
allocation_gap(_, _) ->
    none.

opcode({test, Name, _, _}) -> {test, Name};
opcode(Instruction) when is_tuple(Instruction) -> element(1, Instruction);
opcode(Instruction) -> Instruction.

expression({match, _, Pattern, Value}) ->
    {match, element(1, Pattern), expression(Value)};
expression({call, _, {atom, _, _}, _}) ->
    direct_call;
expression({call, _, {remote, _, {atom, _, _}, {atom, _, _}}, _}) ->
    external_call;
expression({call, _, _, _}) ->
    dynamic_call;
expression({op, _, Operator, _, _}) ->
    {binary_operator, Operator};
expression(Value) ->
    element(1, Value).
continuation_shape([_|Rest]) ->
    Terminal = lists:last(Rest),
    Middle = lists:droplast(Rest),
    {terminal(Terminal), lists:usort([step(E) || E <- Middle])}.

terminal({'case',_,Selector,_}) -> {'case',expression(Selector)};
terminal(Expr) -> expression(Expr).

step({match,_,{var,_,_},Value}) -> step(Value);
step({call,_,_,_}) -> call;
step(Expr) when element(1,Expr) =:= tuple; element(1,Expr) =:= record;
                element(1,Expr) =:= cons; element(1,Expr) =:= map -> constructor;
step(Expr) -> element(1,Expr).
