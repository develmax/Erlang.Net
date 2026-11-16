#!/usr/bin/env escript
-mode(compile).
main([Root, Before, After, Manifest]) ->
    {ok, Text} = file:read_file(Manifest),
    Paths = [binary_to_list(P) || P <- binary:split(Text, <<10>>, [global]), P =/= <<>>],
    Rows = lists:flatmap(fun(Path) -> compare(Root, Before, After, Path) end, Paths),
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
         ReferenceBody = maps:get(Key, Original, []),
         Selectors = [{Operand, keys(Table)} ||
             {select_val, Operand, _, {list, Table}} <- ReferenceBody],
         Matching = [Operand || {Operand, ReferenceKeys} <- Selectors, ReferenceKeys =:= Keys],
         Typed = {tr, Register, {t_atom, any}},
         Kind = case lists:member(Typed, Matching) of
                    true -> exact_operand;
                    false ->
                        case lists:any(fun({Operand, ReferenceKeys}) ->
                            Operand =:= Typed andalso lists:all(fun(K) -> lists:member(K, Keys) end, ReferenceKeys)
                        end, Selectors) of
                            true -> same_type_reduced_keys;
                            false -> fallback_shape(ReferenceBody, Register, Keys)
                        end
                end,
         {Kind, Path, Key, Register, Matching}
     end ||
        {Key, Body} <- maps:to_list(Current),
        {Old, New} <- lists:zip(maps:get(Key, Previous), Body),
        {select_val, {tr, Register, {t_atom, any}}, Fail, {list, Branches}} <- [New],
        Old =:= {select_val, Register, Fail, {list, Branches}},
        Keys <- [keys(Branches)]].

fallback_shape(Body, Register, Keys) ->
    Tests = [Key || {test, Opcode, _, [Operand, Key]} <- Body,
                    Opcode =:= is_eq_exact orelse Opcode =:= is_ne_exact,
                    (Operand =:= Register orelse Operand =:= {tr,Register,{t_atom,any}}),
                    lists:member(Key, Keys)],
    case Tests of
        [] ->
            case [I || I <- Body, not is_function_metadata(I)] of
                [{move, _, {x,0}}, return] -> reference_constant_return;
                _ -> different_or_unmatched
            end;
        _ -> reference_exact_test
    end.

is_function_metadata({label,_}) -> true;
is_function_metadata({line,_}) -> true;
is_function_metadata({func_info,_,_,_}) -> true;
is_function_metadata(_) -> false.

keys(Table) ->
    lists:sort([Key || Key <- Table, element(1, Key) =/= f]).

image(Dir, Root, Path) ->
    Candidate = filename:join(Dir, Path),
    case filelib:is_regular(Candidate) of
        true -> Candidate;
        false -> filename:join(Root, Path)
    end.

read(Path) ->
    {beam_file,_,_,_,_,Fs} = beam_disasm:file(Path),
    maps:from_list([{{Name,Arity},Code} || {function,Name,Arity,_,Code} <- Fs]).
