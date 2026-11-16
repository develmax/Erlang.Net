#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Message = <<"MEGACO/1 [124.124.124.222] Transaction = 9998 { Context = - { "
                "ServiceChange = ROOT { Services { Method=Restart, Reason=\"901 Cold Boot\", "
                "ServiceChangeAddress=55555, Profile=ResGW/1 } } } }">>,
    Partial = <<"MEGACO/1 [124.124.124.222] Transaction = 9998 {">>,
    {ok, Tokens, 1, _} = megaco_text_scanner:scan(Message),
    {ok, Incomplete, 1, _} = megaco_text_scanner:scan(Partial),
    Expected = parse(Reference, [Tokens, Incomplete]),
    Actual = parse(Generated, [Tokens, Incomplete]),
    io:format("OTP ~0p~nC# ~0p~n", [Expected, Actual]),
    [{ok, _}, {error, _}] = Expected,
    Expected = Actual,
    io:format("Valid and incomplete messages match.~n").

parse(Path, Inputs) ->
    Module = megaco_text_parser_v1,
    {module, Module} = code:load_abs(filename:rootname(filename:absname(Path))),
    Results = [Module:parse(Tokens) || Tokens <- Inputs],
    true = code:delete(Module),
    code:purge(Module),
    Results.
