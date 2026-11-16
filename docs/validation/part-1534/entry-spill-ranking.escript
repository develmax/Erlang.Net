#!/usr/bin/env escript
-mode(compile).

main([Root, Generated, PathFile]) ->
    {ok, Contents} = file:read_file(PathFile),
    Paths = [binary_to_list(Path) || Path <- binary:split(Contents, <<10>>, [global]),
                                    Path =/= <<>>],
    Findings = lists:flatmap(fun(Path) -> compare(Root, Generated, Path) end, Paths),
    Groups = lists:foldl(fun({Group, Path, Function, Sizes}, Acc) ->
        maps:update_with(Group, fun(Items) -> [{Path, Function, Sizes} | Items] end,
                         [{Path, Function, Sizes}], Acc)
    end, #{}, Findings),
    io:format("~B modules scanned~n", [length(Paths)]),
    lists:foreach(fun({Group, Entries}) ->
        Modules = lists:usort([Path || {Path, _, _} <- Entries]),
        io:format("~B modules, ~B functions: ~p ~0p~n",
                  [length(Modules), length(Entries), Group, lists:sublist(lists:reverse(Entries), 5)])
    end, lists:sort(maps:to_list(Groups))).

compare(Root, Generated, Path) ->
    Reference = functions(filename:join(Root, Path)),
    Candidate = functions(filename:join(Generated, Path)),
    maps:fold(fun(Function, Code, Findings) ->
        case {entry(Code), entry(maps:get(Function, Reference, []))} of
            {{Actual, Live, Body}, {Expected, Live, _}} when Actual > Expected ->
                [{classify(Body), Path, Function, {Expected, Actual}} | Findings];
            _ -> Findings
        end
    end, [], Candidate).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Code} || {function, Name, Arity, _, Code} <- Functions]).

entry([{label, _}, {line, _}, {func_info, _, _, _}, {label, _},
       {allocate, Size, Live} | Body]) -> {Size, Live, Body};
entry(_) -> none.

classify(Body) ->
    {Setup, Remainder} = lists:splitwith(fun setup/1, Body),
    Saved = [{Y, X} || {move, {x, X}, {y, Y}} <- Setup],
    case {Saved, Remainder} of
        {[], _} -> no_entry_saves;
        {_, [Call | Rest]} ->
            case Call of
                {call, _, _} -> call_saves(Saved, Rest);
                {call_ext, _, _} -> call_saves(Saved, Rest);
                _ -> entry_saves_before_other_instruction
            end;
        _ -> entry_saves_before_other_instruction
    end.

setup({move, _, _}) -> true;
setup({swap, _, _}) -> true;
setup({init_yregs, _}) -> true;
setup({line, _}) -> true;
setup(_) -> false.

call_saves(Saved, Rest) ->
    Later = ordsets:from_list(yregs(Rest)),
    case lists:any(fun({Y, _}) -> not ordsets:is_element(Y, Later) end, Saved) of
        true -> entry_save_without_later_reference;
        false -> all_entry_slots_referenced_later
    end.

yregs({y, Y}) -> [Y];
yregs({literal, _}) -> [];
yregs({move, Source, _Destination}) -> yregs(Source);
yregs({init_yregs, _}) -> [];
yregs(Tuple) when is_tuple(Tuple) -> yregs(tuple_to_list(Tuple));
yregs([Head | Tail]) -> yregs(Head) ++ yregs(Tail);
yregs(_) -> [].
