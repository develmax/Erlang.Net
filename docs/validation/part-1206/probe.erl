-module(entry_guard_comparison_probe).
-export([f/2]).
identity(X) -> X.
f(X, Y) when is_integer(X) andalso X > 0 andalso X =< Y ->
    A = identity(X),
    B = identity(Y),
    {A, B, identity(A + B)}.
