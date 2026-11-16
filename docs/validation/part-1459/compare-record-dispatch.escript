#!/usr/bin/env escript
-mode(compile).

main([Reference, Prior, Current, Name]) ->
    Key = {list_to_atom(Name), 2},
    Expected = read(Reference, Key),
    Before = read(Prior, Key),
    After = read(Current, Key),
    io:format("~0p instructions OTP ~B, previous C# ~B, current C# ~B~n",
        [Key, length(Expected), length(Before), length(After)]),
    lists:foreach(fun({Title, Body}) ->
        io:format("~s entry ~0p~n~s fallback ~0p~n",
            [Title, [summarize(I) || I <- lists:sublist(Body, 8)],
             Title, lists:nthtail(length(Body) - 6, Body)])
    end, [{"OTP", Expected}, {"Previous", Before}, {"Current", After}]),
    true = normalize(Expected) =:= normalize(After),
    io:format("Current instructions match after label rebasing.~n").

read(Path, Key) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    [Ops] = [Body || {function, Name, Arity, _, Body} <- Functions, {Name, Arity} =:= Key],
    Ops.

normalize([{label, Start}|Body]) -> [{label, 0}|[rebase(Op, Start) || Op <- Body]].

rebase({f, 0}, _) -> {f, 0};
rebase({f, Label}, Start) -> {f, Label - Start};
rebase({label, Label}, Start) -> {label, Label - Start};
rebase(Tuple, Start) when is_tuple(Tuple) -> list_to_tuple(rebase(tuple_to_list(Tuple), Start));
rebase(List, Start) when is_list(List) -> [rebase(Item, Start) || Item <- List];
rebase(Term, _) -> Term.

summarize({select_val, Register, Failure, {list, Keys}}) ->
    {select_val, Register, Failure, length(Keys) div 2, lists:sublist(Keys, 6)};
summarize(Op) -> Op.
