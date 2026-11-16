#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:sort([compare(Reference, Root, File) || File <- Files]),
    io:format("Compared ~p differing images.~n", [length(Rows)]),
    lists:foreach(fun(Limit) ->
        Count = length([ok || {Different, _, _, _, _, _} <- Rows, Different =< Limit]),
        io:format("~p images with at most ~p differing functions~n", [Count, Limit])
    end, [0, 1, 2, 5, 10, 25, 50]),
    io:format("~nLowest counts: {differences, image, changed, missing, extra, equivalent}~n"),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sublist(Rows, 40)).

compare(Reference, Root, File) ->
    Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(File)))),
    Expected = functions(filename:join(Reference, Relative)),
    Actual = functions(File),
    Changed = lists:sort([Key || {Key, Ops} <- maps:to_list(Expected),
        {ok, Other} <- [maps:find(Key, Actual)], Ops =/= Other]),
    Missing = lists:sort([Key || Key <- maps:keys(Expected), not maps:is_key(Key, Actual)]),
    Extra = lists:sort([Key || Key <- maps:keys(Actual), not maps:is_key(Key, Expected)]),
    Count = length(Changed) + length(Missing) + length(Extra),
    {Count, Relative, Changed, Missing, Extra, maps:size(Expected) - length(Changed) - length(Missing)}.

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, normalize(Ops)} || {function, Name, Arity, _, Ops} <- Functions]).

normalize(Ops) ->
    Labels = [Label || {label, Label} <- Ops],
    Positions = maps:from_list(lists:zip(Labels, lists:seq(1, length(Labels)))),
    [normalize(Instruction, Positions) || Instruction <- Ops].

normalize({label, Label}, Positions) ->
    {label, maps:get(Label, Positions, Label)};
normalize({f, Label}, Positions) ->
    {f, maps:get(Label, Positions, Label)};
normalize({literal, Value}, _) ->
    {literal, Value};
normalize({make_fun3, Module, Index, _, Destination, Arguments}, Positions) ->
    {make_fun3, Module, Index, uniq, normalize(Destination, Positions), normalize(Arguments, Positions)};
normalize(Tuple, Positions) when is_tuple(Tuple) ->
    list_to_tuple([normalize(Value, Positions) || Value <- tuple_to_list(Tuple)]);
normalize([Head|Tail], Positions) ->
    [normalize(Head, Positions)|normalize(Tail, Positions)];
normalize(Value, _) ->
    Value.
