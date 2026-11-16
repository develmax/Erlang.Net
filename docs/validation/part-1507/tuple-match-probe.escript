#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    lists:foreach(fun(Name) ->
        Expected = frame(Reference, Name, 2),
        Prior = frame(Old, Name, 2),
        Current = frame(New, Name, 2),
        io:format("~p/2 frame: OTP ~B, previous ~B, current ~B~n", [Name, Expected, Prior, Current]),
        true = abs(Current - Expected) =< abs(Prior - Expected)
    end, [known, unknown, literal]),
    Inputs = [ok, invalid, [], 0, 0.0, {ok, 1}, {error, 1}, {0, 1}, {0.0, 1}, {ok, 1, 2},
              {ok, 1, 2, 3}, {error, 1, 2, 3}, {ok, 1, 2, 3, 4}],
    Calls = [{Name, [Input, Expected]} || Name <- [known, unknown, literal], Input <- Inputs,
                                         Expected <- [ok, error, 0, 0.0, <<>>]],
    [] = compare(Module, Reference, Old, New, Calls);
main(["xmerl", Root, Before, After]) ->
    Module = xmerl_xsd_type,
    Path = "lib/xmerl-2.2/ebin/xmerl_xsd_type.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    io:format("duration_atoms_time/1 frame: OTP ~B, previous ~B, current ~B~n",
        [frame(Reference, duration_atoms_time, 1), frame(Old, duration_atoms_time, 1),
         frame(New, duration_atoms_time, 1)]),
    Inputs = ["PT0S", "PT2H", "PT3M", "PT4.5S", "P1DT2H3M4S", "P1Y2M3DT4H5M6.7S",
              "-PT3H", "PT", "P", "P1D", "PT99M", "PT4M3H", "PT-1H", "", "invalid", []],
    _ = compare(Module, Reference, Old, New, [{duration_atoms, [Input]} || Input <- Inputs]).

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
    [] = Changes,
    NewDifferences.

differences(Calls, Left, Right) ->
    [{Call, Expected, Actual} || {Call, Expected, Actual} <- lists:zip3(Calls, Left, Right),
                                Expected =/= Actual].

observe(Module, Image, Calls) ->
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "tuple-match-probe.beam", Image),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, Result}
     catch Class:Reason:Stack ->
         {Class, Reason, [{M, F, A} || {M, F, A, _} <- Stack, M =:= Module]}
     end || {Name, Arguments} <- Calls].
