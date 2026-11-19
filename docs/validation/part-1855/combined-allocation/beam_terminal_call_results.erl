-module(beam_terminal_call_results).
-export([tuple/2, list/2]).
tuple(X, Y) ->
    Value = other:consume([X, Y]),
    {Value, Y}.
list(X, Y) ->
    Value = other:consume([X, Y]),
    [Value | Y].
