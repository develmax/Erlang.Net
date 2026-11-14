-module(beam_dead_call_binding).
-export([run/1]).
run(X) ->
    Ignored = beam_probe:pass(X),
    beam_probe:pass(done),
    {ok,X}.
