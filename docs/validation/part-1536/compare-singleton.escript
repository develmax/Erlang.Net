#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Inputs = [[], [ok], [{nested, 42}], [<<1,2>>], [one,two], [one|tail], invalid],
    Calls = [{F, [Input]} || F <- [init,direct], Input <- Inputs],
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B return values, errors and fixture stack observations match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_singleton_call_construct,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, Function, Args) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {Function, Args} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
