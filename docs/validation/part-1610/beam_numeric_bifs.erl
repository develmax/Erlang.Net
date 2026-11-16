-module(beam_numeric_bifs).
-export([run/3, computed/1]).
run(A,B,M) ->
    {round(A),trunc(B),map_size(M),abs(A),float(B),ceil(A),floor(A)}.
computed(A) -> {ok, round(value(A))}.
value(A) -> {rounding_input,A}, A.
