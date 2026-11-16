#!/usr/bin/env escript
-mode(compile).

main([Reference, Candidate]) ->
    Rows = [[], [{key,value}], [{other,value}], [{1,one}], [{1.0,float}],
            [{1.0,float},{1,int}], [{key}], [{key,value,extra}],
            [{key,first},{key,second}], bad],
    Find = [{find, [Key, Row]} || Key <- [key, other, 1, 1.0], Row <- Rows],
    WithDefault = [{with_default, [Key, Row, Default]} ||
                      {find, [Key, Row]} <- Find, Default <- [marker, {fallback, 1}]],
    Calls = Find ++ WithDefault,
    Expected = observe(Reference, Calls),
    Expected = observe(Candidate, Calls),
    io:format("~B return values, errors and fixture stack observations match OTP.~n", [length(Calls)]).

observe(Path, Calls) ->
    Module = beam_tuple_case_construct,
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
