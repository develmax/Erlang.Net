#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Groups = lists:foldl(fun(Path, Acc) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Expected = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        maps:fold(fun(Key, Ops, Acc0) ->
            case maps:is_key(Key, Actual) andalso is_comprehension(Key) of
                true -> Acc0;
                false ->
                    case is_comprehension(Key) of
                        true ->
                            Shape = {element(2, Key), classify(Key, Ops)},
                            maps:update_with(Shape, fun(Entries) -> [{Relative, Key}|Entries] end,
                                             [{Relative, Key}], Acc0);
                        false -> Acc0
                    end
            end
        end, Acc, Expected)
    end, #{}, Files),
    Rows = lists:reverse(lists:sort([
        {length(lists:usort([Path || {Path, _} <- Entries])), length(Entries), Shape, lists:sublist(lists:sort(Entries), 5)}
        || {Shape, Entries} <- maps:to_list(Groups)
    ])),
    lists:foreach(fun({Images, Functions, Shape, Examples}) ->
        io:format("~p images, ~p missing helpers ~p ~0p~n", [Images, Functions, Shape, Examples])
    end, Rows).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Ops} || {function, Name, Arity, _, Ops} <- Functions]).

is_comprehension({Name, _}) ->
    string:find(atom_to_list(Name), "-lc$^") =/= nomatch.

classify(Key, Ops) ->
    Tuple = lists:any(fun
        ({test, is_tagged_tuple, _, _}) -> true;
        ({test, test_arity, _, _}) -> true;
        (_) -> false
    end, Ops),
    Mapping = lists:any(fun
        ({call, _, {_, Name, Arity}}) -> {Name, Arity} =/= Key andalso not is_comprehension({Name, Arity});
        ({call_ext, _, {extfunc, _, _, _}}) -> true;
        ({call_fun, _}) -> true;
        (_) -> false
    end, Ops),
    Cons = lists:any(fun
        ({put_list, _, _, _}) -> true;
        (_) -> false
    end, Ops),
    {tuple_check, Tuple, mapper_call, Mapping, constructs_list, Cons}.
