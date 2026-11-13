#!/usr/bin/env escript
-mode(compile).

main([Reference, Baseline, Candidate]) ->
    Calls = [{Binary, Size} || Binary <- [<<>>, <<1,2,3>>, <<5:3>>, invalid],
                              Size <- [-1, 0, 1, 2, 4, 1.5, invalid]],
    Expected = observe(Reference, Calls),
    Expected = observe(Baseline, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B return values, error reasons and process dictionary effects match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_split_singleton,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [begin
             erase(last_size),
             Result = try Module:run(Binary, Size) of
                          Value -> {ok, Value}
                      catch
                          Class:Reason -> {Class, Reason}
                      end,
             {Result, erase(last_size)}
         end || {Binary, Size} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
