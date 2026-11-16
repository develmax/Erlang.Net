-module(beam_sibling_effects_probe).
-export([run/2, keep/2]).
run(A,B) -> finish(left(A), right(B)).
keep(A,B) -> finish(left(A), right(B), B).
left(fail) -> erlang:error(left_failed);
left(A) -> erlang:put(sibling_order, left), {left,A}.
right(B) -> erlang:put(sibling_order, right), {right,B}.
finish(A,B) -> {A,B}.
finish(A,B,C) -> {A,B,C}.
