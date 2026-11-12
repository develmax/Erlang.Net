#!/usr/bin/env escript
-mode(compile).

main([Reference, Previous, Current]) ->
    Rows = [[], bad, [head|bad], [{a, first, spare}], [{b, spare, second}],
            [{a, ignored}], [{bad, ignored, value}], [{a,0,1,2}],
            [missing], [bad], [{{nested}, key, value}], [{ok,value}], [{}]],
    Lookup = [[], [{key,value}], [{1,int}], [{1.0,float}],
              [{1.0,first},{1,second}], [{1,first},{1.0,second}],
              [{other,value}], [{key}], [{key,unused,extra}], [invalid|tail], invalid],
    Cases = [{F,[R]} || F <- [identity, alternate, field], R <- Rows] ++
            [{lookup,[Key,R]} || Key <- [key, other, 1, 1.0], R <- Lookup],
    Expected = observe(Reference, Cases),
    Baseline = observe(Previous, Cases),
    Expected = observe(Current, Cases),
    ExpectedValues = [without_stack(Value) || Value <- Expected],
    ExpectedValues = [without_stack(Value) || Value <- Baseline],
    StackChanges = [{Call, Old, Ref} || {Call, Old, Ref} <- lists:zip3(Cases, Baseline, Expected),
                                      Old =/= Ref],
    io:format("~B returns, exceptions and fixture stack frames match OTP in the current image.~n", [length(Cases)]),
    io:format("The previous image matched values and reasons and differed in ~B fixture stacks.~n", [length(StackChanges)]),
    [io:format("~0p~n", [Change]) || Change <- StackChanges].

without_stack({Class, Reason, _}) -> {Class, Reason};
without_stack(Outcome) -> Outcome.

observe(Path, Cases) ->
    Module = beam_tuple_case_fallbacks,
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
