-module(beam_element_operands).
-export([run/2]).
run(A,B) ->
    {element(index(A),tuple(B)), element(1,tuple(B))}.
index(A) -> put(last, index), A.
tuple(B) -> put(last, tuple), B.
