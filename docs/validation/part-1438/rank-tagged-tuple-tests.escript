#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Parts = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Parts), filename:split(filename:absname(Path)))),
        {beam_file, _, _, _, _, Ref} = beam_disasm:file(filename:join(Reference, Relative)),
        {beam_file, _, _, _, _, Actual} = beam_disasm:file(Path),
        Shapes = maps:from_list([{{Name, Arity}, tagged_tests(Body)}
                                || {function, Name, Arity, _, Body} <- Ref]),
        lists:flatmap(fun({function, Name, Arity, _, Body}) ->
            Key = {Name, Arity},
            Expected = maps:get(Key, Shapes, []),
            Fused = tagged_tests(Body),
            Pairs = [Pair || Pair <- split_tests(Body),
                             count(Pair, Expected) > count(Pair, Fused)],
            case Pairs of
                [] -> [];
                _ -> [{Relative, Key, Pairs}]
            end
        end, Actual)
    end, Files),
    Modules = lists:usort([Path || {Path, _, _} <- Rows]),
    Wx = lists:usort([Path || {Path, _, Pairs} <- Rows, lists:member({4, wx_env}, Pairs)]),
    io:format("~p functions in ~p images have split tuple tests with a matching fused OTP test deficit.~n",
              [length(Rows), length(Modules)]),
    io:format("~p of those images have the wx_env tuple shape.~n", [length(Wx)]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

count(Key, Pairs) -> length([ok || Pair <- Pairs, Pair =:= Key]).

tagged_tests(Body) ->
    [{Arity, Tag} || {test, is_tagged_tuple, _, [_, Arity, {atom, Tag}]} <- Body].

split_tests([{test, is_tuple, Fail, [Value]},
             {test, test_arity, Fail, [Value, Arity]},
             {get_tuple_element, Value, 0, TagValue},
             {test, is_eq_exact, Fail, [TagValue, {atom, Tag}]} | Rest]) ->
    [{Arity, Tag} | split_tests(Rest)];
split_tests([_|Rest]) ->
    split_tests(Rest);
split_tests([]) ->
    [].
