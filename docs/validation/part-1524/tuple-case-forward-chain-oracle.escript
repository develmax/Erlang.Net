#!/usr/bin/env escript
%%! -beam_case_rows first second -beam_case_rows third
-mode(compile).

main([Reference, Candidate]) ->
    Calls = [[Key, Tail] || Key <- [beam_case_rows, beam_case_missing, 1],
                            Tail <- [[], [tail], improper]],
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    true = lists:member({ok, {beam_case_rows, ["first", "second", "third", tail]}}, Expected),
    true = lists:member({ok, {beam_case_missing, []}}, Expected),
    io:format("~B returns, exceptions, and fixture stack observations match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_tuple_case_forward_chain,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, take, Args) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || Args <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.