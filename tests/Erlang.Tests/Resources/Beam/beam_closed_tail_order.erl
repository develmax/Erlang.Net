-module(beam_closed_tail_order).
-export([run/1, reshuffle/1, keep/3]).
run(Arg) ->
    Error = {error, {reason, bad}},
    Empty = [],
    keep(Arg, Empty, Error).
reshuffle(Arg) ->
    Options = [{setting, enabled}],
    Empty = [],
    keep(Options, Arg, Empty).
keep(A, B, C) ->
    {A, B, C}.
