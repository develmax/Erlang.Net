#!/usr/bin/env escript
-mode(compile).

main([Reference, Prior, Current]) ->
    Key = {yeccpars2_cont_4, 7},
    Expected = maps:get(Key, functions(Reference)),
    Before = maps:get(Key, functions(Prior)),
    After = maps:get(Key, functions(Current)),
    io:format("~0p~nInstructions OTP ~B, previous C# ~B, current C# ~B~n",
        [Key, length(Expected), length(Before), length(After)]),
    lists:foreach(fun({Title, Body}) ->
        io:format("~s entry ~0p~n~s fallback ~0p~n",
            [Title, [summarize(I) || I <- lists:sublist(Body, 8)],
             Title, lists:nthtail(length(Body) - 3, Body)])
    end, [{"OTP", Expected}, {"Previous", Before}, {"Current", After}]),
    true = normalize(Expected) =:= normalize(After),
    io:format("Current instructions match after label rebasing.~n").

functions(Path) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    maps:from_list([{{Name, Arity}, Ops} || {function, Name, Arity, _, Ops} <- Functions]).

normalize([{label, Start}|_]=Body) -> [rebase(Op, Start) || Op <- Body].

rebase({f, 0}, _) -> {f, 0};
rebase({f, Label}, Start) -> {f, Label - Start};
rebase({label, Label}, Start) -> {label, Label - Start};
rebase(Tuple, Start) when is_tuple(Tuple) -> list_to_tuple(rebase(tuple_to_list(Tuple), Start));
rebase(List, Start) when is_list(List) -> [rebase(Item, Start) || Item <- List];
rebase(Term, _) -> Term.

summarize({select_val, Register, Failure, {list, Keys}}) ->
    {select_val, Register, Failure, length(Keys) div 2, lists:sublist(Keys, 6)};
summarize(Op) -> Op.
