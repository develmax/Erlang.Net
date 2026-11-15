-module(beam_independent_probe).
-export([seq/2, tuple/2, list/2, remote/2, match/2, binary/2, case1/3, case2/3, fun1/3, fun2/3, fun3/3, guard1/3, mix/3]).
seq(A,B) -> L = [a(X) || X <- A], R = [b(X) || X <- B], {L,R}.
tuple(A,B) -> {[a(X) || X <- A], [b(X) || X <- B]}.
list(A,B) -> [[a(X) || X <- A], [b(X) || X <- B]].
remote(A,B) -> erlang:append_element([a(X) || X <- A], [b(X) || X <- B]).
match(A,B) -> {L,R} = {[a(X) || X <- A], [b(X) || X <- B]}, {R,L}.
binary(A,B) -> [a(X) || X <- A] ++ [b(X) || X <- B].
case1(A,B,K) -> case K of yes -> [a(X) || X <- A]; _ -> [b(X) || X <- B] end.
case2(A,B,K) -> case K of yes -> [a(X) || X <- A]; no -> [b(X) || X <- B] end.
fun1(A,B,yes) -> [a(X) || X <- A]; fun1(A,B,_) -> [b(X) || X <- B].
fun2(A,B,yes) -> [a(X) || X <- A]; fun2(A,B,no) -> [b(X) || X <- B].
fun3(A,B,yes) -> [a(X) || X <- A]; fun3(A,B,no) -> [b(X) || X <- B]; fun3(_,_,_) -> [].
guard1(A,_B,K) when is_atom(K) -> [a(X) || X <- A]; guard1(_A,B,_) -> [b(X) || X <- B].
mix(A,B,K) -> {case K of yes -> [a(X) || X <- A]; _ -> [] end, [b(X) || X <- B]}.
a(X) -> {a,X}.
b(X) -> {b,X}.
