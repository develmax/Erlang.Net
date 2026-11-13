#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Rows = [[], [{one,two}], [{one,two,three}], [one], [{2, integer}], invalid],
    Calls = [{run, [Key, Row, Extra]} || Key <- [one, absent, 2, 2.0], Row <- Rows, Extra <- [extra, {saved, 4}]],
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B return values, errors and fixture stack observations match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_case_multi_projected_tail,
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
