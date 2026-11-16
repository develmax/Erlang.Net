-module(beam_nested_siblings_probe).
-export([run/2]).
run(A,B) ->
    finish(left(A), right(B)).
left(A) -> {left,A}.
right(B) -> {right,B}.
finish(A,B) -> {A,B}.
