-module(direct_exception_match_probe).
-export([f/1, g/1]).
f(Value) ->
    try erlang:error(Value)
    catch error:expected:Stack -> {matched, Stack}
    end.
g(Value) ->
    try erlang:throw(Value)
    catch throw:expected:_ -> matched
    end.
