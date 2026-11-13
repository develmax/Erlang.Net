-module(beam_repeated_bif_operand).
-export([run/2]).
run(A,B) ->
    {min(A,A),map_get(A,B),A}.
