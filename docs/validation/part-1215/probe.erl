-module(boolean_case_dispatch_probe).
-export([f/2, g/2]).
identity(V) -> V.
f(Value, Result) ->
    case identity(Value) of true -> {yes,Result}; false -> {no,Result} end.
g(Value, Result) ->
    case identity(Value) of false -> {no,Result}; true -> {yes,Result} end.
