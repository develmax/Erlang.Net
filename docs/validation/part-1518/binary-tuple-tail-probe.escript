#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Current]) ->
    Binaries = [<<9>>, <<32, "rest">>, <<"!">>, <<"!!">>, <<"x">>, <<>>, wrong_type],
    Rows = [[{tag, a, b}], [irrelevant, {tag, 1, #{}}], [{a, b}],
            [atom], [{}], [{a, b, c, d}], [], improper, [head | improper]],
    Cases = [{Binary, Row} || Binary <- Binaries, Row <- Rows],
    Expected = observe(Reference, Cases),
    Expected = observe(Previous, Cases),
    Expected = observe(Current, Cases),
    io:format("~B returns, exceptions and fixture stack frames match OTP.~n", [length(Cases)]).

observe(Path, Cases) ->
    Module = beam_binary_tuple_tail_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try Module:run(Binary, Row) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {Binary, Row} <- Cases]
    after
        code:delete(Module),
        code:purge(Module)
    end.
