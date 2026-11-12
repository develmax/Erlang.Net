#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    lists:foreach(fun({Name, Arity}) -> compare_frame(Name, Arity, Reference, Old, New) end,
                  [{known, 1}, {unknown, 2}, {direct, 1}, {direct_unknown, 2}]),
    Inputs = [ok, [], 0, 0.0, {1, []}, {2, fail}, {3, accepted}, {a, b, c}, {x, <<>>}],
    Calls = [{Name, [Input]} || Name <- [known, direct], Input <- Inputs] ++
        [{Name, [Input, Result]} || Name <- [unknown, direct_unknown, prebound], Input <- Inputs,
                                  Result <- [accepted, fail, [], 0, <<>>]],
    [] = compare(Module, Reference, Old, New, Calls);
main(["xmerl", Root, Before, After]) ->
    Module = xmerl_xsd_type,
    Path = "lib/xmerl-2.2/ebin/xmerl_xsd_type.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    compare_frame(duration_atoms_time, 1, Reference, Old, New),
    Inputs = ["PT0S", "PT2H", "PT3M", "PT4.5S", "P1DT2H3M4S", "P1Y2M3DT4H5M6.7S",
              "-PT3H", "PT", "P", "P1D", "PT99M", "PT4M3H", "PT-1H", "", "invalid", []],
    [] = compare(Module, Reference, Old, New, [{duration_atoms, [Input]} || Input <- Inputs]).

compare_frame(Name, Arity, Reference, Old, New) ->
    Expected = frame(Reference, Name, Arity),
    Prior = frame(Old, Name, Arity),
    Current = frame(New, Name, Arity),
    io:format("~p/~B frame: OTP ~B, previous ~B, current ~B~n", [Name, Arity, Expected, Prior, Current]),
    true = abs(Current - Expected) =< abs(Prior - Expected).

frame(Image, Name, Arity) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Image),
    [Body] = [Code || {function, Function, A, _, Code} <- Functions, Function =:= Name, A =:= Arity],
    lists:max([0 | [Size || {allocate, Size, _} <- Body] ++
                  [Size || {allocate_heap, Size, _, _} <- Body]]).

compare(Module, Reference, Old, New, Calls) ->
    Expected = observe(Module, Reference, Calls),
    Prior = observe(Module, Old, Calls),
    Current = observe(Module, New, Calls),
    OldDifferences = differences(Calls, Expected, Prior),
    NewDifferences = differences(Calls, Expected, Current),
    Changes = differences(Calls, Prior, Current),
    io:format("~B observations: previous vs OTP ~B, current vs OTP ~B, previous vs current ~B differences~n",
              [length(Calls), length(OldDifferences), length(NewDifferences), length(Changes)]),
    lists:foreach(fun(Difference) -> io:format("~0p~n", [Difference]) end, lists:sublist(NewDifferences, 3)),
    NewDifferences.

differences(Calls, Left, Right) ->
    [{Call, Expected, Actual} || {Call, Expected, Actual} <- lists:zip3(Calls, Left, Right),
                                Expected =/= Actual].

observe(Module, Image, Calls) ->
    code:unstick_mod(Module),
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "tuple-chain-probe.beam", Image),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, Result}
     catch Class:Reason:Stack ->
         {Class, Reason, [{M, F, A} || {M, F, A, _} <- Stack, M =:= Module]}
     end || {Name, Arguments} <- Calls].
