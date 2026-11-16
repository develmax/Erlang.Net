-module(beam_discard_constructors).
-export([run/0]).
run() ->
    _ = {
        beam_probe:pass(first),
        [beam_probe:pass(second) |
         beam_probe:pass(third)]},
    ok.
