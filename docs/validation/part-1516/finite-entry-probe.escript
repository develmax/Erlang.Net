#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Calls = [{F, [N]} || F <- [up, down, left, right],
                          N <- [0, 1, 2, 16, -1, 1.5, invalid, []]] ++
            [{from_call, [X, Value]} || X <- [0, 0.0, 1, invalid, []],
                                      Value <- [alpha, 1, {}, #{}, <<>>]],
    Expected = run(Reference, Calls),
    Expected = run(Previous, Calls),
    Expected = run(Current, Calls),
    io:format("~B return, error, and module-stack observations match OTP.~n", [length(Calls)]).

run(Path, Calls) ->
    Module = beam_finite_entry_dispatch_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, F, Args) of
             Result -> {ok, Result}
         catch
             Class:Reason:Stack -> {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {F, Args} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
