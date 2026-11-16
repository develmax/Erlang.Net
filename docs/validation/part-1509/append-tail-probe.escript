#!/usr/bin/env escript
-mode(compile).

main([Source, Before, After]) ->
    {ok, Module, Reference} = compile:file(Source, [binary, deterministic]),
    {ok, Old} = file:read_file(Before),
    {ok, New} = file:read_file(After),
    Names = [{prefixed, 2}, {suffixed, 2}, {nested, 2}, {literal_join, 2}, {shared, 1}],
    lists:foreach(fun({Name, Arity}) -> compare_frame(Name, Arity, Reference, Old, New) end, Names),
    false = lists:any(fun({call_ext, 2, {extfunc, erlang, '++', 2}}) -> true; (_) -> false end,
                      body(New, literal_join, 2)),
    Values = ["file", [], [1|invalid], fail, name, <<>>, 7],
    Calls = [{Name, [First, Second]} || {Name, 2} <- Names, First <- Values, Second <- Values] ++
        [{shared, [Value]} || Value <- Values],
    {0, 0} = compare(Module, Reference, Old, New, Calls);
main(["xref", Root, Before, After]) ->
    Module = xref_utils,
    Path = "lib/tools-4.2.3/ebin/xref_utils.beam",
    [Reference, Old, New] = [begin
        {ok, Image} = file:read_file(filename:join(Directory, Path)),
        Image
    end || Directory <- [Root, Before, After]],
    compare_frame(module_filename, 2, Reference, Old, New),
    Paths = ["", "lib", <<"lib">>, [a|b], 3],
    Names = [kernel, "file", [], <<>>, invalid, [a|b], 3],
    {0, _} = compare(Module, Reference, Old, New,
                     [{module_filename, [PathPart, Name]} || PathPart <- Paths, Name <- Names]).

compare_frame(Name, Arity, Reference, Old, New) ->
    Expected = frame(Reference, Name, Arity),
    Prior = frame(Old, Name, Arity),
    Current = frame(New, Name, Arity),
    io:format("~p/~B frame: OTP ~B, previous ~B, current ~B~n", [Name, Arity, Expected, Prior, Current]),
    true = abs(Current - Expected) =< abs(Prior - Expected).

frame(Image, Name, Arity) ->
    Body = body(Image, Name, Arity),
    lists:max([0 | [Size || {allocate, Size, _} <- Body] ++
                  [Size || {allocate_heap, Size, _, _} <- Body]]).

body(Image, Name, Arity) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Image),
    [Body] = [Code || {function, Function, A, _, Code} <- Functions, Function =:= Name, A =:= Arity],
    Body.

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
    ValueDifferences = differences(Calls, [value(Result) || Result <- Expected],
                                  [value(Result) || Result <- Current]),
    {length(ValueDifferences), length(NewDifferences)}.

value({Class, Reason, _}) -> {Class, Reason};
value(Result) -> Result.

differences(Calls, Left, Right) ->
    [{Call, Expected, Actual} || {Call, Expected, Actual} <- lists:zip3(Calls, Left, Right),
                                Expected =/= Actual].

observe(Module, Image, Calls) ->
    code:unstick_mod(Module),
    code:purge(Module),
    {module, Module} = code:load_binary(Module, "append-tail-probe.beam", Image),
    [try apply(Module, Name, Arguments) of
         Result -> {ok, Result}
     catch Class:Reason:Stack ->
         {Class, Reason, [{M, F, A} || {M, F, A, _} <- Stack, M =:= Module]}
     end || {Name, Arguments} <- Calls].
