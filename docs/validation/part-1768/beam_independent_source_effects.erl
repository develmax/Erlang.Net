-module(beam_independent_source_effects).
-export([selected/2]).
selected(A, B) ->
    {[{left, X} || X <- generator(left, A)], [{right, X} || X <- generator(right, B)]}.
generator(Label, Data) ->
    put(Label, get(counter)),
    put(counter, get(counter) + 1),
    Data.
