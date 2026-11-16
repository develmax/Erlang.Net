#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    Operators = [{filename, 1}, {append, 2}, {subtract, 2}, {difference, 2}, {quotient, 2}, {nested, 2}],
    lists:foreach(fun({Name, Arity}) -> compare_frame(Name, Arity, Reference, Old, New) end, Operators),
    Inputs = [[], [1,2], [1|improper], 0, -2, 0.0, 4.25, 1 bsl 80, invalid],
    Calls = [{filename, [Input]} || Input <- Inputs] ++
        [{Name, [Input, Operand]} || {Name, 2} <- Operators, Input <- Inputs, Operand <- Inputs],
    [] = compare(Module, Reference, Old, New, Calls);
main(["xref", Root, Before, After]) ->
    Module = xref_utils,
    Path = "lib/tools-4.2.3/ebin/xref_utils.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    compare_frame(application_filename, 1, Reference, Old, New),
    Inputs = [kernel, '', 'Módulo', "stdlib", [], <<"crypto">>, {invalid}, [a|b], 4],
    _ = compare(Module, Reference, Old, New, [{application_filename, [Input]} || Input <- Inputs]).

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
    [] = Changes,
    NewDifferences.

differences(Calls, Left, Right) ->
    [{Call, Expected, Actual} || {Call, Expected, Actual} <- lists:zip3(Calls, Left, Right),
                                Expected =/= Actual].

observe(Module, Image, Calls) ->
    code:unstick_mod(Module),
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "terminal-operator-probe.beam", Image),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, Result}
     catch Class:Reason:Stack ->
         {Class, Reason, [{M, F, A} || {M, F, A, _} <- Stack, M =:= Module]}
     end || {Name, Arguments} <- Calls].
