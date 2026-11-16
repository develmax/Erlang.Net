-module(beam_subtract_probe).
-export([sub/2, constant/1, reverse/1, neg/1, guard/2]).
sub(A,B) ->
    A - B.
constant(A) -> A - 1.
reverse(A) -> 7 - A.
neg(A) -> -A.
guard(A,B) when A - B > 0 -> yes;
guard(_,_) -> no.
