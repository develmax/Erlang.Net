-module(arithmetic).
-export([double/1, zero/0, classify/1]).
double(X) when is_integer(X) -> X * 2.
zero() -> -0.0.
classify(0.0) -> positive;
classify(-0.0) -> negative.
