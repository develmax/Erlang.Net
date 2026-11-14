-module(beam_append_destination).
-export([run/1]).
run(Tail) ->
    beam_probe:take(key, " " ++ Tail).
