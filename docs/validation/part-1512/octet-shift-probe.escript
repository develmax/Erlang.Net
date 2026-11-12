#!/usr/bin/env escript
-mode(compile).

main([Release, Previous, Current]) ->
    Module = beam_octet_shift_range_probe,
    Inputs = [0, 1, 127, 128, 255, 256, 32768, 1 bsl 80, 1 bsl 248,
              0.0, 1.5, bad_octet, [], {}, #{}, <<>>],
    Calls = [{F, X} || F <- [mixed, unknown], X <- Inputs] ++
            [{mixed, X} || X <- [-1, -256, -1.5]],
    Expected = run(Module, Release, Calls),
    Expected = run(Module, Previous, Calls),
    Expected = run(Module, Current, Calls),
    io:format("~B return, error, and module-stack observations match OTP.~n", [length(Calls)]).

run(Module, Path, Calls) ->
    {ok, Binary} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Binary),
    try
        [observe(Module, Function, Input) || {Function, Input} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.

observe(Module, Function, Input) ->
    try apply(Module, Function, [Input]) of
        Result -> {ok, Result}
    catch
        Class:Reason:Stack ->
            {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
    end.
