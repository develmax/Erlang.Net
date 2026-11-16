#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Expected = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        lists:filtermap(fun({Key, Ops}) ->
            case maps:find(Key, Actual) of
                {ok, GeneratedOps} -> entry_dispatch(Relative, Key, Ops, GeneratedOps);
                error -> false
            end
        end, maps:to_list(Expected))
    end, Files),
    print_rows("All entry atom tables", Rows),
    Classes = maps:groups_from_list(fun({_, _, _, _, Class}) -> Class end, Rows),
    lists:foreach(fun({C, ClassRows}) -> print_rows(atom_to_list(C), ClassRows) end, lists:sort(maps:to_list(Classes))).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Ops} || {function, Name, Arity, _, Ops} <- Functions]).

entry_dispatch(Path, Key, Ops, Actual) ->
    case lists:dropwhile(fun header/1, Ops) of
        [{select_val, {x, Register}, _, {list, Entries}}|_] ->
            Existing = sets:from_list([Atom || {select_val, _, _, {list, Values}} <- Actual, {atom, Atom} <- Values]),
            Tests = sets:from_list([Atom || {test, is_eq_exact, _, [_, {atom, Atom}]} <- Actual]),
            Linear = sets:subtract(Tests, Existing),
            Cases = atom_cases(Entries),
            Missing = [{Atom, Label} || {Atom, Label} <- Cases, sets:is_element(Atom, Linear)],
            case length(Missing) of
                Count when Count >= 2 ->
                    {true, {Path, Key, Register, Count, target_shape(Cases, Ops)}};
                _ -> false
            end;
        _ -> false
    end.

header({label, _}) -> true;
header({line, _}) -> true;
header({func_info, _, _, _}) -> true;
header(_) -> false.

atom_cases([{atom, Atom}, {f, Label}|Rest]) -> [{Atom, Label}|atom_cases(Rest)];
atom_cases([_,_|Rest]) -> atom_cases(Rest);
atom_cases([]) -> [].

target_shape(Cases, Ops) ->
    Blocks = blocks(Ops, #{}),
    Targets = [maps:get(Label, Blocks, []) || {_, Label} <- Cases],
    case lists:all(fun tail_block/1, Targets) of
        true -> literal_tail;
        false ->
            case lists:all(fun literal_return/1, Targets) of
                true -> literal_return;
                false -> classify(Targets)
            end
    end.

literal_return([{line, _}|Rest]) -> literal_return(Rest);
literal_return([{move, {Kind, _}, {x, 0}}, return]) ->
    Kind =:= atom orelse Kind =:= integer orelse Kind =:= literal orelse Kind =:= float;
literal_return(_) -> false.

classify(Targets) ->
    Heads = [head_opcode(Body) || Body <- Targets],
    case lists:usort(Heads) of
        [Shape] -> Shape;
        _ -> mixed
    end.

head_opcode([{line, _}|Rest]) -> head_opcode(Rest);
head_opcode([{test, Opcode, _, _}|_]) -> Opcode;
head_opcode([Op|_]) when is_tuple(Op) -> element(1, Op);
head_opcode([Op|_]) -> Op;
head_opcode([]) -> missing.

blocks([{label, Label}|Rest], Blocks) ->
    {Body, Remaining} = lists:splitwith(fun before_next_label/1, Rest),
    blocks(Remaining, Blocks#{Label => Body});
blocks([_|Rest], Blocks) -> blocks(Rest, Blocks);
blocks([], Blocks) -> Blocks.

before_next_label({label, _}) -> false;
before_next_label(_) -> true.

tail_block([{move, {Kind, _}, {x, _}}|Rest]) when Kind =:= atom; Kind =:= integer; Kind =:= literal ->
    tail_block(Rest);
tail_block([{line, _}|Rest]) -> tail_block(Rest);
tail_block([{call_only, _, _}]) -> true;
tail_block([{call_ext_only, _, _}]) -> true;
tail_block(_) -> false.

print_rows(Title, Rows) ->
    Paths = lists:usort([Path || {Path, _, _, _, _} <- Rows]),
    io:format("~s: ~p functions in ~p images.~n", [Title, length(Rows), length(Paths)]),
    ByRegister = maps:groups_from_list(fun({_, _, Register, _, _}) -> Register end, Rows),
    lists:foreach(fun({Register, Entries}) ->
        Images = lists:usort([Path || {Path, _, _, _, _} <- Entries]),
        io:format("x~p: ~p functions in ~p images.~n", [Register, length(Entries), length(Images)])
    end, lists:sort(maps:to_list(ByRegister))),
    Sorted = lists:sort(fun({PathA, KeyA, _, A, _}, {PathB, KeyB, _, B, _}) ->
        {-A, PathA, KeyA} < {-B, PathB, KeyB}
    end, Rows),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sublist(Sorted, 15)).
