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
    print_rows("All dispatch targets tail-call with optional literal moves", [Row || Row={_,_,_,_,true} <- Rows]).

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
                    {true, {Path, Key, Register, Count, tail_targets(Cases, Ops)}};
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

tail_targets(Cases, Ops) ->
    Blocks = blocks(Ops, #{}),
    lists:all(fun({_, Label}) -> tail_block(maps:get(Label, Blocks, [])) end, Cases).

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
