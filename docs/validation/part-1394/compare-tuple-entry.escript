#!/usr/bin/env escript
-mode(compile).

main([Otp, Previous, Current, PathList]) ->
    {ok, Content} = file:read_file(PathList),
    Paths = [binary_to_list(Line) || Line <- binary:split(Content, <<10>>, [global]), Line =/= <<>>],
    Changes = [compare(Otp, Previous, Current, Path) || Path <- Paths],
    Count = fun(Column) -> lists:sum([length(element(Column, Result)) || Result <- Changes]) end,
    Modules = fun(Column) -> length([ok || Result <- Changes, element(Column, Result) =/= []]) end,
    io:format("Matched OTP tuple-entry test after previously allocating first in ~B functions across ~B modules.~n",
              [Count(2), Modules(2)]),
    io:format("Lost the OTP tuple-entry test in ~B functions across ~B modules.~n", [Count(3), Modules(3)]),
    lists:foreach(fun({Path,Fixed,Lost}) ->
        case {Fixed,Lost} of {[],[]} -> ok; _ -> io:format("~s +~p -~p~n",[Path,Fixed,Lost]) end
    end, Changes).

compare(Otp, Previous, Current, Path) ->
    Reference = read(filename:join(Otp,Path)),
    Before = read_or_reference(filename:join(Previous,Path), Reference),
    After = read_or_reference(filename:join(Current,Path), Reference),
    Fixed = [Key || {Key, {tuple_test, Register}} <- maps:to_list(Reference),
                    maps:get(Key,Before,missing) =:= allocate,
                    maps:get(Key,After,missing) =:= {tuple_test, Register}],
    Lost = [Key || {Key, {tuple_test, Register}} <- maps:to_list(Reference),
                   maps:get(Key,Before,missing) =:= {tuple_test, Register},
                   maps:get(Key,After,missing) =/= {tuple_test, Register}],
    {Path, Fixed, Lost}.

read_or_reference(Path, Reference) ->
    case filelib:is_regular(Path) of true -> read(Path); false -> Reference end.

read(Path) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name,Arity}, entry(Instructions)} ||
                   {function,Name,Arity,_,Instructions} <- Functions]).

entry([{func_info,_,_,_}, {label,_}, {test,is_tuple,_,[{x,Register}]} | _]) ->
    {tuple_test, Register};
entry([{func_info,_,_,_}, {label,_}, Instruction | _]) when is_tuple(Instruction) ->
    case element(1,Instruction) of
        allocate -> allocate;
        allocate_heap -> allocate;
        allocate_zero -> allocate;
        allocate_heap_zero -> allocate;
        _ -> other
    end;
entry([_ | Instructions]) ->
    entry(Instructions);
entry([]) ->
    other.
