-module(mixed_move_probe).
-export([run/3]).
run(A, B, C) -> lists:map(fun(X) -> relay(A, fixed, X, C, B, marker) end, [sample]).
relay(A, Fixed, X, C, B, Marker) -> {A, Fixed, X, C, B, Marker}.
