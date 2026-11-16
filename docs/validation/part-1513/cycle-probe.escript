#!/usr/bin/env escript
-mode(compile).
main([Reference, Candidate]) ->
    [check(Name, Reference, Candidate) || Name <- [choose, choose_unknown]],
    Calls = [{F, Count, Value} || F <- [choose, choose_unknown],
             Count <- lists:seq(0, 7), Value <- [missing, ok, 2, [a], {tuple}, #{}, <<3>>]],
    Results = run(Reference, Calls),
    Results = run(Candidate, Calls),
    io:format("~B returned terms match OTP for mutually recursive producers.~n", [length(Calls)]).

check(Name, Reference, Candidate) ->
    Type = selector(Reference, Name),
    Type = selector(Candidate, Name),
    io:format("~p/3 selector: ~0p~n", [Name, Type]).

selector(Path, Name) ->
    {beam_file, _, _, _, _, Functions} = beam_disasm:file(Path),
    [Operand] = [V || {function, F, 3, _, Code} <- Functions, F =:= Name,
                     {test, is_eq_exact, _, [V, {atom, missing}]} <- Code],
    Operand.

run(Path, Calls) ->
    Module = beam_union_cycle_probe,
    {ok, Image} = file:read_file(Path),
    {module, Module} = code:load_binary(Module, Path, Image),
    try
        [apply(Module, F, [Count, Value, default]) || {F, Count, Value} <- Calls]
    after
        code:delete(Module),
        code:purge(Module)
    end.
