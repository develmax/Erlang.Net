-module(beam_guarded_non_gc_bif).
-export([classify/2, choose/3, second/2, tuple_tag/2]).
classify(Tuple, Keep) when element(1, Tuple) =:= yes -> {ok, Keep};
classify(_, Keep) -> {fallback, Keep}.
choose(N, Tuple, Keep) when element(N, Tuple) =:= yes -> {ok, Keep};
choose(_, _, Keep) -> leaf_tail_probe:resolve(Keep, stream).
second(Tuple, Keep) when element(2, Tuple) =:= yes -> {ok, Keep};
second(_, Keep) -> {fallback, Keep}.
tuple_tag(Tuple, Keep) when tuple_size(Tuple) =:= 2 -> {ok, Keep};
tuple_tag(_, Keep) -> {fallback, Keep}.
