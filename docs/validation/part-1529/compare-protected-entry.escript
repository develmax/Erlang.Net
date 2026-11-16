#!/usr/bin/env escript
-mode(compile).
main([Reference, Candidate]) ->
    Keys = [find_integer, find_list, find_mixed],
    Expected = read(Reference, Keys),
    Keys = [Name || {Name, _} <- Expected],
    Expected = read(Candidate, Keys),
    lists:foreach(fun({Name, Instructions}) ->
        [Operand | _] = [Right || {test,is_eq_exact,_,[{x,2},Right]} <- Instructions],
        io:format("~p: ~p~n", [Name, Operand])
    end, Expected),
    Calls = [{Function, [Key, Rows]} ||
        Function <- [integer_key, list_key, mixed_key],
        Key <- [1, 1.0, [], [key], [key | tail], key],
        Rows <- [[], [{1, integer}, {1.0, float}], [{[key], proper}, {[key | tail], improper}], invalid]],
    Observations = observe(Reference, Calls),
    Observations = observe(Candidate, Calls),
    io:format("~B protected calls returned the same values or errors.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_protected_entry_type,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, Function, Args) of
            {'EXIT', {Reason, Stack}} when is_list(Stack) -> {caught, Reason};
            Value -> {ok, Value}
        catch
            Class:Reason -> {Class, Reason}
        end || {Function, Args} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.

read(Path, Keys) ->
    {beam_file,_,_,_,_,Functions} = beam_disasm:file(Path),
    [{Name, normalize(Instructions)} ||
        {function,Name,2,_,Instructions} <- Functions, lists:member(Name, Keys)].

normalize(Instructions) ->
    Labels = [L || {label,L} <- Instructions],
    Indices = maps:from_list(lists:zip(Labels, lists:seq(1,length(Labels)))),
    Convert = fun F({label,L}) -> {label,maps:get(L,Indices)};
                  F({f,L}) -> {f,maps:get(L,Indices,L)};
                  F(T) when is_tuple(T) -> list_to_tuple([F(X) || X <- tuple_to_list(T)]);
                  F(L) when is_list(L) -> [F(X) || X <- L];
                  F(X) -> X
              end,
    [Convert(I) || I <- Instructions].
