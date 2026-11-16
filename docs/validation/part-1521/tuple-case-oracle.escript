#!/usr/bin/env escript
-mode(compile).
main([Reference, Previous, Current]) ->
    Project = [[{ok,a}], [{error,{already_started,b}}], [{error,{bad,b}}],
               [{ok}], [{ok,a,b}], [none], [{unexpected,a}], [{}], [], bad, [a|b]],
    Rows = [[], [{key,value}], [{key,first},{key,second}], [{other,1}],
            [{key}], [{key,a,b}], [bad,{key,yes}], [bad | improper], bad],
    Calls = [{project, [Input]} || Input <- Project] ++
            [{lookup, [Key, Row]} || Key <- [key, other, 1, 1.0], Row <- Rows],
    Expected = observe(Reference, Calls),
    Expected = observe(Previous, Calls),
    Expected = observe(Current, Calls),
    io:format("~B returns, exceptions and fixture stack frames match OTP.~n", [length(Calls)]).
observe(Path, Calls) ->
    Module = beam_call_tuple_case_projection,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, Function, Arguments) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {Function, Arguments} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
