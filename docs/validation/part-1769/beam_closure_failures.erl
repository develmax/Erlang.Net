-module(beam_closure_failures).
-export([guard/1, tuple/1, multi/1, fallback/1, swap/1, plain/0, body/1, left/1]).
guard(Key) -> fun(X) when is_atom(X) -> {Key,X} end.
tuple(Key) -> fun({X,Y}) -> {Key,X,Y} end.
multi(Key) -> fun(yes)->{Key,yes}; (no)->{Key,no} end.
fallback(Key) -> fun(yes)->{Key,yes}; (X)->{Key,X} end.
swap(Key) -> fun(X, yes) -> {Key,X}; (Y,no) -> {Y,Key} end.
plain() -> fun({yes,X}) -> X end.
body(Key) -> fun(X) when X =:= Key -> X; (Y) when is_integer(Y) -> {Key,Y} end.
left(Key) -> [fun(X) when is_atom(X) -> {Key,X} end].
