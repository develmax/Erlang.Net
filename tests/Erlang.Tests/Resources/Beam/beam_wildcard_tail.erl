-module(beam_wildcard_tail).
-export([run/1, nested/1]).
run(X) ->
    _ = beam_probe:pass(X).
nested(X) ->
    _ = (_ = beam_probe:pass(X)).
