#!/usr/bin/env escript
-mode(compile).
main([Root,Generated,PathFile]) ->
    {ok, Content} = file:read_file(PathFile),
    Paths = [unicode:characters_to_list(Line) || Line <- binary:split(Content, <<10>>, [global]), Line =/= <<>>],
Norm=fun(Is) ->
    Labels=maps:from_list(lists:zip([L || {label,L}<-Is],lists:seq(1,length([L || {label,L}<-Is])))),
    Fix=fun F({label,L})->{label,maps:get(L,Labels,L)};
        F({f,L})->{f,maps:get(L,Labels,L)};
        F({literal,V})->{literal,V};
        F({make_fun3,M,I,_,Dst,Args})->{make_fun3,M,I,uniq,F(Dst),F(Args)};
        F(T) when is_tuple(T)->list_to_tuple([F(X)||X<-tuple_to_list(T)]);
        F([H|T])->[F(H)|F(T)];
        F(V)->V
    end,
    [Fix(I)||I<-Is]
end,
Read=fun(P)->
    {beam_file,_,_,_,_,Fs}=beam_disasm:file(P),
    maps:from_list([{{N,A},Norm(Is)}||{function,N,A,_,Is}<-Fs])
end,
    Rows = [summarize(Path, Read(filename:join(Root,Path)), Read(filename:join(Generated,Path))) || Path <- Paths],
    Pairs = lists:usort(lists:append([Signatures || {_, Signatures, _} <- Rows])),
    Counts = [score(Pair, Rows) || Pair <- Pairs],
    io:format("~B images. Counts describe first instruction differences after normalizing local labels.~n", [length(Rows)]),
    io:format("Columns are touched images, images with only this first-difference category, functions, and opcode pair.~n"),
    [io:format("~0p~n", [Row]) || Row <- lists:sublist(lists:reverse(lists:sort(Counts)), 40)],
    Frames = lists:usort(lists:append([Details || {_, _, Details} <- Rows])),
    io:format("Allocate differences by stack size and live input count relative to OTP.~n"),
    [begin
         Occurrences = [Path || {Path, _, Details} <- Rows, Value <- Details, Value =:= Frame],
         io:format("~0p: ~B functions in ~B images~n",
                   [Frame, length(Occurrences), length(lists:usort(Occurrences))])
     end || Frame <- Frames],
    io:format("Images where all differing functions first differ at allocate:~n"),
    [io:format("~s (~B functions)~n", [Path, length(ModulePairs)]) ||
        {Path, ModulePairs, _} <- Rows, lists:usort(ModulePairs) =:= [{allocate, allocate}]].

summarize(Path, Expected, Actual) ->
    Keys = lists:usort(maps:keys(Expected) ++ maps:keys(Actual)),
    Differences = [first_difference(maps:get(Key, Expected, missing), maps:get(Key, Actual, missing)) ||
                     Key <- Keys, maps:get(Key, Expected, missing) =/= maps:get(Key, Actual, missing)],
    Frames = [{relative(A, E), relative(AL, EL)} ||
                 {{allocate, E, EL}, {allocate, A, AL}} <- Differences],
    {Path, [difference(Difference) || Difference <- Differences], Frames}.

relative(A, B) when A < B -> smaller;
relative(A, B) when A > B -> larger;
relative(_, _) -> equal.

score(Pair, Rows) ->
    Counts = [length([P || P <- Pairs, P =:= Pair]) || {_, Pairs, _} <- Rows],
    {length([C || C <- Counts, C > 0]),
     length([Path || {Path, Pairs, _} <- Rows, lists:usort(Pairs) =:= [Pair]]),
     lists:sum(Counts), Pair}.

difference({missing, _}) -> missing_in_otp;
difference({_, missing}) -> missing_in_csharp;
difference({E, A}) -> {opcode(E), opcode(A)}.

opcode({test, Test, _, _}) -> {test, Test};
opcode(Instruction) when is_tuple(Instruction) -> element(1, Instruction);
opcode(Instruction) -> Instruction.

first_difference(missing, _) -> {missing, []};
first_difference(_, missing) -> {[], missing};
first_difference([I|Expected],[I|Actual]) -> first_difference(Expected,Actual);
first_difference([E|_],[A|_]) -> {E,A};
first_difference([], [A|_]) -> {end_of_function,A};
first_difference([E|_], []) -> {E,end_of_function};
first_difference([],[]) -> same.



