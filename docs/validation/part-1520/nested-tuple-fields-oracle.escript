#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Current]) ->
    Inputs = [[], other, [bad], [[]], [[bad]], [bad | improper],
              [[{bad, {a,b,c}}]], [[{{unused}, {a,b}}]],
              [[{{unused}, {a,b,c}}]], [[{{unused}, {a,b,c,d}}]],
              [[{{unused}, {fail,b}}]], [[{{unused}, {a,fail,c}}]],
              [[{{unused}, {a,b,c,fail}}]], [[{{unused,extra}, {a,b,c}}]]],
    Cases = [{F, [Suffix, Rows]} || F <- [two, three, four],
              Suffix <- [[], "suffix", atom_tail], Rows <- Inputs],
    Expected = observe(Reference, Cases),
    Expected = observe(Previous, Cases),
    Expected = observe(Current, Cases),
    io:format("~B returns, exceptions and fixture stack frames match OTP.~n", [length(Cases)]).

observe(Path, Cases) ->
    Module = beam_nested_tuple_fields_oracle,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [try apply(Module, F, Arguments) of
             Value -> {ok, Value}
         catch
             Class:Reason:Stack ->
                 {Class, Reason, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
         end || {F, Arguments} <- Cases]
    after
        code:delete(Module),
        code:purge(Module)
    end.
