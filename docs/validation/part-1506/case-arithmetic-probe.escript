#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    Names = [left, right, followed],
    lists:foreach(fun(Name) ->
        Expected = frame(Reference, Name),
        Prior = frame(Old, Name),
        Current = frame(New, Name),
        io:format("~p/2 frame: OTP ~B, previous ~B, current ~B~n", [Name, Expected, Prior, Current]),
        true = abs(Current - Expected) =< abs(Prior - Expected),
        true = arithmetic_uses_call_register(New, Name)
    end, Names),
    Inputs = [0, 1, -2, 5.5, -0.0, 1 bsl 80, invalid, []],
    Calls = [{Name, [Input, Offset]} || Name <- Names, Input <- Inputs, Offset <- Inputs],
    check(Module, Reference, Old, New, Calls, require_reference);
main(["xmerl", Root, Before, After]) ->
    Module = xmerl_xsd_type,
    Path = "lib/xmerl-2.2/ebin/xmerl_xsd_type.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    io:format("pow/2 frame: OTP ~B, previous ~B, current ~B~n",
        [frame(Reference, pow), frame(Old, pow), frame(New, pow)]),
    Inputs = ["1", "3", "1.3", "1.30", "3E-1", "9.5E-2", "6.5E4", "2.0E308",
              "-3.4E-1", "0.20", "0", "NaN", "INF", "-INF", "invalid"],
    Calls = [{compare_floats, [Left, Right]} || Left <- Inputs, Right <- Inputs],
    check(Module, Reference, Old, New, Calls, compare_reference).

check(Module, Reference, Old, New, Calls, Requirement) ->
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
    case Requirement of
        require_reference -> [] = NewDifferences;
        compare_reference -> ok
    end.

differences(Calls, Left, Right) ->
    [{Call, Expected, Actual} || {Call, Expected, Actual} <- lists:zip3(Calls, Left, Right),
                                Expected =/= Actual].

frame(Image, Name) ->
    Body = body(Image, Name),
    lists:max([0 | [Size || {allocate, Size, _} <- Body] ++
                  [Size || {allocate_heap, Size, _, _} <- Body]]).

arithmetic_uses_call_register(Image, Name) ->
    [{gc_bif, '-', _, 1, Arguments, {x,0}}] =
        [I || {gc_bif, '-', _, _, _, _} = I <- body(Image, Name)],
    lists:member({x,0}, Arguments).

body(Image, Name) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Image),
    [Code] = [Code || {function, Function, 2, _, Code} <- Functions, Function =:= Name],
    Code.

observe(Module, Image, Calls) ->
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "case-arithmetic-probe.beam", Image),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, Result}
     catch Class:Reason:Stack ->
         {Class, Reason, [{M, F, A} || {M, F, A, _} <- Stack, M =:= Module]}
     end || {Name, Arguments} <- Calls].
