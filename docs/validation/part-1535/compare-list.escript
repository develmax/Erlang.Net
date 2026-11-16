#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Inputs = [ok, -12, {nested, [1,2]}, <<"value">>],
    Calls = [{F, [A,B]} || F <- [a,b,c,e], A <- Inputs, B <- Inputs],
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B return values match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_list_live_inputs,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [apply(Module, Function, Args) || {Function, Args} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
