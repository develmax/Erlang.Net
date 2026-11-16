-module(beam_sibling_three_probe).
-export([run/3, nonzero/2]).
run(A,B,C) -> finish(left(A), right(B), last(C)).
nonzero(A,B) -> pair(left(A), finish(literal,B,tail)).
left(A) -> {left,A}.
right(B) -> {right,B}.
last(C) -> {last,C}.
pair(A,B) -> {A,B}.
finish(A,B,C) -> {A,B,C}.
