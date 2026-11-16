-module(beam_constructor_probe).
-export([pair/2, tagged/1, single/1, cons/2, empty/0, triple/3, record/2, append/2, prefix/1, empty_right/1, append_pair/2]).
-record(sample, {first, second, third = default}).
pair(A,B) -> {A,B}.
tagged(X) -> {ok,X}.
single(A) -> [A].
cons(A,B) -> [A|B].
empty() -> [].
triple(_,B,C) -> {B,ok,C}.
record(A,B) -> #sample{second = B, first = A}.
append(A,B) ->
    A ++ B.
prefix(A) -> [1,2] ++ A.
empty_right(A) -> A ++ [].
append_pair(A,B) -> {A ++ B, B}.
