-module(length_argument_probe).
-export([f/2, g/2]).
collect(A,B,C) -> {A,B,C}.
f(List, Value) -> collect(Value, fixed, length(List)).
g(List, Value) -> collect(length(List), Value, fixed).
