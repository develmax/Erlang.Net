#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Inputs = [missing, other, 0, -1, 0.0, [], [1], {value, 1}, #{}, <<1,2>>],
    Defaults = [none, 42, [default], {value, fallback}],
    Calls = [{Input, Default} || Input <- Inputs, Default <- Defaults],
    Values = run(Reference, Calls),
    Values = run(Previous, Calls),
    Values = run(Current, Calls),
    io:format("~B returned terms match OTP across all three images.~n", [length(Calls)]).

run(Path, Calls) ->
    Module = beam_result_union_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [Module:choose(Input, Default) || {Input, Default} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
