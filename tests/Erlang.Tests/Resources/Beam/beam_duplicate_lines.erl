-module(beam_duplicate_lines).
-export([run/0, catch_fail/0]).
run() ->
    _ = {beam_probe:pass(first), [beam_probe:pass(second) | beam_probe:pass(third)]},
    ok.
catch_fail() ->
    beam_probe:pass(before_failure), beam_probe:pass(fail).
