-module(integer_tail_dispatch_probe).
-export([run/3]).
run(A, 2, B) -> first(A,B);
run(X, 17, Y) -> first(X,Y);
run(A, 9, B) -> second(B,A);
run(A, -4, B) -> third(A,B).
first(A,B) -> {first,A,B}.
second(A,B) -> {second,A,B}.
third(A,B) -> {third,A,B}.
