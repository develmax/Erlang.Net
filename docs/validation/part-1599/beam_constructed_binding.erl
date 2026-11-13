-module(beam_constructed_binding).
-export([run/3]).
run(A, B, C) ->
    {X, Y, Z} = {id(A), id(B), id(C)},
    {Z, X, Y}.
id(X) -> {value, X}.
