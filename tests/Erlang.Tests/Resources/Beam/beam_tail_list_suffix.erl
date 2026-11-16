-module(beam_tail_list_suffix).
-export([proper/1, improper/1, dynamic/2]).
proper(X) ->
    beam_probe:pass([X, a, b]).
improper(X) ->
    beam_probe:pass([X, a | b]).
dynamic(X, Tail) ->
    beam_probe:pass([X, a | Tail]).
