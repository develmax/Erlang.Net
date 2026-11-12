#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:map(fun(Path) ->
        Relative = lists:nthtail(length(Root), filename:split(filename:absname(Path))),
        Original = filename:join([Reference|Relative]),
        Expected = read_functions(Original),
        Actual = read_functions(Path),
        Keys = lists:usort(maps:keys(Expected) ++ maps:keys(Actual)),
        Differences = [Key || Key <- Keys,
            maps:get(Key, Expected, missing) =/= maps:get(Key, Actual, missing)],
        {length(Differences), filename:join(Relative), Differences, Original, Path}
    end, Files),
    io:format("~p differing images. Modules by count of functions with differing normalized instructions:~n",
        [length(Rows)]),
    Histogram = lists:foldl(fun({Count, _, _, _, _}, Acc) ->
        Bucket = case Count of N when N < 3 -> N; N when N < 6 -> '3-5'; _ -> '6+' end,
        maps:update_with(Bucket, fun(Total) -> Total + 1 end, 1, Acc)
    end, #{}, Rows),
    io:format("~p~n", [Histogram]),
    lists:foreach(fun({Count, Relative, Differences, Original, Path}) ->
        {ok, _, Left} = beam_lib:all_chunks(Original),
        {ok, _, Right} = beam_lib:all_chunks(Path),
        Expected = maps:from_list(Left),
        Actual = maps:from_list(Right),
        Keys = lists:usort(maps:keys(Expected) ++ maps:keys(Actual)),
        Chunks = [Key || Key <- Keys,
            maps:get(Key, Expected, missing) =/= maps:get(Key, Actual, missing)],
        io:format("~w ~s ~p ~p~n", [Count, Relative, Differences, Chunks])
    end, lists:sublist(lists:sort(Rows), 30)).

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

