-module(entry_multi_list_heap_probe).
-export([f/3, g/3]).
take(A, B, C) -> {A,B,C}.
f(A, B, C) ->
    Value = take([B, fixed], C, A),
    {Value, C}.
g(A, B, C) ->
    Value = take(B, [A, fixed], C),
    {Value, A}.
