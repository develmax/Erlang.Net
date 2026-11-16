-module(length_call_line_probe).
-export([same/2, split/2]).
collect(A,B,C) -> {A,B,C}.
same(List, Value) ->
    {result, collect(Value, fixed, length(List))}.
split(List, Value) ->
    {result, collect(Value, fixed,
                     length(List))}.
