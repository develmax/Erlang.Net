#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Inputs = [{0, 0, 0}, {1, 123456, 789012}, {1735, 689600, 21}, {1784, 999999, 999999}],
    Calls = [{F, [Value]} || F <- [format_timestamp, format_short_timestamp, format_long_timestamp],
                             Value <- Inputs] ++
            [{format_timestamp, [Value, fun calendar:now_to_universal_time/1]} || Value <- Inputs],
    Expected = run(Reference, Calls),
    Expected = run(Previous, Calls),
    Expected = run(Current, Calls),
    io:format("~B release megaco timestamp returns match OTP.~n", [length(Calls)]).

run(Path, Calls) ->
    {ok, Image} = file:read_file(Path),
    {module, megaco} = code:load_binary(megaco, Path, Image),
    try
        [apply(megaco, F, Args) || {F, Args} <- Calls]
    after
        code:delete(megaco),
        code:purge(megaco)
    end.
