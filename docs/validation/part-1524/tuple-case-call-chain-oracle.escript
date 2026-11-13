#!/usr/bin/env escript
-mode(compile).
main([Reference, Candidate]) ->
    Rows = [[], [{key,value}], [{other,value}], [{1,one}], [{1.0,float}],
            [{1.0,float},{1,int}], [{key}], [{key,value,extra}], [{key,first},{key,second}], bad],
    Calls = [[Default, Row, Key] || Key <- [key, other, 1, 1.0], Row <- Rows, Default <- [missing, []]],
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B returns, exceptions, and fixture stack observations match OTP.~n", [length(Calls)]).
observe(Path, Calls) ->
    Module = beam_tuple_case_call_chain,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, take, Args) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || Args <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
