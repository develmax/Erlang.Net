-module(beam_tail_argument_origin).
-export([run/9]).
run(A, B, C, D, E, F, G, H, I) ->
    target(A, B, C, D, E, {inserted}, F, G, H, I).
target(A, B, C, D, E, Insertion, F, G, H, I) ->
    {A, B, C, D, E, Insertion, F, G, H, I}.
