-module(beam_call_result_callback).
-export([run/3]).
run(F,A,B) ->
    V1 = F(B),
    V2 = next(A,V1),
    {V2,B}.
next(A,V) -> {next,A,V}.
