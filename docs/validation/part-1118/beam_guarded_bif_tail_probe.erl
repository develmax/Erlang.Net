-module(beam_guarded_bif_tail_probe).
-export([run/2]).
run(A,B) when is_float(A) -> run(erlang:floor(A), B);
run(A,B) when is_float(B) -> run(A, erlang:floor(B));
run(A,B) -> {A,B}.

