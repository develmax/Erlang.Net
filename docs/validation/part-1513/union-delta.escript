#!/usr/bin/env escript
-mode(compile).
main([Root, Before, After, Manifest]) ->
    {ok, Text} = file:read_file(Manifest),
    Paths = [binary_to_list(P) || P <- binary:split(Text, <<10>>, [global]), P =/= <<>>],
    Rows = [Row || Path <- Paths, Row <- compare(Root, Before, After, Path)],
    [io:format("~0p~n", [Row]) || Row <- Rows],
    Groups = lists:foldl(fun({Kind, _, _, _, _}, Counts) ->
        maps:update_with(Kind, fun(N) -> N+1 end, 1, Counts)
    end, #{}, Rows),
    io:format("~0p~n", [Groups]).

compare(Root, Before, After, Path) ->
    Original = read(filename:join(Root, Path)),
    Previous = read(image(Before, Root, Path)),
    Current = read(image(After, Root, Path)),
    [begin
         Reference = maps:get(Key, Original, []),
         {Kind, Seen} = status(Reference, Register, Type, Compared),
         {Kind, Path, Key, Compared, Seen}
     end ||
        {Key, Body} <- maps:to_list(Current),
        {Old, New} <- lists:zip(maps:get(Key, Previous), Body),
        {test, is_eq_exact, F, [{tr,Register,Type},Compared]} <- [New],
        Old =:= {test, is_eq_exact, F, [Register,Compared]}].

status(Body, Register, Type, Compared) ->
    Seen = [Operand || {test,is_eq_exact,_,[Operand,Key]} <- Body, Key =:= Compared],
    case lists:member({tr,Register,Type}, Seen) of
        true -> {exact_operand, Seen};
        false ->
            case lists:any(fun({tr,_,T}) -> T =:= Type; (_) -> false end, Seen) of
                true -> {same_type_other_register, Seen};
                false ->
                    case lists:member({tr,Register,Type}, [Operand || {test,is_ne_exact,_,[Operand,Key]} <- Body, Key =:= Compared]) of
                        true -> {inverse_test_operand, Seen};
                        false -> {different_or_unmatched, Seen}
                    end
            end
    end.

image(Dir, Root, Path) ->
    Candidate = filename:join(Dir, Path),
    case filelib:is_regular(Candidate) of
        true -> Candidate;
        false -> filename:join(Root, Path)
    end.

read(Path) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(Path),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Fs]).

