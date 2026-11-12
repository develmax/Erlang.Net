-module(beam_move_probe).
-export([order/3, dup/2, shift/3, ord/3, lit/1]).
order(A,B,C) -> target(C,A,B).
dup(A,B) -> pair(B,B).
shift(A,B,C) -> pair(B,C).
ord(_,B,_) -> one(B), ok.
lit(42) -> yes;
lit(_) -> no.
target(A,B,_) -> A.
pair(_,B) -> B.
one(X) -> X.
