#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Calls = [{F, []} || F <- [pick_first, pick_second, choose_third, same_first, same_second,
                             pair_first, pair_second, wide_alpha, wide_beta, wide_gamma,
                             wide_delta, wide_epsilon, wide_zeta]] ++
            [{F, [V]} || F <- [route_first, route_second, tag_unused, open],
             V <- [alpha, beta, unused, missing, 0, 0.0, [], [1], {}, #{}, <<1>>]],
    Expected = run(Reference, Calls),
    Expected = run(Previous, Calls),
    Expected = run(Current, Calls),
    io:format("~B return, error, and module-stack observations match OTP.~n", [length(Calls)]).

run(Path, Calls) ->
    Module = beam_finite_atom_probe,
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
