-module(beam_dead_call_binding).
-export([run/1, used/1, repeated/1]).
run(X) ->
    Ignored = beam_probe:pass(X),
    beam_probe:pass(done),
    {ok,X}.
used(X) ->
    Y = beam_probe:pass(X),
    beam_probe:pass(done),
    F = fun() -> Y end,
    {F(),X}.
repeated(X) ->
    Y = beam_probe:pass(X),
    Y = beam_probe:pass(other),
    ok.
