#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Inputs = [[], [1], [2,3], [invalid,4], [1|tail], invalid],
    Calls = [{run, [Key, Input]} || Key <- [add, wrong_arity, other, missing], Input <- Inputs],
    Expected = observe(Reference, Calls),
    Actual = observe(Candidate, Calls),
    [io:format("~p~n", [Row]) || Row = {_, E, A} <- lists:zip3(Calls, Expected, Actual), E =/= A],
    Expected = Actual,
    io:format("~B return values, errors and fixture stack observations match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_length_selector_apply,
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
