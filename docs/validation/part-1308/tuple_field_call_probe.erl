-module(tuple_field_call_probe).
-export([run/1]).
run({A,B}) -> {decrease(A),B};
run(Other) -> Other.
decrease(N) when is_integer(N), N > 0 -> N-1;
decrease(Other) -> Other.
