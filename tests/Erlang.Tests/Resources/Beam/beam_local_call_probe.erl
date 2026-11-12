-module(beam_local_call_probe).
-export([caller/2, zero/0]).
caller(X, Y) -> target(X, Y).
target(_, Y) -> Y.
zero() -> constant().
constant() -> 42.
