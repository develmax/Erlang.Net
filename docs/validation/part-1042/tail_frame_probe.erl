-module(tail_frame_probe).
-export([run/3]).
run(A, B, C) -> lists:map(fun(X) -> relay(A, X, C, B) end, [sample]).
relay(A, X, C, B) -> {A, X, C, B}.
