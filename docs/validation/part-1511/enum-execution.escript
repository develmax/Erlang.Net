#!/usr/bin/env escript
-mode(compile).

main([Root, Previous, Current, Ranking]) ->
    {ok, Text} = file:read_file(Ranking),
    [_Heading | Lines] = binary:split(Text, <<10>>, [global]),
    Cases = lists:foldl(fun add_case/2, #{}, Lines),
    {Functions, Calls} = lists:foldl(
        fun({Path, Names}, {TotalFunctions, TotalCalls}) ->
            Release = filename:join(Root, Path),
            Old = image(Previous, Path, Release),
            New = image(Current, Path, Release),
            {ok, {Module, Chunks}} = beam_lib:chunks(Release, [exports, abstract_code]),
            Exports = proplists:get_value(exports, Chunks),
            {raw_abstract_v1, Forms} = proplists:get_value(abstract_code, Chunks),
            TestCases = [{Name, keys(Clauses)} ||
                {function, _, Name, 2, Clauses} <- Forms,
                lists:member({Name, 2}, Names), lists:member({Name, 2}, Exports)],
            Inputs = [{Name, [Key, Tags]} || {Name, Keys} <- TestCases,
                Key <- Keys ++ [unrecognized_atom_tail_probe, 42, {unknown, value}],
                Tags <- [[], [4]]],
            Reference = run(Module, Release, Inputs),
            Prior = run(Module, Old, Inputs),
            Candidate = run(Module, New, Inputs),
            Reference = Candidate,
            [Result || {Result, _} <- Reference] =:= [Result || {Result, _} <- Prior] orelse
                error({different_previous_results, Module}),
            io:format("~s: ~B exported functions, ~B matching calls, ~B prior stack differences~n",
                      [Path, length(TestCases), length(Inputs),
                       length([Pair || {A, B} = Pair <- lists:zip(Reference, Prior), A =/= B])]),
            {TotalFunctions + length(TestCases), TotalCalls + length(Inputs)}
        end, {0, 0}, lists:sort(maps:to_list(Cases))),
    true = Calls > 0,
    io:format("~B calls agree with OTP, including module stack frames, across ~B exported functions.~n",
              [Calls, Functions]).

add_case(<<>>, Acc) -> Acc;
add_case(Line, Acc) ->
    {ok, Tokens, _} = erl_scan:string(string:trim(binary_to_list(Line)) ++ "."),
    {ok, {Path, Name, _, _}} = erl_parse:parse_term(Tokens),
    maps:update_with(Path, fun(Names) -> [Name | Names] end, [Name], Acc).

image(Dir, Path, Release) ->
    Candidate = filename:join(Dir, Path),
    case filelib:is_regular(Candidate) of
        true -> Candidate;
        false -> Release
    end.

keys([{clause, _, _, [], [{'case', _, _, Arms}]}]) ->
    [Key || {clause, _, [{atom, _, Key}], [], _} <- Arms].

run(Module, Path, Inputs) ->
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [observe(Module, Name, Arguments) || {Name, Arguments} <- Inputs]
    after
        code:delete(Module),
        code:purge(Module)
    end.

observe(Module, Name, Arguments) ->
    try apply(Module, Name, Arguments) of
        Value -> {{ok, Value}, []}
    catch
        Class:Reason:Stack ->
            {{Class, Reason}, [Frame || {M, _, _, _} = Frame <- Stack, M =:= Module]}
    end.
