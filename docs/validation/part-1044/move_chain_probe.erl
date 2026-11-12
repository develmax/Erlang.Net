-module(move_chain_probe).
-export([run/3]).
run(A, B, C) -> lists:map(fun(X) -> relay(A, A, X, C, B) end, [sample]).
relay(A, Again, X, C, B) -> {A, Again, X, C, B}.
