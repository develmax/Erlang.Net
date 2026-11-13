#!/usr/bin/env escript
-mode(compile).
main([Reference, Candidate]) ->
    Keys = [find_integer, find_list, find_mixed],
    Expected = read(Reference, Keys),
    Expected = read(Candidate, Keys),
    lists:foreach(fun({Name, Instructions}) ->
        [Operand | _] = [Right || {test,is_eq_exact,_,[{x,2},Right]} <- Instructions],
        io:format("~p: ~p~n", [Name, Operand])
    end, Expected).

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
