#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Current]) ->
    Values = [0, 1, 9, 10, -1, -9, -10, 1234567890123456789, 1.5, bad, []],
    Rows = [[[]], [[a]], [[a,b]], [[a|b]], [bad], [], [bad|bad],
            [{{ignored,a},{a,a}}], [{{ignored,b},{a,b}}], [{{ignored,b},{a,a,a}}]],
    Cases = [{pad, [N]} || N <- Values] ++
            [{pad_base, [N, Base]} || N <- Values, Base <- [2, 10, 16, 36, 1, 37, bad]] ++
            [{F, [Row]} || F <- [pad_unknown, nested], Row <- Rows],
    Expected = observe(Reference, Cases),
    Expected = observe(Previous, Cases),
    Expected = observe(Current, Cases),
    io:format("~B returns, exceptions and fixture stack frames match OTP.~n", [length(Cases)]).

observe(Path, Cases) ->
    Module = beam_result_liveness_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, Function, Arguments) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {Function, Arguments} <- Cases]
    after
        code:delete(Module),
        code:purge(Module)
    end.
