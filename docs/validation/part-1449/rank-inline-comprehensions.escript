#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Root = filename:split(filename:absname(Generated)),
    Files = filelib:fold_files(Generated, "[.]beam$", true, fun(Path, Acc) -> [Path|Acc] end, []),
    Rows = lists:flatmap(fun(Path) ->
        Relative = filename:join(lists:nthtail(length(Root), filename:split(filename:absname(Path)))),
        Expected = functions(filename:join(Reference, Relative)),
        Actual = functions(Path),
        [{Relative, Key, Before, After}
         || {Key, OtpCode} <- maps:to_list(Expected),
            CSharpCode <- [maps:get(Key, Actual, [])],
            invokes_list_comprehension(OtpCode),
            not invokes_list_comprehension(CSharpCode),
            lists:any(fun is_nonempty_list_test/1, CSharpCode),
            Before <- [frame_size(OtpCode)],
            After <- [frame_size(CSharpCode)],
            After > Before]
    end, Files),
    io:format("~p functions in ~p images have larger frames and an inline list test where OTP invokes a list comprehension helper.~n",
        [length(Rows), length(lists:usort([Path || {Path, _, _, _} <- Rows]))]),
    lists:foreach(fun(Row) -> io:format("~0p~n", [Row]) end, lists:sort(Rows)).

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Ops} || {function, Name, Arity, _, Ops} <- Functions]).

invokes_list_comprehension(Instructions) ->
    lists:any(fun is_comprehension_call/1, Instructions).

is_comprehension_call({call_last, _, {_, Name, _}, _}) ->
    list_comprehension_name(Name);
is_comprehension_call({Op, _, {_, Name, _}}) when Op =:= call; Op =:= call_only ->
    list_comprehension_name(Name);
is_comprehension_call(_) ->
    false.

list_comprehension_name(Name) ->
    string:find(atom_to_list(Name), "-lc$^") =/= nomatch.

is_nonempty_list_test({test, is_nonempty_list, _, _}) -> true;
is_nonempty_list_test(_) -> false.

frame_size(Instructions) ->
    lists:max([0|[element(2, Op) || Op <- Instructions, is_tuple(Op),
        lists:member(element(1, Op), [allocate, allocate_heap, allocate_zero, allocate_heap_zero])]]).
