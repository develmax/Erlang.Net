-module(entry_tuple_heap_probe).
-export([f/3, g/2]).
take(A,B,C) -> {A,B,C}.
one(A) -> A.
f(A, B, C) ->
    Value = take({tag, B, A}, C, A),
    {Value, C}.
g(A, B) ->
    Value = one({tag, A, B}),
    {Value, A}.
