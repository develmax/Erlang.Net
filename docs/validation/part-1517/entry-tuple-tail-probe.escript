#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Current]) ->
    Rows = [[{tag, a, b}], [ignore, {tag, 1, #{}}], [{1, 2, 3}, <<>>],
            [atom], [{}], [{a, b}], [{a, b, c, d}], [], improper, [head | improper]],
    Cases = [{Function, [Value]} || Function <- [left, right, plain], Value <- Rows],
    Expected = observe(Reference, Cases),
    Expected = observe(Previous, Cases),
    Expected = observe(Current, Cases),
    io:format("~B returns, exceptions and fixture stack frames match OTP.~n", [length(Cases)]).

observe(Path, Cases) ->
    Module = beam_entry_tuple_tail_probe,
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
