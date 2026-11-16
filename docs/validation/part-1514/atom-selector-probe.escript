#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Calls = [{converted, Value} || Value <- ["alpha", "beta", "other", "", [16#03BB],
                                             alpha, 1, #{}, <<1,2>>, [256|1]]] ++
            [{F, Value} || F <- [guarded, unknown],
             Value <- [alpha, beta, other, 0, 0.0, [], [x], {}, #{}, <<1>>]],
    Expected = run(Reference, Calls),
    Expected = run(Previous, Calls),
    Expected = run(Current, Calls),
    io:format("~B return, error, and module-stack observations match OTP.~n", [length(Calls)]).

run(Path, Calls) ->
    Module = beam_atom_selector_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, F, [Value]) of
             Result -> {ok, Result}
         catch
             Class:Reason:Stack -> {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {F, Value} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
