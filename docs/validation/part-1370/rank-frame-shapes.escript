#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    {Groups, Errors} = lists:foldl(fun(Path, {Groups0, Errors0}) ->
        Relative = lists:nthtail(length(Root), filename:split(filename:absname(Path))),
        Original = filename:join([Reference|Relative]),
        case {beam_disasm:file(Original), beam_disasm:file(Path)} of
            {{beam_file, Module, _, _, _, Expected}, {beam_file, Module, _, _, _, Actual}} ->
                Shapes = source_shapes(Original),
                ActualOps = maps:from_list([{{Name, Arity}, operations(Ops)}
                    || {function, Name, Arity, _, Ops} <- Actual]),
                Next = lists:foldl(fun({function, Name, Arity, _, Ops}, Acc) ->
                    Key = {Name, Arity},
                    case difference(operations(Ops), maps:get(Key, ActualOps, []), none) of
                        none -> Acc;
                        Difference ->
                            Shape = maps:get(Key, Shapes, generated),
                            Group = {Difference, Shape},
                            maps:update_with(Group, fun(Entries) -> [{Module, Key}|Entries] end,
                                             [{Module, Key}], Acc)
                    end
                end, Groups0, Expected),
                {Next, Errors0};
            _ ->
                {Groups0, [filename:join(Relative)|Errors0]}
        end
    end, {#{}, []}, Files),
    io:format("~p differing images. Unreadable pairs ~p.~n", [length(Files), Errors]),
    lists:foreach(fun({Modules, Functions, Group, Examples}) ->
        io:format("~p modules, ~p functions ~w ~p~n", [Modules, Functions, Group, Examples])
    end, lists:reverse(lists:sort([
        {length(lists:usort([M || {M, _} <- Entries])), length(Entries), Group,
            lists:sublist(lists:usort(Entries), 3)}
        || {Group, Entries} <- maps:to_list(Groups)]))).

source_shapes(Path) ->
    case beam_lib:chunks(Path, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            maps:from_list([{{Name, Arity}, shape(Clauses)}
                || {function, _, Name, Arity, Clauses} <- Forms]);
        _ -> #{}
    end.

shape([{clause, _, Parameters, [], Body}]) ->
    Head = case lists:all(fun(Pattern) -> element(1, Pattern) =:= var end, Parameters) of
        true -> variable_head;
        false -> pattern_head
    end,
    {Head, case Body of [Expression] -> element(1, Expression); _ -> sequence end};
shape([_]) -> guarded;
shape(_) -> multiple_clauses.

operations(Ops) ->
    [I || I <- Ops, not lists:member(opcode(I), [label, line, func_info])].

opcode({test, Name, _, _}) -> {test, Name};
opcode(I) when is_atom(I) -> I;
opcode(I) -> element(1, I).

difference([Expected|MoreExpected], [Actual|MoreActual], Previous) ->
    case {opcode(Expected), opcode(Actual), Previous} of
        {Op, Op, _} -> difference(MoreExpected, MoreActual, {Expected, Actual});
        {move, init_yregs, {{allocate, Size, Live}, {allocate, OtherSize, OtherLive}}}
            when Size =/= OtherSize; Live =/= OtherLive -> allocation_operands;
        {{test, Name}, allocate, _} -> {test_before_allocate, Name};
        _ -> none
    end;
difference(_, _, _) -> none.
