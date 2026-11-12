#!/usr/bin/env escript
-mode(compile).

main([Root, Generated, PathFile]) ->
    {ok, Data} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Path) || Path <- binary:split(Data, <<10>>, [global]),
                                                 Path =/= <<>>],
    Findings = lists:flatmap(fun(Path) -> compare(Root, Generated, Path) end, Paths),
    lists:foreach(fun(Kind) ->
        Group = [{Path, Function, Slots} || {Path, Function, Result, Slots, _} <- Findings, Result =:= Kind],
        Modules = lists:usort([Path || {Path, _, _} <- Group]),
        io:format("~p: ~B modules, ~B functions~n", [Kind, length(Modules), length(Group)]),
        lists:foreach(fun(Example) -> io:format("  ~0p~n", [Example]) end, lists:sublist(Group, 12))
    end, [unused_entry_save, arithmetic_result_save]),
    rank_source_shapes(Findings).

compare(Root, Generated, Path) ->
    OriginalPath = filename:join(Root, Path),
    Reference = maps:from_list(functions(OriginalPath)),
    Shapes = source_shapes(OriginalPath),
    lists:flatmap(fun({Function, Current}) ->
        case {maps:get(Function, Reference, []), Current} of
            {[{allocate, Expected, Live} | _], [{allocate, Actual, Live} | Code]} when Actual > Expected ->
                Shape = maps:get(Function, Shapes, unavailable),
                [{Path, Function, Kind, Slots, Shape} || {Kind, Slots} <- first_call_saves(Code)];
            _ -> []
        end
    end, functions(filename:join(Generated, Path))).

source_shapes(Path) ->
    case beam_lib:chunks(Path, [abstract_code]) of
        {ok, {_, [{abstract_code, {raw_abstract_v1, Forms}}]}} ->
            maps:from_list([{{Name, Arity}, shape(Clauses)}
                            || {function, _, Name, Arity, Clauses} <- Forms]);
        _ -> #{}
    end.

shape([{clause, _, Arguments, [], [First | Rest]}]) ->
    Parameters = case lists:all(fun(Argument) -> element(1, Argument) =:= var end, Arguments) of
        true -> variables;
        false -> patterns
    end,
    {Parameters, expression(First), case Rest of [] -> terminal; _ -> sequence end};
shape([{clause, _, _, _, _}]) -> guarded;
shape(_) -> multiple_clauses.

expression({match, _, Left, Right}) -> {match, element(1, Left), expression(Right)};
expression({'case', _, Selector, _}) -> {'case', expression(Selector)};
expression({call, _, {remote, _, {atom, _, _}, {atom, _, _}}, _}) -> remote_call;
expression({call, _, {atom, _, Name}, Arguments}) ->
    case erl_internal:bif(Name, length(Arguments)) of
        true -> {bif, Name, length(Arguments)};
        false -> local_call
    end;
expression(Expression) -> element(1, Expression).

rank_source_shapes(Findings) ->
    Groups = lists:foldl(fun({Path, Function, unused_entry_save, _, Shape}, Acc) ->
        maps:update_with(Shape, fun(Group) -> [{Path, Function} | Group] end, [{Path, Function}], Acc);
        (_, Acc) -> Acc
    end, #{}, Findings),
    Rows = lists:reverse(lists:sort(
        [{length(lists:usort([Path || {Path, _} <- Group])), length(Group), Shape, lists:sublist(Group, 2)}
         || {Shape, Group} <- maps:to_list(Groups)])),
    io:format("Entry saves by source shape:~n"),
    lists:foreach(fun({Modules, Functions, Shape, Examples}) ->
        io:format("  ~B modules, ~B functions: ~0p ~0p~n", [Modules, Functions, Shape, Examples])
    end, lists:sublist(Rows, 16)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    [{{Name, Arity}, Code} || {function, Name, Arity, _, [_, _, _, _ | Code]} <- Functions].

first_call_saves(Code) ->
    {Prefix, Remaining} = lists:splitwith(fun straight_argument_setup/1, Code),
    case Remaining of
        [{Opcode, _, _} | After] when Opcode =:= call; Opcode =:= call_ext ->
            Unused = lists:usort([Slot || {move, {x, _}, {y, Slot}} <- Prefix,
                                        not lists:any(fun(I) -> uses_slot(I, Slot) end, After)]),
            [{unused_entry_save, Unused} || Unused =/= []] ++ result_save(After);
        _ -> []
    end.

straight_argument_setup({move, _, _}) -> true;
straight_argument_setup({line, _}) -> true;
straight_argument_setup({init_yregs, _}) -> true;
straight_argument_setup(_) -> false.

result_save([{move, {x, 0}, {y, Slot}} | After]) ->
    case lists:dropwhile(fun(I) -> element_if_tuple(I) =:= line end, After) of
        [{gc_bif, Op, _, _, Args, _} | Rest] ->
            [{arithmetic_result_save, {Op, Slot}} || lists:any(fun(Arg) -> uses_slot(Arg, Slot) end, Args),
                                                    not lists:any(fun(I) -> uses_slot(I, Slot) end, Rest)];
        _ -> []
    end;
result_save(_) -> [].

element_if_tuple(I) when is_tuple(I) -> element(1, I);
element_if_tuple(I) -> I.

uses_slot({y, Slot}, Slot) -> true;
uses_slot({literal, _}, _) -> false;
uses_slot(Tuple, Slot) when is_tuple(Tuple) -> uses_slot(tuple_to_list(Tuple), Slot);
uses_slot([Head | Tail], Slot) -> uses_slot(Head, Slot) orelse uses_slot(Tail, Slot);
uses_slot(_, _) -> false.
