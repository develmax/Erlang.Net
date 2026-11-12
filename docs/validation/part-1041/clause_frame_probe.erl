-module(clause_frame_probe).
-export([run/2]).
run({tag, X}, Y) when is_binary(X) ->
    erlang:put(clause_frame_probe, X),
    {binary, X, Y};
run({tag, X}, Y) ->
    erlang:put(clause_frame_probe, Y),
    {other, X, Y};
run(X, Y) ->
    erlang:erase(clause_frame_probe),
    {fallback, X, Y}.
