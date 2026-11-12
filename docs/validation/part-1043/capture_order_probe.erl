-module(capture_order_probe).
-export([run/3]).
run(Z, A, M) -> lists:map(fun(X) -> relay(X, A, M, Z) end, [sample]).
relay(X, A, M, Z) -> {X, A, M, Z}.
